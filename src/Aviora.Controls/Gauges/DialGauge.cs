using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a value within a range on a radial dial.
/// </summary>
public class DialGauge : Control
{
    private const double StartAngle = -15;
    private const double SweepAngle = 210;

    private readonly Dictionary<int, FormattedText> _tickLabelCache = [];

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<DialGauge, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<DialGauge, double>(nameof(Maximum), 100.0);
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<DialGauge, double>(nameof(Value));
    public static readonly StyledProperty<bool> ShowTicksProperty =
        AvaloniaProperty.Register<DialGauge, bool>(nameof(ShowTicks), true);
    public static readonly StyledProperty<int> TickCountProperty =
        AvaloniaProperty.Register<DialGauge, int>(nameof(TickCount), 20);
    public static readonly StyledProperty<IBrush> TickBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(TickBrush),
            AvioraControlPalette.Accent);
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
    public static readonly StyledProperty<bool> ShowTickLabelsProperty =
        AvaloniaProperty.Register<DialGauge, bool>(nameof(ShowTickLabels), true);
    public static readonly StyledProperty<int> TickLabelIntervalProperty =
        AvaloniaProperty.Register<DialGauge, int>(nameof(TickLabelInterval), 5);
    public static readonly StyledProperty<string?> TickLabelFormatProperty =
        AvaloniaProperty.Register<DialGauge, string?>(nameof(TickLabelFormat), "0.##");
    public static readonly StyledProperty<Func<double, string?>?> TickLabelFormatterProperty =
        AvaloniaProperty.Register<DialGauge, Func<double, string?>?>(nameof(TickLabelFormatter));
    public static readonly StyledProperty<IBrush> TickLabelBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(TickLabelBrush),
            AvioraControlPalette.TextMuted);
    public static readonly StyledProperty<double> TickLabelFontSizeProperty =
        AvaloniaProperty.Register<DialGauge, double>(nameof(TickLabelFontSize), 11.0);
    public static readonly StyledProperty<FontFamily> TickLabelFontFamilyProperty =
        AvaloniaProperty.Register<DialGauge, FontFamily>(nameof(TickLabelFontFamily), FontFamily.Default);
    public static readonly StyledProperty<FontWeight> TickLabelFontWeightProperty =
        AvaloniaProperty.Register<DialGauge, FontWeight>(nameof(TickLabelFontWeight), FontWeight.Normal);
    public static readonly StyledProperty<IBrush> NeedleBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(NeedleBrush),
            AvioraControlPalette.AccentStrong);
    public static readonly StyledProperty<IBrush> PivotBrushProperty =
        AvaloniaProperty.Register<DialGauge, IBrush>(
            nameof(PivotBrush),
            AvioraControlPalette.AccentStrong);

    static DialGauge()
    {
        AffectsRender<DialGauge>(
            BoundsProperty,
            MinimumProperty,
            MaximumProperty,
            ValueProperty,
            ShowTicksProperty,
            TickCountProperty,
            TickBrushProperty,
            TickColorModeProperty,
            LowRangeBrushProperty,
            MediumRangeBrushProperty,
            HighRangeBrushProperty,
            ShowTickLabelsProperty,
            TickLabelIntervalProperty,
            TickLabelFormatProperty,
            TickLabelFormatterProperty,
            TickLabelBrushProperty,
            TickLabelFontSizeProperty,
            TickLabelFontFamilyProperty,
            TickLabelFontWeightProperty,
            NeedleBrushProperty,
            PivotBrushProperty);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool ShowTicks { get => GetValue(ShowTicksProperty); set => SetValue(ShowTicksProperty, value); }
    public int TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }
    public IBrush TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
    public DialGaugeTickColorMode TickColorMode { get => GetValue(TickColorModeProperty); set => SetValue(TickColorModeProperty, value); }
    public IBrush LowRangeBrush { get => GetValue(LowRangeBrushProperty); set => SetValue(LowRangeBrushProperty, value); }
    public IBrush MediumRangeBrush { get => GetValue(MediumRangeBrushProperty); set => SetValue(MediumRangeBrushProperty, value); }
    public IBrush HighRangeBrush { get => GetValue(HighRangeBrushProperty); set => SetValue(HighRangeBrushProperty, value); }
    public bool ShowTickLabels { get => GetValue(ShowTickLabelsProperty); set => SetValue(ShowTickLabelsProperty, value); }
    public int TickLabelInterval { get => GetValue(TickLabelIntervalProperty); set => SetValue(TickLabelIntervalProperty, value); }
    public string? TickLabelFormat { get => GetValue(TickLabelFormatProperty); set => SetValue(TickLabelFormatProperty, value); }
    public Func<double, string?>? TickLabelFormatter { get => GetValue(TickLabelFormatterProperty); set => SetValue(TickLabelFormatterProperty, value); }
    public IBrush TickLabelBrush { get => GetValue(TickLabelBrushProperty); set => SetValue(TickLabelBrushProperty, value); }
    public double TickLabelFontSize { get => GetValue(TickLabelFontSizeProperty); set => SetValue(TickLabelFontSizeProperty, value); }
    public FontFamily TickLabelFontFamily { get => GetValue(TickLabelFontFamilyProperty); set => SetValue(TickLabelFontFamilyProperty, value); }
    public FontWeight TickLabelFontWeight { get => GetValue(TickLabelFontWeightProperty); set => SetValue(TickLabelFontWeightProperty, value); }
    public IBrush NeedleBrush { get => GetValue(NeedleBrushProperty); set => SetValue(NeedleBrushProperty, value); }
    public IBrush PivotBrush { get => GetValue(PivotBrushProperty); set => SetValue(PivotBrushProperty, value); }

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

    internal static double NormalizeValue(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value) || !double.IsFinite(minimum) ||
            !double.IsFinite(maximum) || maximum <= minimum)
        {
            return 0;
        }

        return Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);
    }

    internal string FormatTickLabel(double value)
    {
        if (TickLabelFormatter != null)
        {
            return TickLabelFormatter(value) ?? string.Empty;
        }

        try
        {
            return value.ToString(TickLabelFormat, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }
    }

    internal static bool ShouldDrawTickLabel(int index, int tickCount, int interval)
    {
        interval = Math.Max(1, interval);
        return index == 0 || index == tickCount || index % interval == 0;
    }

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

    private static Point PointOnCircle(
        double centerX,
        double centerY,
        double radius,
        double angleDegrees)
    {
        double radians = (180 - angleDegrees) * Math.PI / 180;
        return new Point(
            centerX + (radius * Math.Cos(radians)),
            centerY - (radius * Math.Sin(radians)));
    }
}
