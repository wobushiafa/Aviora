using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Aviora.Controls;

/// <summary>
/// Displays determinate progress as a circular arc or an animated ring for indeterminate progress.
/// </summary>
public class ProgressRing : RangeBase
{
    /// <summary>Defines the <see cref="IndicatorBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty =
        AvaloniaProperty.Register<ProgressRing, IBrush?>(nameof(IndicatorBrush), AvioraControlPalette.Accent);

    /// <summary>Defines the <see cref="TrackBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<ProgressRing, IBrush?>(nameof(TrackBrush), AvioraControlPalette.Subtle);

    /// <summary>Defines the <see cref="StrokeThickness"/> property.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(StrokeThickness), 4, validate: value => value > 0);

    /// <summary>Defines the <see cref="IsIndeterminate"/> property.</summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsIndeterminate));

    /// <summary>Defines the <see cref="AnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<ProgressRing, TimeSpan>(
            nameof(AnimationDuration),
            TimeSpan.FromMilliseconds(900),
            validate: value => value > TimeSpan.Zero);

    private bool _animationFrameRequested;

    static ProgressRing()
    {
        AffectsRender<ProgressRing>(
            ValueProperty,
            MinimumProperty,
            MaximumProperty,
            IndicatorBrushProperty,
            TrackBrushProperty,
            StrokeThicknessProperty,
            IsIndeterminateProperty,
            AnimationDurationProperty);
        IsIndeterminateProperty.Changed.AddClassHandler<ProgressRing>((progress, _) => progress.OnAnimationStateChanged());
        AnimationDurationProperty.Changed.AddClassHandler<ProgressRing>((progress, _) => progress.RequestAnimationFrame());
    }

    /// <summary>Gets or sets the brush used to draw the progress arc.</summary>
    public IBrush? IndicatorBrush
    {
        get => GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    /// <summary>Gets or sets the brush used to draw the circular track.</summary>
    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    /// <summary>Gets or sets the thickness of the track and progress arc.</summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>Gets or sets whether the control displays continuous indeterminate progress.</summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Gets or sets the duration of one indeterminate animation cycle.</summary>
    public TimeSpan AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        const double defaultSize = 48;
        return new Size(
            double.IsInfinity(availableSize.Width) ? defaultSize : Math.Min(defaultSize, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? defaultSize : Math.Min(defaultSize, availableSize.Height));
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double thickness = Math.Min(StrokeThickness, Math.Max(1, Math.Min(Bounds.Width, Bounds.Height) / 4));
        double radius = Math.Max(0, (Math.Min(Bounds.Width, Bounds.Height) - thickness) / 2);
        if (radius <= 0)
        {
            return;
        }

        Point center = new(Bounds.Width / 2, Bounds.Height / 2);
        if (TrackBrush is not null)
        {
            context.DrawEllipse(null, new Pen(TrackBrush, thickness), center, radius, radius);
        }

        if (IndicatorBrush is null)
        {
            return;
        }

        if (IsIndeterminate)
        {
            (double start, double sweep) = RingDrawing.CalculateIndeterminateArc(GetAnimationProgress());
            DrawArc(context, center, radius, thickness, start, sweep);
            return;
        }

        double range = Maximum - Minimum;
        double progress = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        if (progress >= 1)
        {
            context.DrawEllipse(null, new Pen(IndicatorBrush, thickness), center, radius, radius);
        }
        else if (progress > 0)
        {
            DrawArc(context, center, radius, thickness, -90, progress * 360);
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

    private void DrawArc(DrawingContext context, Point center, double radius, double thickness, double start, double sweep)
    {
        PathGeometry geometry = RingDrawing.CreateArcGeometry(center, radius, start, sweep);
        context.DrawGeometry(null, new Pen(IndicatorBrush, thickness, lineCap: PenLineCap.Round), geometry);
    }

    private double GetAnimationProgress()
    {
        double duration = Math.Max(1, AnimationDuration.TotalMilliseconds);
        return (TimeProvider.System.GetTimestamp() / (double)TimeProvider.System.TimestampFrequency * 1000 / duration) % 1;
    }

    private void OnAnimationStateChanged()
    {
        InvalidateVisual();
        RequestAnimationFrame();
    }

    private void RequestAnimationFrame()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (!IsIndeterminate || _animationFrameRequested || topLevel is null)
        {
            return;
        }

        _animationFrameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan _)
    {
        _animationFrameRequested = false;
        if (!IsIndeterminate)
        {
            return;
        }

        InvalidateVisual();
        RequestAnimationFrame();
    }
}
