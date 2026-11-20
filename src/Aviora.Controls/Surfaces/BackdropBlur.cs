using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Aviora.Controls;

/// <summary>Blurs the already rendered page beneath a tinted, rounded surface.</summary>
public sealed class BackdropBlur : TemplatedControl
{
    /// <summary>Defines the <see cref="BlurRadius"/> property.</summary>
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BackdropBlur, double>(nameof(BlurRadius), 12,
            validate: value => double.IsFinite(value) && value >= 0);

    /// <summary>Defines the <see cref="RenderScale"/> property.</summary>
    public static readonly StyledProperty<double> RenderScaleProperty =
        AvaloniaProperty.Register<BackdropBlur, double>(nameof(RenderScale), 1,
            validate: value => double.IsFinite(value) && value >= 0.25 && value <= 1);

    // Delegate surface rendering to Border for its cached geometry, asymmetric borders,
    // individual corner radii, and BackgroundSizing behavior. This is not a visual child.
    private readonly Border _surface = new();
    private readonly BackdropResourcesOwner _backdropResources = new();

    static BackdropBlur()
    {
        AffectsRender<BackdropBlur>(BlurRadiusProperty, RenderScaleProperty, BackgroundProperty, BackgroundSizingProperty,
            BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty);
    }

    /// <summary>
    /// Gets or sets the Gaussian blur sigma in device-independent pixels. Zero disables blur.
    /// </summary>
    public double BlurRadius
    {
        get => GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the background sampling resolution, from 0.25 to 1 (the default).
    /// Lower values reduce background detail and can improve large or high-DPI blur
    /// performance. The blur radius in DIPs and the resolution of the corners,
    /// border, and foreground stay unchanged.
    /// </summary>
    public double RenderScale
    {
        get => GetValue(RenderScaleProperty);
        set => SetValue(RenderScaleProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        if (BlurRadius > 0)
            context.Custom(new BackdropOperation(bounds, CornerRadius, BlurRadius, RenderScale, _backdropResources));

        _surface.Background = Background;
        _surface.BackgroundSizing = BackgroundSizing;
        _surface.BorderBrush = BorderBrush;
        _surface.BorderThickness = BorderThickness;
        _surface.CornerRadius = CornerRadius;
        _surface.Measure(bounds.Size);
        _surface.Arrange(bounds);
        _surface.Render(context);
    }

    // Operations keep immutable drawing parameters. Only their render resources are
    // shared across replacements; the owner is weak so removing a scene frees them.
    private sealed class BackdropResourcesOwner
    {
        private WeakReference<BackdropResources>? _last;

        public BackdropResources Acquire()
        {
            lock (this)
            {
                if (_last is not null && _last.TryGetTarget(out var resources) && resources.TryRetain())
                    return resources;
                resources = new BackdropResources();
                _last = new WeakReference<BackdropResources>(resources);
                return resources;
            }
        }
    }

    private sealed class BackdropOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly CornerRadius _corners;
        private readonly double _radius;
        private readonly double _renderScale;
        private BackdropResources? _resources;

        public BackdropOperation(Rect bounds, CornerRadius corners, double radius, double renderScale)
            : this(bounds, corners, radius, renderScale, new BackdropResourcesOwner()) { }

        public BackdropOperation(Rect bounds, CornerRadius corners, double radius, double renderScale,
            BackdropResourcesOwner owner)
        {
            _bounds = bounds;
            _corners = corners;
            _radius = radius;
            _renderScale = renderScale;
            _resources = owner.Acquire();
        }

        public Rect Bounds => _bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => other is BackdropOperation operation
            && operation._bounds == _bounds && operation._corners == _corners
            && operation._radius == _radius && operation._renderScale == _renderScale;
        public void Dispose() => Interlocked.Exchange(ref _resources, null)?.Release();
        public void Render(ImmediateDrawingContext context) =>
            Volatile.Read(ref _resources)?.Render(context, _bounds, _corners, _radius, _renderScale);
    }

    private sealed class BackdropResources
    {
        private int _references = 1;
        private SKPaint? _paint;
        private SKPaint? _copyPaint;
        private SKPaint? _compositePaint;
        private SKRoundRect? _clip;
        private Rect _clipBounds;
        private CornerRadius _clipCorners;
        private SKImageFilter? _blur;
        private SKImageFilter? _previousBlur;
        private float _sigmaX;
        private float _sigmaY;
        private float _previousSigmaX;
        private float _previousSigmaY;
        private SKSurface? _blurSurface;
        private WeakReference<GRContext>? _workContext;
        private SKImageInfo _workInfo;
        private IntPtr _workColorSpaceHandle;
        private SKSurface? _cleanBackdrop;
        private WeakReference<SKSurface>? _cleanSourceSurface;
        private WeakReference<GRContext>? _cleanContext;
        private SKImageInfo _cleanInfo;
        private IntPtr _cleanColorSpaceHandle;
        private SKRectI _cleanRect;
        private SKRect _cleanDeviceRect;
        private static readonly SKSamplingOptions LinearSampling = new(SKFilterMode.Linear);
        private static readonly SKSamplingOptions NearestSampling = new(SKFilterMode.Nearest);

        public bool TryRetain()
        {
            int count = Volatile.Read(ref _references);
            while (count > 0)
            {
                int observed = Interlocked.CompareExchange(ref _references, count + 1, count);
                if (observed == count) return true;
                count = observed;
            }
            return false;
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref _references) != 0) return;
            lock (this)
            {
                _paint?.Dispose();
                _copyPaint?.Dispose();
                _compositePaint?.Dispose();
                _blur?.Dispose();
                _previousBlur?.Dispose();
                _clip?.Dispose();
                DisposeWorkSurfaces();
                DisposeCleanBackdrop();
                _paint = null;
                _copyPaint = null;
                _compositePaint = null;
                _blur = null;
                _previousBlur = null;
                _clip = null;
            }
        }

