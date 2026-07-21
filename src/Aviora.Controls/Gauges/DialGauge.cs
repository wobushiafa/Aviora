using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a value within a range on a radial dial.
/// </summary>
public class DialGauge : GaugeBase
{
    private const double StartAngle = -15;
    private const double SweepAngle = 210;

    private readonly Dictionary<int, FormattedText> _tickLabelCache = [];

    public static readonly StyledProperty<DialGaugeTickColorMode> TickColorModeProperty =
        AvaloniaProperty.Register<DialGauge, DialGaugeTickColorMode>(nameof(TickColorMode));
    public static readonly StyledProperty<IBrush> LowRangeBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(LowRangeBrush),
            AvioraControlPalette.Accent);
    public static readonly StyledProperty<IBrush> MediumRangeBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(MediumRangeBrush),
            AvioraControlPalette.Warning);
    public static readonly StyledProperty<IBrush> HighRangeBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(HighRangeBrush),
            AvioraControlPalette.Danger);
    public static readonly StyledProperty<int> TickLabelIntervalProperty =
        AvaloniaProperty.Register<DialGauge, int>(nameof(TickLabelInterval), 5);

    static DialGauge()
    {
        AffectsRender<DialGauge>(
            TickColorModeProperty,
            LowRangeBrushProperty,
            MediumRangeBrushProperty,
            HighRangeBrushProperty,
            TickLabelIntervalProperty
            );
    }

    public DialGaugeTickColorMode TickColorMode { get => GetValue(TickColorModeProperty); set => SetValue(TickColorModeProperty, value); }
    public IBrush LowRangeBrush { get => GetValue(LowRangeBrushProperty); set => SetValue(LowRangeBrushProperty, value); }
    public IBrush MediumRangeBrush { get => GetValue(MediumRangeBrushProperty); set => SetValue(MediumRangeBrushProperty, value); }
    public IBrush HighRangeBrush { get => GetValue(HighRangeBrushProperty); set => SetValue(HighRangeBrushProperty, value); }
    public int TickLabelInterval { get => GetValue(TickLabelIntervalProperty); set => SetValue(TickLabelIntervalProperty, value); }
    internal new static double NormalizeValue(double value, double minimum, double maximum) => GaugeBase.NormalizeValue(value, minimum, maximum);
    internal new string FormatTickLabel(double value) => base.FormatTickLabel(value);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MinimumProperty ||
            change.Property == MaximumProperty ||
            change.Property == TickCountProperty ||
            change.Property == TickLabelIntervalProperty ||
            change.Property == TickLabelFormatProperty ||
            change.Property == TickLabelFormatterProperty ||
            change.Property == TickLabelBrushProperty ||
            change.Property == TickLabelFontSizeProperty ||
            change.Property == TickLabelFontFamilyProperty ||
            change.Property == TickLabelFontWeightProperty)
        {
            _tickLabelCache.Clear();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        const double defaultWidth = 240;
        const double defaultHeight = 180;
        return new Size(
            double.IsInfinity(availableSize.Width) ? defaultWidth : Math.Min(defaultWidth, Math.Max(0, availableSize.Width)),
            double.IsInfinity(availableSize.Height) ? defaultHeight : Math.Min(defaultHeight, Math.Max(0, availableSize.Height)));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width < 48 || height < 48)
        {
            return;
        }

        double centerX = width / 2;
        double centerY = Math.Min(height - 16, height * 0.72);
        double labelSpace = ShowTickLabels ? Math.Max(12, TickLabelFontSize + 5) : 4;
        double radius = Math.Min((width / 2) - labelSpace - 4, centerY - labelSpace - 4);
        if (radius <= 12)
        {
            return;
        }

        int tickCount = Math.Clamp(TickCount, 0, 200);
        if (ShowTicks && tickCount > 0)
        {
            DrawScale(context, centerX, centerY, radius, tickCount);
        }

        DrawNeedle(context, centerX, centerY, radius);
    }

    internal new static bool ShouldDrawTickLabel(int index, int tickCount, int interval) =>
        GaugeBase.ShouldDrawTickLabel(index, tickCount, interval);

    private void DrawScale(
        DrawingContext context,
        double centerX,
        double centerY,
        double radius,
        int tickCount)
    {
        var arcGeometry = new StreamGeometry();
        using (StreamGeometryContext geometryContext = arcGeometry.Open())
        {
            geometryContext.BeginFigure(PointOnCircle(centerX, centerY, radius, StartAngle), false);
            geometryContext.ArcTo(
                PointOnCircle(centerX, centerY, radius, StartAngle + SweepAngle),
                new Size(radius, radius),
                0,
                true,
                SweepDirection.Clockwise);
        }

        context.DrawGeometry(null, new Pen(TickBrush, 1), arcGeometry);

        double labelRadius = radius - Math.Max(15, TickLabelFontSize + 7);
        for (int index = 0; index <= tickCount; index++)
        {
            double progress = index / (double)tickCount;
            double angle = StartAngle + (progress * SweepAngle);
            bool isMajor = ShouldDrawTickLabel(index, tickCount, TickLabelInterval);
            double tickLength = isMajor ? 9 : 5;
            var pen = new Pen(ResolveTickBrush(progress), isMajor ? 2 : 1, lineCap: PenLineCap.Round);
            context.DrawLine(
                pen,
                PointOnCircle(centerX, centerY, radius, angle),
                PointOnCircle(centerX, centerY, radius - tickLength, angle));

            if (ShowTickLabels && isMajor)
            {
                DrawTickLabel(context, index, tickCount, centerX, centerY, labelRadius, angle);
            }
        }
    }

    private void DrawTickLabel(
        DrawingContext context,
        int index,
        int tickCount,
        double centerX,
        double centerY,
        double radius,
        double angle)
    {
        if (!_tickLabelCache.TryGetValue(index, out FormattedText? text))
        {
            double progress = index / (double)tickCount;
            double value = Minimum + (progress * (Maximum - Minimum));
            text = new FormattedText(
                FormatTickLabel(value),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(TickLabelFontFamily, FontStyle.Normal, TickLabelFontWeight),
                Math.Max(1, TickLabelFontSize),
                TickLabelBrush);
            _tickLabelCache[index] = text;
        }

        Point position = PointOnCircle(centerX, centerY, radius, angle);
        context.DrawText(text, new Point(position.X - (text.Width / 2), position.Y - (text.Height / 2)));
    }

    private IBrush ResolveTickBrush(double progress)
    {
        if (TickColorMode != DialGaugeTickColorMode.Range)
        {
            return TickBrush;
        }

        return progress switch
        {
            < 0.6 => LowRangeBrush,
            < 0.8 => MediumRangeBrush,
            _ => HighRangeBrush,
        };
    }

    private void DrawNeedle(
        DrawingContext context,
        double centerX,
        double centerY,
        double radius)
    {
        double angle = StartAngle + (NormalizeValue(Value, Minimum, Maximum) * SweepAngle);
        double radians = (180 - angle) * Math.PI / 180;
        double unitX = Math.Cos(radians);
        double unitY = -Math.Sin(radians);
        double perpendicularX = Math.Sin(radians);
        double perpendicularY = Math.Cos(radians);
        double needleLength = Math.Max(8, radius - 14);
        double halfBase = Math.Clamp(radius * 0.025, 1.5, 3.5);

        Point tip = new(centerX + (needleLength * unitX), centerY + (needleLength * unitY));
        Point left = new(centerX - (halfBase * perpendicularX), centerY - (halfBase * perpendicularY));
        Point right = new(centerX + (halfBase * perpendicularX), centerY + (halfBase * perpendicularY));
        var needleGeometry = new StreamGeometry();
        using (StreamGeometryContext geometryContext = needleGeometry.Open())
        {
            geometryContext.BeginFigure(left, true);
            geometryContext.LineTo(tip);
            geometryContext.LineTo(right);
            geometryContext.EndFigure(true);
        }

        context.DrawGeometry(NeedleBrush, null, needleGeometry);
        double pivotRadius = Math.Clamp(radius * 0.055, 4, 7);
        context.DrawEllipse(PivotBrush, null, new Point(centerX, centerY), pivotRadius, pivotRadius);
    }

}
