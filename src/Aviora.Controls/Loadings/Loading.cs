using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Aviora.Controls;

/// <summary>
/// Displays an animated loading indicator with built-in visuals or custom content.
/// </summary>
public class Loading : ContentControl
{
    /// <summary>Defines the <see cref="IndicatorStyle"/> property.</summary>
    public static readonly StyledProperty<LoadingIndicatorStyle> IndicatorStyleProperty =
        AvaloniaProperty.Register<Loading, LoadingIndicatorStyle>(nameof(IndicatorStyle));

    /// <summary>Defines the <see cref="IndicatorBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty =
        AvaloniaProperty.Register<Loading, IBrush?>(nameof(IndicatorBrush), AvioraControlPalette.Accent);

    /// <summary>Defines the <see cref="TrackBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<Loading, IBrush?>(nameof(TrackBrush), AvioraControlPalette.Subtle);

    /// <summary>Defines the <see cref="StrokeThickness"/> property.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Loading, double>(nameof(StrokeThickness), 3, validate: value => value > 0);

    /// <summary>Defines the <see cref="AnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<Loading, TimeSpan>(
            nameof(AnimationDuration),
            TimeSpan.FromMilliseconds(900),
            validate: value => value > TimeSpan.Zero);

    /// <summary>Defines the <see cref="IsActive"/> property.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<Loading, bool>(nameof(IsActive), true);

    private bool _animationFrameRequested;

    static Loading()
    {
        AffectsRender<Loading>(
            IndicatorStyleProperty,
            IndicatorBrushProperty,
            TrackBrushProperty,
            StrokeThicknessProperty,
            AnimationDurationProperty,
            IsActiveProperty,
            ContentProperty);
        IsActiveProperty.Changed.AddClassHandler<Loading>((loading, _) => loading.OnAnimationStateChanged());
        AnimationDurationProperty.Changed.AddClassHandler<Loading>((loading, _) => loading.RequestAnimationFrame());
        ContentProperty.Changed.AddClassHandler<Loading>((loading, _) => loading.OnAnimationStateChanged());
    }

    /// <summary>Gets or sets the built-in indicator visual.</summary>
    public LoadingIndicatorStyle IndicatorStyle
    {
        get => GetValue(IndicatorStyleProperty);
        set => SetValue(IndicatorStyleProperty, value);
    }

    /// <summary>Gets or sets the brush used to draw the active indicator.</summary>
    public IBrush? IndicatorBrush
    {
        get => GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    /// <summary>Gets or sets the brush used to draw inactive indicator tracks.</summary>
    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    /// <summary>Gets or sets the stroke thickness of built-in indicators.</summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>Gets or sets the time required for one animation cycle.</summary>
    public TimeSpan AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    /// <summary>Gets or sets whether the indicator is visible and animating.</summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Content is not null)
        {
            return base.MeasureOverride(availableSize);
        }

        const double defaultSize = 32;
        return new Size(
            double.IsInfinity(availableSize.Width) ? defaultSize : Math.Min(defaultSize, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? defaultSize : Math.Min(defaultSize, availableSize.Height));
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsActive || Content is not null || IndicatorBrush is null)
        {
            return;
        }

        double progress = GetAnimationProgress();
        switch (IndicatorStyle)
        {
            case LoadingIndicatorStyle.Ring:
                DrawRing(context, progress);
                break;
            case LoadingIndicatorStyle.Dots:
                DrawDots(context, progress);
                break;
            case LoadingIndicatorStyle.Pulse:
                DrawPulse(context, progress);
                break;
            case LoadingIndicatorStyle.Bars:
                DrawBars(context, progress);
                break;
            case LoadingIndicatorStyle.Wave:
                DrawWave(context, progress);
                break;
            case LoadingIndicatorStyle.Orbit:
                DrawOrbit(context, progress);
                break;
            case LoadingIndicatorStyle.DoubleRing:
                DrawDoubleRing(context, progress);
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RequestAnimationFrame();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _animationFrameRequested = false;
        base.OnDetachedFromVisualTree(e);
    }

    private double GetAnimationProgress()
    {
        double duration = Math.Max(1, AnimationDuration.TotalMilliseconds);
        return (TimeProvider.System.GetTimestamp() / (double)TimeProvider.System.TimestampFrequency * 1000 / duration) % 1;
    }

    private void DrawRing(DrawingContext context, double progress)
    {
        double thickness = GetEffectiveThickness();
        Point center = GetCenter();
        double radius = GetRadius(thickness);
        if (radius <= 0)
        {
            return;
        }

        if (TrackBrush is not null)
        {
            using (context.PushOpacity(0.45))
            {
                context.DrawEllipse(null, new Pen(TrackBrush, thickness), center, radius, radius);
            }
        }

        (double startAngle, double sweep) = CalculateRingArc(progress);
        PathGeometry geometry = CreateRingArcGeometry(center, radius, startAngle, sweep);
        context.DrawGeometry(null, new Pen(IndicatorBrush, thickness, lineCap: PenLineCap.Round), geometry);
    }

    internal static PathGeometry CreateRingArcGeometry(Point center, double radius, double startAngle, double sweep) =>
        RingDrawing.CreateArcGeometry(center, radius, startAngle, sweep);

    internal static (double StartAngle, double SweepAngle) CalculateRingArc(double progress) =>
        RingDrawing.CalculateIndeterminateArc(progress);

    private void DrawDots(DrawingContext context, double progress)
    {
        double size = Math.Min(Bounds.Width, Bounds.Height);
        double radius = Math.Max(1, Math.Min(size / 9, StrokeThickness));
        double spacing = radius * 3;
        Point center = GetCenter();

        for (int index = 0; index < 3; index++)
        {
            double phase = (progress - (index * 0.16) + 1) % 1;
            double wave = (Math.Sin(phase * Math.Tau) + 1) / 2;
            double dotRadius = radius * (0.72 + (wave * 0.28));
            double offsetY = -wave * radius * 1.5;
            using (context.PushOpacity(0.4 + (wave * 0.6)))
            {
                context.DrawEllipse(
                    IndicatorBrush,
                    null,
                    new Point(center.X + ((index - 1) * spacing), center.Y + offsetY),
                    dotRadius,
                    dotRadius);
            }
        }
    }

    private void DrawPulse(DrawingContext context, double progress)
    {
        double thickness = GetEffectiveThickness();
        Point center = GetCenter();
        double maximumRadius = GetRadius(thickness);
        if (maximumRadius <= 0)
        {
            return;
        }

        for (int index = 0; index < 2; index++)
        {
            double phase = (progress + (index * 0.5)) % 1;
            double radius = maximumRadius * (0.25 + (phase * 0.75));
            using (context.PushOpacity(1 - phase))
            {
                context.DrawEllipse(null, new Pen(IndicatorBrush, thickness), center, radius, radius);
            }
        }

        context.DrawEllipse(IndicatorBrush, null, center, thickness, thickness);
    }

    private void DrawBars(DrawingContext context, double progress)
    {
        double width = Math.Min(Bounds.Width, Bounds.Height);
        double thickness = Math.Min(GetEffectiveThickness(), Math.Max(1, width / 8));
        double spacing = thickness * 1.65;
        Point center = GetCenter();

        for (int index = 0; index < 5; index++)
        {
            double phase = (progress - (index * 0.1) + 1) % 1;
            double wave = (Math.Sin(phase * Math.Tau) + 1) / 2;
            double halfHeight = Math.Max(thickness, width * (0.16 + (wave * 0.24)));
            double x = center.X + ((index - 2) * spacing);
            context.DrawLine(
                new Pen(IndicatorBrush, thickness, lineCap: PenLineCap.Round),
                new Point(x, center.Y - halfHeight),
                new Point(x, center.Y + halfHeight));
        }
    }

    private void DrawWave(DrawingContext context, double progress)
    {
        double size = Math.Min(Bounds.Width, Bounds.Height);
        double radius = Math.Max(1, Math.Min(size / 12, StrokeThickness));
        double spacing = radius * 2.6;
        Point center = GetCenter();

        for (int index = 0; index < 5; index++)
        {
            double phase = (progress - (index * 0.1) + 1) % 1;
            double wave = Math.Sin(phase * Math.Tau);
            double opacity = 0.55 + (((wave + 1) / 2) * 0.45);
            using (context.PushOpacity(opacity))
            {
                context.DrawEllipse(
                    IndicatorBrush,
                    null,
                    new Point(center.X + ((index - 2) * spacing), center.Y - (wave * size * 0.16)),
                    radius,
                    radius);
            }
        }
    }

    private void DrawOrbit(DrawingContext context, double progress)
    {
        double size = Math.Min(Bounds.Width, Bounds.Height);
        double dotRadius = Math.Max(1, Math.Min(StrokeThickness, size / 10));
        double orbitRadius = Math.Max(0, (size / 2) - dotRadius);
        Point center = GetCenter();

        for (int index = 0; index < 3; index++)
        {
            double phase = (progress + (index / 3d)) % 1;
            double angle = phase * Math.Tau;
            using (context.PushOpacity(1 - (index * 0.25)))
            {
                context.DrawEllipse(
                    IndicatorBrush,
                    null,
                    new Point(center.X + (Math.Cos(angle) * orbitRadius), center.Y + (Math.Sin(angle) * orbitRadius)),
                    dotRadius * (1 - (index * 0.12)),
                    dotRadius * (1 - (index * 0.12)));
            }
        }
    }

    private void DrawDoubleRing(DrawingContext context, double progress)
    {
        double thickness = GetEffectiveThickness();
        Point center = GetCenter();
        double outerRadius = GetRadius(thickness);
        double innerRadius = outerRadius - (thickness * 2.2);
        if (innerRadius <= 0)
        {
            DrawRing(context, progress);
            return;
        }

        PathGeometry outer = RingDrawing.CreateArcGeometry(center, outerRadius, (progress * 360) - 90, 135);
        PathGeometry inner = RingDrawing.CreateArcGeometry(center, innerRadius, (-progress * 360) - 90, 110);
        var pen = new Pen(IndicatorBrush, thickness, lineCap: PenLineCap.Round);
        context.DrawGeometry(null, pen, outer);
        using (context.PushOpacity(0.65))
        {
            context.DrawGeometry(null, pen, inner);
        }
    }

    private void OnAnimationStateChanged()
    {
        InvalidateVisual();
        RequestAnimationFrame();
    }

    private void RequestAnimationFrame()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (!IsActive || Content is not null || _animationFrameRequested || topLevel is null)
        {
            return;
        }

        _animationFrameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan _)
    {
        _animationFrameRequested = false;
        if (!IsActive || Content is not null)
        {
            return;
        }

        InvalidateVisual();
        RequestAnimationFrame();
    }

    private Point GetCenter() => new(Bounds.Width / 2, Bounds.Height / 2);

    private double GetEffectiveThickness() =>
        Math.Min(StrokeThickness, Math.Max(1, Math.Min(Bounds.Width, Bounds.Height) / 4));

    private double GetRadius(double thickness) =>
        Math.Max(0, (Math.Min(Bounds.Width, Bounds.Height) - thickness) / 2);

}