        public void Render(ImmediateDrawingContext context, Rect bounds, CornerRadius corners, double radius, double renderScale)
        {
            // Protect resource updates when the same control is also rendered to a
            // bitmap. UI code never mutates the native paints, filters, or surfaces.
            lock (this)
            {
                if (_references <= 0 || context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature) return;
                using var lease = feature.Lease();
                if (lease.CurrentOpacity <= 0) return;
                var canvas = lease.SkCanvas;
                var matrix = canvas.TotalMatrix;
                var rect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
                var deviceRect = matrix.MapRect(rect);
                if (!float.IsFinite(deviceRect.Left) || !float.IsFinite(deviceRect.Top)
                    || !float.IsFinite(deviceRect.Right) || !float.IsFinite(deviceRect.Bottom)) return;
                var sigmaX = (float)radius * MathF.Sqrt(matrix.ScaleX * matrix.ScaleX + matrix.SkewX * matrix.SkewX);
                var sigmaY = (float)radius * MathF.Sqrt(matrix.SkewY * matrix.SkewY + matrix.ScaleY * matrix.ScaleY);
                if (!float.IsFinite(sigmaX) || !float.IsFinite(sigmaY) || sigmaX <= 0 || sigmaY <= 0) return;
                var visible = canvas.DeviceClipBounds;
                if (deviceRect.Right <= visible.Left || deviceRect.Left >= visible.Right
                    || deviceRect.Bottom <= visible.Top || deviceRect.Top >= visible.Bottom) return;
                UpdateClip(bounds, corners, rect);

                // Keep one physical pixel around the rounded device clip: integer
                // clip bounds can otherwise trim a fractional, fully visible card
                // and change its downsampling grid as it moves.
                var captureRect = new SKRect(Math.Max(deviceRect.Left, visible.Left - 1f), Math.Max(deviceRect.Top, visible.Top - 1f),
                    Math.Min(deviceRect.Right, visible.Right + 1f), Math.Min(deviceRect.Bottom, visible.Bottom + 1f));
                // Output may be clipped by a scrolling viewport. Input still includes
                // the full halo beyond that clip, bounded only by the source surface.
                captureRect.Inflate(3 * sigmaX, 3 * sigmaY);

                using var snapshot = lease.SkSurface?.Snapshot();
                var gpu = lease.GrContext;
                if (snapshot is null || (snapshot.IsTextureBacked && gpu is null))
                {
                    if (renderScale == 1 && _blurSurface is not null) DisposeWorkSurfaces();
                    RenderNativeBackdrop(canvas, deviceRect, sigmaX, sigmaY, lease.CurrentOpacity);
                    return;
                }

                // Partial dirty-rect redraws (e.g. a foreground Slider updating above the blur)
                // only clear the dirty rect on the target surface; outside it, the surface still
                // holds the previous frame's blur, tint, and foreground. Maintain an uncontaminated
                // backdrop copy and patch only the newly redrawn dirty rect into it.
                if (!EnsureAndPatchCleanBackdrop(lease.SkSurface!, snapshot, gpu, deviceRect, sigmaX, sigmaY, visible, captureRect, out bool fullCleanVisible, out var renderCapture))
                {
                    if (renderScale == 1 && _blurSurface is not null) DisposeWorkSurfaces();
                    RenderNativeBackdrop(canvas, deviceRect, sigmaX, sigmaY, lease.CurrentOpacity);
                    return;
                }

                if (renderScale == 1)
                {
                    if (_blurSurface is not null) DisposeWorkSurfaces();
                    if (fullCleanVisible)
                    {
                        RenderNativeBackdrop(canvas, deviceRect, sigmaX, sigmaY, lease.CurrentOpacity);
                        return;
                    }

                    using var cleanImage = _cleanBackdrop!.Snapshot();
                    if (cleanImage is null)
                    {
                        RenderNativeBackdrop(canvas, deviceRect, sigmaX, sigmaY, lease.CurrentOpacity);
                        return;
                    }
                    RenderCleanBackdrop(canvas, cleanImage, _cleanRect, sigmaX, sigmaY, lease.CurrentOpacity);
                    return;
                }

                using var downsampledSource = fullCleanVisible ? null : _cleanBackdrop!.Snapshot();
                var sourceImage = downsampledSource ?? snapshot;
                var sourceOffset = fullCleanVisible ? default : new SKPoint(_cleanRect.Left, _cleanRect.Top);
                using var image = CaptureBackdrop(sourceImage, sourceOffset, gpu, renderCapture, sigmaX, sigmaY, renderScale, out var sampleRect);
                if (image is null)
                {
                    if (fullCleanVisible)
                        RenderNativeBackdrop(canvas, deviceRect, sigmaX, sigmaY, lease.CurrentOpacity);
                    else if (downsampledSource is not null)
                        RenderCleanBackdrop(canvas, downsampledSource, _cleanRect, sigmaX, sigmaY, lease.CurrentOpacity);
                    return;
                }
                _compositePaint ??= new SKPaint();
                _compositePaint.Color = SKColors.White.WithAlpha((byte)Math.Clamp(lease.CurrentOpacity * 255, 0, 255));
                canvas.Save();
                try
                {
                    canvas.ClipRoundRect(_clip!, SKClipOperation.Intersect, antialias: true);
                    canvas.ResetMatrix();
                    var destination = new SKRect(renderCapture.Left, renderCapture.Top, renderCapture.Right, renderCapture.Bottom);
                    if (sampleRect.Width == image.Width && sampleRect.Height == image.Height)
                        canvas.DrawImage(image, destination, LinearSampling, _compositePaint);
                    else
                        canvas.DrawImage(image, sampleRect, destination, LinearSampling, _compositePaint);
                }
                finally { canvas.Restore(); }
            }
        }

