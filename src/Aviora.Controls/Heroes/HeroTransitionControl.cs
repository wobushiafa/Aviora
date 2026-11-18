using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Aviora.Controls;

/// <summary>
/// A <see cref="ContentControl"/> that smoothly transitions between old and new content,
/// designed to pair with <see cref="HeroHost"/> so non-hero content fades instead of snapping.
/// </summary>
[TemplatePart(OutgoingSnapshotPartName, typeof(Image))]
[TemplatePart(ContentPresenterPartName, typeof(ContentControl))]
public class HeroTransitionControl : ContentControl
{
    internal const string OutgoingSnapshotPartName = "PART_OutgoingSnapshot";
    internal const string ContentPresenterPartName = "PART_ContentPresenter";

    /// <summary>Defines the <see cref="IsAnimationEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<HeroTransitionControl, bool>(nameof(IsAnimationEnabled), true);

    /// <summary>Defines the <see cref="TransitionDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> TransitionDurationProperty =
        AvaloniaProperty.Register<HeroTransitionControl, TimeSpan>(
            nameof(TransitionDuration),
            TimeSpan.FromMilliseconds(180),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="TransitionEasing"/> property.</summary>
    public static readonly StyledProperty<Easing?> TransitionEasingProperty =
        AvaloniaProperty.Register<HeroTransitionControl, Easing?>(nameof(TransitionEasing), new CubicEaseOut());

    /// <summary>Defines the <see cref="TransitionMode"/> property.</summary>
    public static readonly StyledProperty<HeroContentTransitionMode> TransitionModeProperty =
        AvaloniaProperty.Register<HeroTransitionControl, HeroContentTransitionMode>(
            nameof(TransitionMode),
            HeroContentTransitionMode.Fade);

    private Image? _outgoingImage;
    private ContentControl? _presenter;
    private readonly ScaleTransform _outScale = new(1, 1);
    private readonly TranslateTransform _outTranslate = new(0, 0);
    private readonly TranslateTransform _inTranslate = new(0, 0);
    private RenderTargetBitmap? _outgoingBitmap;
    private DispatcherTimer? _completionTimer;
    private bool _isTransitioning;
    private bool _frameRequested;
    private long _startTimestamp;

    /// <summary>Gets or sets whether content transitions are animated.</summary>
    public bool IsAnimationEnabled
    {
        get => GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>Gets or sets the duration of the content transition.</summary>
    public TimeSpan TransitionDuration
    {
        get => GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <summary>Gets or sets the easing function used for the content transition.</summary>
    public Easing? TransitionEasing
    {
        get => GetValue(TransitionEasingProperty);
        set => SetValue(TransitionEasingProperty, value);
    }

    /// <summary>Gets or sets the transition style when switching content.</summary>
    public HeroContentTransitionMode TransitionMode
    {
        get => GetValue(TransitionModeProperty);
        set => SetValue(TransitionModeProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        StopTransition();

        _outgoingImage = e.NameScope.Find<Image>(OutgoingSnapshotPartName);
        _presenter = e.NameScope.Find<ContentControl>(ContentPresenterPartName);

        if (_presenter is not null)
        {
            _presenter.Content = Content;
            _presenter.Opacity = 1;
            _presenter.RenderTransform = null;
        }

        if (_outgoingImage is not null)
        {
            _outgoingImage.IsVisible = false;
            _outgoingImage.Source = null;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty && _presenter is not null)
        {
            OnContentChanged(change.OldValue, change.NewValue);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _frameRequested = false;
        StopTransition();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnContentChanged(object? oldContent, object? newContent)
    {
        if (_presenter is null)
        {
            return;
        }

        StopTransition();

        bool canAnimate = IsAnimationEnabled &&
            TransitionDuration > TimeSpan.Zero &&
            _outgoingImage is not null &&
            oldContent is not null &&
            TopLevel.GetTopLevel(this) is not null &&
            _presenter.Bounds.Width >= 1 &&
            _presenter.Bounds.Height >= 1;

        Size outgoingSize = _presenter.Bounds.Size;
        if (canAnimate)
        {
            _outgoingBitmap = CaptureOutgoingSnapshot(_presenter);
        }

        _presenter.Content = newContent;

        if (!canAnimate || _outgoingBitmap is null || _outgoingImage is null)
        {
            _outgoingBitmap?.Dispose();
            _outgoingBitmap = null;
            _presenter.Opacity = 1;
            _presenter.RenderTransform = null;
            return;
        }

        _outgoingImage.Width = outgoingSize.Width;
        _outgoingImage.Height = outgoingSize.Height;
        _outgoingImage.Source = _outgoingBitmap;
        _outgoingImage.IsVisible = true;

        _isTransitioning = true;
        _startTimestamp = Stopwatch.GetTimestamp();

        CompositionVisual? outgoingVisual = ElementComposition.GetElementVisual(_outgoingImage);
        CompositionVisual? presenterVisual = ElementComposition.GetElementVisual(_presenter);

        if (outgoingVisual is not null && presenterVisual is not null)
        {
            StartCompositionTransition(outgoingVisual, presenterVisual, outgoingSize, newContent is null);
            return;
        }

        _outgoingImage.Opacity = 1;
        _outgoingImage.RenderTransform = null;
        _presenter.Opacity = newContent is null ? 1 : 0;
        ApplyTransforms(0);
        RequestNextFrame();
    }

    private void StartCompositionTransition(
        CompositionVisual outgoingVisual,
        CompositionVisual presenterVisual,
        Size outgoingSize,
        bool isNewContentNull)
    {
        TimeSpan duration = TransitionDuration;
        Easing easing = TransitionEasing ?? new CubicEaseOut();
        Compositor compositor = outgoingVisual.Compositor;

        outgoingVisual.StopAnimation("Opacity");
        outgoingVisual.StopAnimation("Scale");
        outgoingVisual.StopAnimation("Translation");
        presenterVisual.StopAnimation("Opacity");
        presenterVisual.StopAnimation("Translation");

        outgoingVisual.Opacity = 1.0f;
        ScalarKeyFrameAnimation outFade = compositor.CreateScalarKeyFrameAnimation();
        outFade.Duration = duration;
        outFade.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        outFade.InsertKeyFrame(0.0f, 1.0f);
        outFade.InsertKeyFrame(1.0f, 0.0f, easing);
        outgoingVisual.StartAnimation("Opacity", outFade);

        if (!isNewContentNull)
        {
            presenterVisual.Opacity = 0.0f;
            ScalarKeyFrameAnimation inFade = compositor.CreateScalarKeyFrameAnimation();
            inFade.Duration = duration;
            inFade.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            inFade.InsertKeyFrame(0.0f, 0.0f);
            inFade.InsertKeyFrame(1.0f, 1.0f, easing);
            presenterVisual.StartAnimation("Opacity", inFade);
        }
        else
        {
            presenterVisual.Opacity = 1.0f;
        }

        switch (TransitionMode)
        {
            case HeroContentTransitionMode.FadeScale:
            {
                outgoingVisual.CenterPoint = new Vector3D(outgoingSize.Width * 0.5, outgoingSize.Height * 0.5, 0);
                outgoingVisual.Scale = new Vector3D(1, 1, 1);
                Vector3DKeyFrameAnimation outScale = compositor.CreateVector3DKeyFrameAnimation();
                outScale.Duration = duration;
                outScale.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                outScale.InsertKeyFrame(0.0f, new Vector3D(1, 1, 1));
                outScale.InsertKeyFrame(1.0f, new Vector3D(0.97, 0.97, 1), easing);
                outgoingVisual.StartAnimation("Scale", outScale);
                break;
            }
            case HeroContentTransitionMode.SlideVertical:
            {
                outgoingVisual.Translation = new Vector3D(0, 0, 0);
                Vector3DKeyFrameAnimation outSlide = compositor.CreateVector3DKeyFrameAnimation();
                outSlide.Duration = duration;
                outSlide.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                outSlide.InsertKeyFrame(0.0f, new Vector3D(0, 0, 0));
                outSlide.InsertKeyFrame(1.0f, new Vector3D(0, -14.0, 0), easing);
                outgoingVisual.StartAnimation("Translation", outSlide);

                presenterVisual.Translation = new Vector3D(0, 18.0, 0);
                Vector3DKeyFrameAnimation inSlide = compositor.CreateVector3DKeyFrameAnimation();
                inSlide.Duration = duration;
                inSlide.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                inSlide.InsertKeyFrame(0.0f, new Vector3D(0, 18.0, 0));
                inSlide.InsertKeyFrame(1.0f, new Vector3D(0, 0, 0), easing);
                presenterVisual.StartAnimation("Translation", inSlide);
                break;
            }
            case HeroContentTransitionMode.SlideHorizontal:
            {
                outgoingVisual.Translation = new Vector3D(0, 0, 0);
                Vector3DKeyFrameAnimation outSlide = compositor.CreateVector3DKeyFrameAnimation();
                outSlide.Duration = duration;
                outSlide.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                outSlide.InsertKeyFrame(0.0f, new Vector3D(0, 0, 0));
                outSlide.InsertKeyFrame(1.0f, new Vector3D(-20.0, 0, 0), easing);
                outgoingVisual.StartAnimation("Translation", outSlide);

                presenterVisual.Translation = new Vector3D(24.0, 0, 0);
                Vector3DKeyFrameAnimation inSlide = compositor.CreateVector3DKeyFrameAnimation();
                inSlide.Duration = duration;
                inSlide.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                inSlide.InsertKeyFrame(0.0f, new Vector3D(24.0, 0, 0));
                inSlide.InsertKeyFrame(1.0f, new Vector3D(0, 0, 0), easing);
                presenterVisual.StartAnimation("Translation", inSlide);
                break;
            }
        }

        DispatcherTimer timer = new() { Interval = duration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (ReferenceEquals(_completionTimer, timer))
            {
                StopTransition();
            }
        };
        _completionTimer = timer;
        timer.Start();
    }

    private void RequestNextFrame()
    {
        if (!_isTransitioning || _frameRequested)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            StopTransition();
            return;
        }

        _frameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan _)
    {
        _frameRequested = false;
        if (!_isTransitioning || _presenter is null || _outgoingImage is null)
        {
            return;
        }

        double elapsedMs = (Stopwatch.GetTimestamp() - _startTimestamp) * 1000.0 / Stopwatch.Frequency;
        double durationMs = Math.Max(1.0, TransitionDuration.TotalMilliseconds);
        double rawT = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
        Easing easing = TransitionEasing ?? new CubicEaseOut();
        double t = Math.Clamp(easing.Ease(rawT), 0.0, 1.0);

        _outgoingImage.Opacity = 1.0 - t;
        _presenter.Opacity = _presenter.Content is null ? 1.0 : t;
        ApplyTransforms(t);

        if (rawT >= 1.0)
        {
            StopTransition();
            return;
        }

        RequestNextFrame();
    }

    private void ApplyTransforms(double t)
    {
        if (_presenter is null || _outgoingImage is null)
        {
            return;
        }

        switch (TransitionMode)
        {
            case HeroContentTransitionMode.FadeScale:
            {
                double outScale = 1.0 - (0.03 * t);
                _outScale.ScaleX = outScale;
                _outScale.ScaleY = outScale;
                if (!ReferenceEquals(_outgoingImage.RenderTransform, _outScale))
                {
                    _outgoingImage.RenderTransform = _outScale;
                }

                // Keep the incoming presenter unscaled so Hero.TransformToVisual measures exact resting bounds.
                _presenter.RenderTransform = null;
                break;
            }
            case HeroContentTransitionMode.SlideVertical:
            {
                _outTranslate.X = 0;
                _outTranslate.Y = -14.0 * t;
                _inTranslate.X = 0;
                _inTranslate.Y = 18.0 * (1.0 - t);
                if (!ReferenceEquals(_outgoingImage.RenderTransform, _outTranslate))
                {
                    _outgoingImage.RenderTransform = _outTranslate;
                }

                if (!ReferenceEquals(_presenter.RenderTransform, _inTranslate))
                {
                    _presenter.RenderTransform = _inTranslate;
                }

                break;
            }
            case HeroContentTransitionMode.SlideHorizontal:
            {
                _outTranslate.X = -20.0 * t;
                _outTranslate.Y = 0;
                _inTranslate.X = 24.0 * (1.0 - t);
                _inTranslate.Y = 0;
                if (!ReferenceEquals(_outgoingImage.RenderTransform, _outTranslate))
                {
                    _outgoingImage.RenderTransform = _outTranslate;
                }

                if (!ReferenceEquals(_presenter.RenderTransform, _inTranslate))
                {
                    _presenter.RenderTransform = _inTranslate;
                }

                break;
            }
            default:
                _outgoingImage.RenderTransform = null;
                _presenter.RenderTransform = null;
                break;
        }
    }

    private void StopTransition()
    {
        _isTransitioning = false;
        _completionTimer?.Stop();
        _completionTimer = null;

        if (_presenter is not null)
        {
            CompositionVisual? presenterVisual = ElementComposition.GetElementVisual(_presenter);
            if (presenterVisual is not null)
            {
                presenterVisual.StopAnimation("Opacity");
                presenterVisual.StopAnimation("Translation");
                presenterVisual.Opacity = 1.0f;
                presenterVisual.Translation = new Vector3D(0, 0, 0);
            }

            _presenter.Opacity = 1;
            _presenter.RenderTransform = null;
        }

        if (_outgoingImage is not null)
        {
            CompositionVisual? outgoingVisual = ElementComposition.GetElementVisual(_outgoingImage);
            if (outgoingVisual is not null)
            {
                outgoingVisual.StopAnimation("Opacity");
                outgoingVisual.StopAnimation("Scale");
                outgoingVisual.StopAnimation("Translation");
                outgoingVisual.Opacity = 0.0f;
                outgoingVisual.Scale = new Vector3D(1, 1, 1);
                outgoingVisual.Translation = new Vector3D(0, 0, 0);
            }

            _outgoingImage.IsVisible = false;
            _outgoingImage.Opacity = 0;
            _outgoingImage.RenderTransform = null;
            _outgoingImage.Source = null;
        }

        _outgoingBitmap?.Dispose();
        _outgoingBitmap = null;
    }

    private RenderTargetBitmap? CaptureOutgoingSnapshot(Control presenter)
    {
        Hero? hiddenHero = null;
        double savedOpacity = 1.0;

        HeroHost? host = this.FindAncestorOfType<HeroHost>();
        if (host is { IsAnimationEnabled: true })
        {
            List<Hero> visibleHeroes =
            [
                .. presenter.GetVisualDescendants()
                    .OfType<Hero>()
                    .Where(h => h.IsHeroEnabled && h.IsEffectivelyVisible && h.Tag is not null),
            ];

            if (visibleHeroes.Count == 1)
            {
                hiddenHero = visibleHeroes[0];
                savedOpacity = hiddenHero.Opacity;
                hiddenHero.Opacity = 0;
            }
        }

        try
        {
            return CapturePresenterSnapshot(presenter);
        }
        finally
        {
            if (hiddenHero is not null)
            {
                hiddenHero.Opacity = savedOpacity;
            }
        }
    }

    private RenderTargetBitmap? CapturePresenterSnapshot(Control presenter)
    {
        double area = presenter.Bounds.Width * presenter.Bounds.Height;
        double rawScale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        double scale = area > 250_000 ? 1.0 : Math.Min(rawScale, 1.5);
        int pixelWidth = Math.Max(1, (int)Math.Ceiling(presenter.Bounds.Width * scale));
        int pixelHeight = Math.Max(1, (int)Math.Ceiling(presenter.Bounds.Height * scale));

        try
        {
            RenderTargetBitmap bitmap = new(new PixelSize(pixelWidth, pixelHeight), new Vector(96 * scale, 96 * scale));
            bitmap.Render(presenter);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
