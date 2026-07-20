using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a value within a range as a vertical thermometer.
/// </summary>
public class Thermometer : Control
{
    private readonly Dictionary<int, FormattedText> _tickLabelCache = [];
    private double _tickLabelCacheWidth;
    private bool _tickLabelCacheValid;

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<Thermometer, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<Thermometer, double>(nameof(Maximum), 100.0);
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<Thermometer, double>(nameof(Value));
    public static readonly StyledProperty<bool> ShowTicksProperty =
        AvaloniaProperty.Register<Thermometer, bool>(nameof(ShowTicks), true);
    public static readonly StyledProperty<int> TickCountProperty =
        AvaloniaProperty.Register<Thermometer, int>(nameof(TickCount), 10);
    public static readonly StyledProperty<bool> ShowTickLabelsProperty =
        AvaloniaProperty.Register<Thermometer, bool>(nameof(ShowTickLabels));
    public static readonly StyledProperty<int> TickLabelIntervalProperty =
        AvaloniaProperty.Register<Thermometer, int>(nameof(TickLabelInterval), 1);
    public static readonly StyledProperty<string?> TickLabelFormatProperty =
        AvaloniaProperty.Register<Thermometer, string?>(nameof(TickLabelFormat), "0.##");
    public static readonly StyledProperty<Func<double, string?>?> TickLabelFormatterProperty =
        AvaloniaProperty.Register<Thermometer, Func<double, string?>?>(nameof(TickLabelFormatter));
    public static readonly StyledProperty<IBrush> TickLabelBrushProperty =
        AvaloniaProperty.Register<Thermometer, IBrush>(
            nameof(TickLabelBrush),
            new ImmutableSolidColorBrush(Color.Parse("#64748B")));
    public static readonly StyledProperty<double> TickLabelFontSizeProperty =
        AvaloniaProperty.Register<Thermometer, double>(nameof(TickLabelFontSize), 10.0);
    public static readonly StyledProperty<double> TickLabelSpacingProperty =
        AvaloniaProperty.Register<Thermometer, double>(nameof(TickLabelSpacing), 4.0);
    public static readonly StyledProperty<FontFamily> TickLabelFontFamilyProperty =
        AvaloniaProperty.Register<Thermometer, FontFamily>(nameof(TickLabelFontFamily), FontFamily.Default);
    public static readonly StyledProperty<FontWeight> TickLabelFontWeightProperty =
        AvaloniaProperty.Register<Thermometer, FontWeight>(nameof(TickLabelFontWeight), FontWeight.Normal);
    public static readonly StyledProperty<IBrush> TubeBrushProperty =
        AvaloniaProperty.Register<Thermometer, IBrush>(
            nameof(TubeBrush),
            new ImmutableSolidColorBrush(Color.Parse("#E2E8F0")));
    public static readonly StyledProperty<IBrush> TickBrushProperty =
        AvaloniaProperty.Register<Thermometer, IBrush>(
            nameof(TickBrush),
            new ImmutableSolidColorBrush(Color.Parse("#94A3B8")));
    public static readonly StyledProperty<IBrush> LiquidBrushProperty =
        AvaloniaProperty.Register<Thermometer, IBrush>(
            nameof(LiquidBrush),
            new ImmutableSolidColorBrush(Color.Parse("#0EA5E9")));
    public static readonly StyledProperty<LiquidBrushMappingMode> LiquidBrushMappingModeProperty =
        AvaloniaProperty.Register<Thermometer, LiquidBrushMappingMode>(
            nameof(LiquidBrushMappingMode),
            global::Aviora.Controls.LiquidBrushMappingMode.FilledArea);
    public static readonly StyledProperty<IBrush> GlareBrushProperty =
        AvaloniaProperty.Register<Thermometer, IBrush>(
            nameof(GlareBrush),
            new ImmutableSolidColorBrush(Color.Parse("#B3FFFFFF")));