        private void UpdateClip(Rect bounds, CornerRadius corners, SKRect rect)
        {
            if (_clip is not null && _clipBounds == bounds && _clipCorners == corners) return;
            _clip ??= new SKRoundRect();
            ReadOnlySpan<SKPoint> radii = stackalloc SKPoint[4]
            {
                new((float)corners.TopLeft, (float)corners.TopLeft),
                new((float)corners.TopRight, (float)corners.TopRight),
                new((float)corners.BottomRight, (float)corners.BottomRight),
                new((float)corners.BottomLeft, (float)corners.BottomLeft),
            };
            _clip.SetRectRadii(rect, radii);
            _clipBounds = bounds;
            _clipCorners = corners;
        }

        private void RenderNativeBackdrop(SKCanvas canvas, SKRect deviceRect, float sigmaX, float sigmaY, double opacity)
        {
            UpdateFilter(sigmaX, sigmaY);
            _compositePaint ??= new SKPaint();
            _compositePaint.Color = SKColors.White.WithAlpha((byte)Math.Clamp(opacity * 255, 0, 255));
            int saveCount = canvas.Save();
            try
            {
                canvas.ClipRoundRect(_clip!, SKClipOperation.Intersect, antialias: true);
                canvas.ResetMatrix();
                canvas.SaveLayer(new SKCanvasSaveLayerRec
                {
                    Bounds = deviceRect, Backdrop = _blur, Paint = _compositePaint,
                });
            }
            finally { canvas.RestoreToCount(saveCount); }
        }