    static Thermometer()
    {
        AffectsRender<Thermometer>(
            BoundsProperty,
            MinimumProperty,
            MaximumProperty,
            ValueProperty,
            ShowTicksProperty,
            TickCountProperty,
            ShowTickLabelsProperty,
            TickLabelIntervalProperty,
            TickLabelFormatProperty,
            TickLabelFormatterProperty,
            TickLabelBrushProperty,
            TickLabelFontSizeProperty,
            TickLabelSpacingProperty,
            TickLabelFontFamilyProperty,
            TickLabelFontWeightProperty,
            TubeBrushProperty,
            TickBrushProperty,
            LiquidBrushProperty,
            LiquidBrushMappingModeProperty,
            GlareBrushProperty);
        AffectsMeasure<Thermometer>(
            MinimumProperty,
            MaximumProperty,
            ShowTicksProperty,
            ShowTickLabelsProperty,
            TickCountProperty,
            TickLabelIntervalProperty,
            TickLabelFormatProperty,
            TickLabelFormatterProperty,
            TickLabelFontSizeProperty,
            TickLabelSpacingProperty,
            TickLabelFontFamilyProperty,
            TickLabelFontWeightProperty);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool ShowTicks { get => GetValue(ShowTicksProperty); set => SetValue(ShowTicksProperty, value); }
    public int TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }
    public bool ShowTickLabels { get => GetValue(ShowTickLabelsProperty); set => SetValue(ShowTickLabelsProperty, value); }
    public int TickLabelInterval { get => GetValue(TickLabelIntervalProperty); set => SetValue(TickLabelIntervalProperty, value); }
    public string? TickLabelFormat { get => GetValue(TickLabelFormatProperty); set => SetValue(TickLabelFormatProperty, value); }
    public Func<double, string?>? TickLabelFormatter { get => GetValue(TickLabelFormatterProperty); set => SetValue(TickLabelFormatterProperty, value); }
    public IBrush TickLabelBrush { get => GetValue(TickLabelBrushProperty); set => SetValue(TickLabelBrushProperty, value); }
    public double TickLabelFontSize { get => GetValue(TickLabelFontSizeProperty); set => SetValue(TickLabelFontSizeProperty, value); }
    public double TickLabelSpacing { get => GetValue(TickLabelSpacingProperty); set => SetValue(TickLabelSpacingProperty, value); }
    public FontFamily TickLabelFontFamily { get => GetValue(TickLabelFontFamilyProperty); set => SetValue(TickLabelFontFamilyProperty, value); }
    public FontWeight TickLabelFontWeight { get => GetValue(TickLabelFontWeightProperty); set => SetValue(TickLabelFontWeightProperty, value); }
    public IBrush TubeBrush { get => GetValue(TubeBrushProperty); set => SetValue(TubeBrushProperty, value); }
    public IBrush TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
    public IBrush LiquidBrush { get => GetValue(LiquidBrushProperty); set => SetValue(LiquidBrushProperty, value); }
    public LiquidBrushMappingMode LiquidBrushMappingMode { get => GetValue(LiquidBrushMappingModeProperty); set => SetValue(LiquidBrushMappingModeProperty, value); }
    public IBrush GlareBrush { get => GetValue(GlareBrushProperty); set => SetValue(GlareBrushProperty, value); }

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
            _tickLabelCacheWidth = 0;
            _tickLabelCacheValid = false;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        const double defaultWidth = 48;
        const double defaultHeight = 180;
        double desiredWidth = defaultWidth;
        if (ShowTicks && ShowTickLabels)
        {
            desiredWidth += Math.Max(0, TickLabelSpacing) + MeasureTickLabelWidth();
        }