        private void RenderCleanBackdrop(SKCanvas canvas, SKImage cleanImage, SKRectI cleanRect, float sigmaX, float sigmaY, double opacity)
        {
            UpdateFilter(sigmaX, sigmaY);
            _paint ??= new SKPaint { BlendMode = SKBlendMode.Src, Color = SKColors.White };
            var previousBlend = _paint.BlendMode;
            var previousColor = _paint.Color;
            _paint.BlendMode = SKBlendMode.SrcOver;
            _paint.Color = SKColors.White.WithAlpha((byte)Math.Clamp(opacity * 255, 0, 255));
            _paint.ImageFilter = _blur;
            canvas.Save();
            try
            {
                canvas.ClipRoundRect(_clip!, SKClipOperation.Intersect, antialias: true);
                canvas.ResetMatrix();
                canvas.DrawImage(cleanImage, cleanRect.Left, cleanRect.Top, _paint);
            }
            finally
            {
                canvas.Restore();
                _paint.BlendMode = previousBlend;
                _paint.Color = previousColor;
            }
        }

        private void UpdateFilter(float sigmaX, float sigmaY)
        {
            if (_blur is not null && _sigmaX == sigmaX && _sigmaY == sigmaY) return;
            if (_previousBlur is not null && _previousSigmaX == sigmaX && _previousSigmaY == sigmaY)
            {
                (_blur, _previousBlur) = (_previousBlur, _blur);
                (_sigmaX, _previousSigmaX) = (_previousSigmaX, _sigmaX);
                (_sigmaY, _previousSigmaY) = (_previousSigmaY, _sigmaY);
            }
            else
            {
                _previousBlur?.Dispose();
                _previousBlur = _blur;
                _previousSigmaX = _sigmaX;
                _previousSigmaY = _sigmaY;
                _blur = SKImageFilter.CreateBlur(sigmaX, sigmaY, SKShaderTileMode.Clamp);
                _sigmaX = sigmaX;
                _sigmaY = sigmaY;
            }
            if (_paint is not null) _paint.ImageFilter = _blur;
        }

        private static SKRectI ClampToSnapshot(SKRect rect, int width, int height) => new(
            (int)Math.Clamp(MathF.Floor(rect.Left), 0, width),
            (int)Math.Clamp(MathF.Floor(rect.Top), 0, height),
            (int)Math.Clamp(MathF.Ceiling(rect.Right), 0, width),
            (int)Math.Clamp(MathF.Ceiling(rect.Bottom), 0, height));

        private static SKSurface? CreateSurface(SKImageInfo info, GRContext? gpu) =>
            gpu is null ? SKSurface.Create(info) : SKSurface.Create(gpu, true, info);

        private void CopySnapshotRegion(SKImage source, SKRectI sourceRect, SKSurface target, SKRectI targetOrigin)
        {
            var dst = new SKRect(sourceRect.Left - targetOrigin.Left, sourceRect.Top - targetOrigin.Top,
                sourceRect.Right - targetOrigin.Left, sourceRect.Bottom - targetOrigin.Top);
            target.Canvas.DrawImage(source,
                new SKRect(sourceRect.Left, sourceRect.Top, sourceRect.Right, sourceRect.Bottom),
                dst, NearestSampling, _copyPaint);
        }

        private bool EnsureAndPatchCleanBackdrop(SKSurface sourceSurface, SKImage snapshot, GRContext? gpu,
            SKRect deviceRect, float sigmaX, float sigmaY, SKRectI visible, SKRect neededRectF,
            out bool fullCleanVisible, out SKRectI renderCapture)
        {
            fullCleanVisible = false;
            renderCapture = default;
            var needed = ClampToSnapshot(neededRectF, snapshot.Width, snapshot.Height);
            if (needed.Width <= 0 || needed.Height <= 0) return false;

            var maxRectF = deviceRect;
            maxRectF.Inflate(3 * sigmaX, 3 * sigmaY);
            var maxRect = ClampToSnapshot(maxRectF, snapshot.Width, snapshot.Height);

            var effectiveGpu = snapshot.IsTextureBacked ? gpu : null;
            using var colorSpace = snapshot.ColorSpace;
            var colorSpaceHandle = colorSpace?.Handle ?? IntPtr.Zero;
            bool sameSource = _cleanSourceSurface is not null && _cleanSourceSurface.TryGetTarget(out var previousSource)
                && ReferenceEquals(previousSource, sourceSurface);
            bool sameContext = effectiveGpu is null ? _cleanContext is null
                : _cleanContext is not null && _cleanContext.TryGetTarget(out var previousGpu) && ReferenceEquals(previousGpu, effectiveGpu);
            bool compatible = _cleanBackdrop is not null && sameSource && sameContext
                && _cleanInfo.ColorType == snapshot.ColorType && _cleanColorSpaceHandle == colorSpaceHandle;

            _copyPaint ??= new SKPaint { BlendMode = SKBlendMode.Src, Color = SKColors.White };
            var clampedDevice = ClampToSnapshot(deviceRect, snapshot.Width, snapshot.Height);
            fullCleanVisible = clampedDevice.Width > 0 && clampedDevice.Height > 0
                && visible.Left <= clampedDevice.Left && visible.Top <= clampedDevice.Top
                && visible.Right >= clampedDevice.Right && visible.Bottom >= clampedDevice.Bottom;

            if (!fullCleanVisible && compatible && _cleanDeviceRect == deviceRect)
            {
                if (!_cleanRect.Contains(needed))
                {
                    var expanded = SKRectI.Union(_cleanRect, needed);
                    expanded.Intersect(maxRect);
                    if (expanded.Width <= 0 || expanded.Height <= 0) return false;

                    var info = new SKImageInfo(expanded.Width, expanded.Height, snapshot.ColorType, SKAlphaType.Premul, colorSpace);
                    var grown = CreateSurface(info, effectiveGpu);
                    if (grown is null) return false;

                    CopySnapshotRegion(snapshot, expanded, grown, expanded);
                    using (var previousImage = _cleanBackdrop!.Snapshot())
                    {
                        if (previousImage is not null)
                        {
                            var oldDst = new SKRect(
                                _cleanRect.Left - expanded.Left, _cleanRect.Top - expanded.Top,
                                _cleanRect.Right - expanded.Left, _cleanRect.Bottom - expanded.Top);
                            grown.Canvas.DrawImage(previousImage, oldDst, NearestSampling, _copyPaint);
                        }
                    }
                    _cleanBackdrop.Dispose();
                    _cleanBackdrop = grown;
                    _cleanInfo = new SKImageInfo(info.Width, info.Height, info.ColorType, info.AlphaType);
                    _cleanRect = expanded;
                }

                var patch = SKRectI.Intersect(visible, _cleanRect);
                if (patch.Width > 0 && patch.Height > 0)
                    CopySnapshotRegion(snapshot, patch, _cleanBackdrop!, _cleanRect);
            }
            else
            {
                var initial = fullCleanVisible ? maxRect : needed;
                var info = new SKImageInfo(initial.Width, initial.Height, snapshot.ColorType, SKAlphaType.Premul, colorSpace);
                if (!compatible || _cleanInfo.Width != info.Width || _cleanInfo.Height != info.Height)
                {
                    DisposeCleanBackdrop();
                    _cleanBackdrop = CreateSurface(info, effectiveGpu);
                    if (_cleanBackdrop is null) return false;
                    _cleanInfo = new SKImageInfo(info.Width, info.Height, info.ColorType, info.AlphaType);
                    _cleanColorSpaceHandle = colorSpaceHandle;
                    _cleanContext = effectiveGpu is null ? null : new WeakReference<GRContext>(effectiveGpu);
                    _cleanSourceSurface = new WeakReference<SKSurface>(sourceSurface);
                }

                _cleanRect = initial;
                _cleanDeviceRect = deviceRect;
                CopySnapshotRegion(snapshot, initial, _cleanBackdrop!, initial);
            }

            renderCapture = fullCleanVisible ? needed : SKRectI.Intersect(maxRect, _cleanRect);
            return renderCapture.Width > 0 && renderCapture.Height > 0;
        }