        double width = double.IsInfinity(availableSize.Width)
            ? desiredWidth
            : Math.Min(desiredWidth, Math.Max(0, availableSize.Width));
        double height = double.IsInfinity(availableSize.Height)
            ? defaultHeight
            : Math.Min(defaultHeight, Math.Max(0, availableSize.Height));

        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width < 12 || height < 32)
        {
            return;
        }

        double scale = Math.Min(width / 48.0, height / 180.0);
        double tubeThickness = Math.Clamp(6 * scale, 3, 10);
        double liquidThickness = Math.Max(2, tubeThickness * 0.64);
        double bulbRadius = Math.Clamp(8 * scale, 5, 14);
        double tickGap = Math.Max(3, 3 * scale);
        double tickLength = ShowTicks ? Math.Clamp(7 * scale, 4, 12) : 0;
        double labelWidth = ShowTicks && ShowTickLabels ? MeasureTickLabelWidth() : 0;
        double labelSpacing = labelWidth > 0 ? Math.Max(0, TickLabelSpacing) : 0;
        double leftExtent = bulbRadius;
        double rightExtent = Math.Max(
            bulbRadius,
            (tubeThickness / 2) + tickGap + tickLength + labelSpacing + labelWidth);
        double centerX = Math.Clamp(
            (width + leftExtent - rightExtent) / 2,
            leftExtent,
            Math.Max(leftExtent, width - rightExtent));
        double bottomMargin = Math.Max(2, 4 * scale);
        double bulbCenterY = height - bottomMargin - bulbRadius;
        double tubeTop = Math.Max(tubeThickness / 2, 6 * scale);
        double tubeBottom = bulbCenterY - bulbRadius;
        double usableHeight = tubeBottom - tubeTop;
        if (usableHeight <= 0)
        {
            return;
        }

        var tubePen = new Pen(TubeBrush, tubeThickness, lineCap: PenLineCap.Round);
        context.DrawLine(tubePen, new Point(centerX, tubeTop), new Point(centerX, bulbCenterY));
        context.DrawEllipse(TubeBrush, null, new Point(centerX, bulbCenterY), bulbRadius, bulbRadius);

        DrawTicks(context, centerX, tubeTop, tubeBottom, tubeThickness, tickGap, tickLength);

        double progress = NormalizeValue(Value, Minimum, Maximum);
        double fillTop = tubeBottom - (progress * usableHeight);
        double liquidRadius = Math.Max(2, bulbRadius - Math.Max(1, tubeThickness * 0.2));
        Geometry liquidGeometry = CreateLiquidGeometry(
            centerX,
            bulbCenterY,
            fillTop,
            liquidThickness,
            liquidRadius);
        DrawLiquid(
            context,
            liquidGeometry,
            centerX,
            bulbCenterY,
            tubeTop,
            liquidThickness,
            liquidRadius);

        double glareRadius = Math.Max(1.2, liquidRadius * 0.2);
        context.DrawEllipse(
            GlareBrush,
            null,
            new Point(centerX - (liquidRadius * 0.38), bulbCenterY - (liquidRadius * 0.38)),
            glareRadius,
            glareRadius);
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

    private static Geometry CreateLiquidGeometry(
        double centerX,
        double bulbCenterY,
        double fillTop,
        double liquidThickness,
        double liquidRadius)
    {
        double columnRadius = liquidThickness / 2;
        double shoulderOffset = Math.Sqrt(
            Math.Max(0, (liquidRadius * liquidRadius) - (columnRadius * columnRadius)));
        double shoulderY = bulbCenterY - shoulderOffset;
        var geometry = new StreamGeometry();
        using StreamGeometryContext geometryContext = geometry.Open();
        geometryContext.BeginFigure(new Point(centerX - columnRadius, fillTop), true);
        geometryContext.ArcTo(
            new Point(centerX + columnRadius, fillTop),
            new Size(columnRadius, columnRadius),
            0,
            false,
            SweepDirection.Clockwise,
            true);
        geometryContext.LineTo(new Point(centerX + columnRadius, shoulderY), true);
        geometryContext.ArcTo(
            new Point(centerX - columnRadius, shoulderY),
            new Size(liquidRadius, liquidRadius),
            0,
            true,
            SweepDirection.Clockwise,
            true);
        geometryContext.LineTo(new Point(centerX - columnRadius, fillTop), true);
        geometryContext.EndFigure(true);
        return geometry;
    }

    private void DrawLiquid(
        DrawingContext context,
        Geometry liquidGeometry,
        double centerX,
        double bulbCenterY,
        double tubeTop,
        double liquidThickness,
        double liquidRadius)
    {
        if (LiquidBrushMappingMode == LiquidBrushMappingMode.FilledArea)
        {
            context.DrawGeometry(LiquidBrush, null, liquidGeometry);
            return;
        }

        Geometry fullRangeGeometry = CreateLiquidGeometry(
            centerX,
            bulbCenterY,
            tubeTop,
            liquidThickness,
            liquidRadius);
        using (context.PushGeometryClip(liquidGeometry))
        {
            context.DrawGeometry(LiquidBrush, null, fullRangeGeometry);
        }
    }

    private void DrawTicks(
        DrawingContext context,
        double centerX,
        double tubeTop,
        double tubeBottom,
        double tubeThickness,
        double tickGap,
        double tickLength)
    {
        int count = Math.Clamp(TickCount, 0, 100);
        if (!ShowTicks || count == 0)
        {
            return;
        }

        var tickPen = new Pen(TickBrush, Math.Max(1, tubeThickness * 0.16));
        double startX = centerX + (tubeThickness / 2) + tickGap;
        double labelX = startX + tickLength + Math.Max(0, TickLabelSpacing);
        for (int index = 0; index <= count; index++)
        {
            double progress = index / (double)count;
            double y = tubeBottom - (progress * (tubeBottom - tubeTop));
            double length = index == 0 || index == count ? tickLength : tickLength * 0.72;
            context.DrawLine(tickPen, new Point(startX, y), new Point(startX + length, y));
            if (!ShowTickLabels || !ShouldDrawTickLabel(index, count, TickLabelInterval))
            {
                continue;
            }

            double value = Minimum + (progress * (Maximum - Minimum));
            FormattedText label = GetTickLabel(index, value);
            context.DrawText(label, new Point(labelX, y - (label.Height / 2)));
        }
    }

    private double MeasureTickLabelWidth()
    {
        EnsureTickLabelCache();
        return _tickLabelCacheWidth;
    }

    private FormattedText GetTickLabel(int index, double value)
    {
        EnsureTickLabelCache();
        return _tickLabelCache.TryGetValue(index, out FormattedText? label)
            ? label
            : CreateTickLabelText(FormatTickLabel(value));
    }

    private void EnsureTickLabelCache()
    {
        if (_tickLabelCacheValid)
        {
            return;
        }

        _tickLabelCache.Clear();
        _tickLabelCacheWidth = 0;
        int count = Math.Clamp(TickCount, 0, 100);
        if (count == 0 || !double.IsFinite(Minimum) || !double.IsFinite(Maximum))
        {
            _tickLabelCacheValid = true;
            return;
        }

        for (int index = 0; index <= count; index++)
        {
            if (!ShouldDrawTickLabel(index, count, TickLabelInterval))
            {
                continue;
            }

            double progress = index / (double)count;
            double value = Minimum + (progress * (Maximum - Minimum));
            FormattedText label = CreateTickLabelText(FormatTickLabel(value));
            _tickLabelCache[index] = label;
            _tickLabelCacheWidth = Math.Max(_tickLabelCacheWidth, label.Width);
        }

        _tickLabelCacheValid = true;
    }

    private FormattedText CreateTickLabelText(string text) =>
        new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(TickLabelFontFamily, FontStyle.Normal, TickLabelFontWeight),
            Math.Max(1, TickLabelFontSize),
            TickLabelBrush);

}

#pragma warning restore CS1591