        private SKImage? CaptureBackdrop(SKImage source, SKPoint sourceOrigin, GRContext? gpu, SKRectI capture,
            float sigmaX, float sigmaY, double renderScale, out SKRect sampleRect)
        {
            sampleRect = default;
            if (capture.Width <= 0 || capture.Height <= 0) return null;
            int width = Math.Max(1, (int)Math.Ceiling(capture.Width * renderScale));
            int height = Math.Max(1, (int)Math.Ceiling(capture.Height * renderScale));
            sampleRect = new SKRect(0, 0, width, height);
            using var colorSpace = source.ColorSpace;
            var info = new SKImageInfo(width, height, source.ColorType, SKAlphaType.Premul, colorSpace);
            if (!EnsureWorkSurfaces(info, source.IsTextureBacked ? gpu : null)) return null;

            _paint ??= new SKPaint { BlendMode = SKBlendMode.Src, Color = SKColors.White };
            UpdateFilter(sigmaX * width / capture.Width, sigmaY * height / capture.Height);
            _paint.ImageFilter = _blur;
            _blurSurface!.Canvas.Clear(SKColors.Transparent);
            _blurSurface.Canvas.DrawImage(source,
                new SKRect(capture.Left - sourceOrigin.X, capture.Top - sourceOrigin.Y,
                    capture.Right - sourceOrigin.X, capture.Bottom - sourceOrigin.Y),
                sampleRect, LinearSampling, _paint);
            return _blurSurface.Snapshot();
        }

        private bool EnsureWorkSurfaces(SKImageInfo info, GRContext? gpu)
        {
            bool sameContext = gpu is null ? _workContext is null
                : _workContext is not null && _workContext.TryGetTarget(out var previous) && ReferenceEquals(previous, gpu);
            var colorSpaceHandle = info.ColorSpace?.Handle ?? IntPtr.Zero;
            bool compatible = _blurSurface is not null && sameContext && _workInfo.ColorType == info.ColorType
                && _workColorSpaceHandle == colorSpaceHandle;
            if (_blurSurface is not null && _workInfo.Width >= info.Width && _workInfo.Height >= info.Height
                && _workInfo.Width <= Math.Max(64, info.Width * 2) && _workInfo.Height <= Math.Max(64, info.Height * 2)
                && compatible) return true;
            bool growing = compatible && (info.Width > _workInfo.Width || info.Height > _workInfo.Height);
            DisposeWorkSurfaces();
            // Fixed sizes keep an exact texture and the simpler whole-image draw.
            // Once sizes grow, reserve space for small subsequent animation changes.
            var capacity = new SKImageInfo(growing ? (info.Width + 15) / 16 * 16 : info.Width,
                growing ? (info.Height + 15) / 16 * 16 : info.Height,
                info.ColorType, info.AlphaType, info.ColorSpace);
            _blurSurface = gpu is null ? SKSurface.Create(capacity) : SKSurface.Create(gpu, true, capacity);
            if (_blurSurface is null) return false;
            _workInfo = new SKImageInfo(capacity.Width, capacity.Height, capacity.ColorType, capacity.AlphaType);
            _workColorSpaceHandle = colorSpaceHandle;
            _workContext = gpu is null ? null : new WeakReference<GRContext>(gpu);
            return true;
        }

        private void DisposeWorkSurfaces()
        {
            _blurSurface?.Dispose();
            _blurSurface = null;
            _workContext = null;
        }

        private void DisposeCleanBackdrop()
        {
            _cleanBackdrop?.Dispose();
            _cleanBackdrop = null;
            _cleanContext = null;
            _cleanSourceSurface = null;
            _cleanRect = default;
            _cleanDeviceRect = default;
        }
    }
}
