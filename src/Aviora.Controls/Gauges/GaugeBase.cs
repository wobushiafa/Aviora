using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>Shared value, scale, label, and needle behavior for gauge controls.</summary>
public abstract class GaugeBase : Control
{
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<GaugeBase, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<GaugeBase, double>(nameof(Maximum), 100.0);
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<GaugeBase, double>(nameof(Value));
    public static readonly StyledProperty<bool> ShowTicksProperty = AvaloniaProperty.Register<GaugeBase, bool>(nameof(ShowTicks), true);
    public static readonly StyledProperty<int> TickCountProperty = AvaloniaProperty.Register<GaugeBase, int>(nameof(TickCount), 20);
    public static readonly StyledProperty<IBrush> TickBrushProperty = AvaloniaProperty.Register<GaugeBase, IBrush>(nameof(TickBrush), AvioraControlPalette.Accent);
    public static readonly StyledProperty<bool> ShowTickLabelsProperty = AvaloniaProperty.Register<GaugeBase, bool>(nameof(ShowTickLabels), true);
    public static readonly StyledProperty<string?> TickLabelFormatProperty = AvaloniaProperty.Register<GaugeBase, string?>(nameof(TickLabelFormat), "0.##");
    public static readonly StyledProperty<Func<double, string?>?> TickLabelFormatterProperty = AvaloniaProperty.Register<GaugeBase, Func<double, string?>?>(nameof(TickLabelFormatter));
    public static readonly StyledProperty<IBrush> TickLabelBrushProperty = AvaloniaProperty.Register<GaugeBase, IBrush>(nameof(TickLabelBrush), AvioraControlPalette.TextMuted);
    public static readonly StyledProperty<double> TickLabelFontSizeProperty = AvaloniaProperty.Register<GaugeBase, double>(nameof(TickLabelFontSize), 11.0);
    public static readonly StyledProperty<FontFamily> TickLabelFontFamilyProperty = AvaloniaProperty.Register<GaugeBase, FontFamily>(nameof(TickLabelFontFamily), FontFamily.Default);
    public static readonly StyledProperty<FontWeight> TickLabelFontWeightProperty = AvaloniaProperty.Register<GaugeBase, FontWeight>(nameof(TickLabelFontWeight), FontWeight.Normal);
    public static readonly StyledProperty<IBrush> NeedleBrushProperty = AvaloniaProperty.Register<GaugeBase, IBrush>(nameof(NeedleBrush), AvioraControlPalette.AccentStrong);
    public static readonly StyledProperty<IBrush> PivotBrushProperty = AvaloniaProperty.Register<GaugeBase, IBrush>(nameof(PivotBrush), AvioraControlPalette.AccentStrong);

    static GaugeBase() => AffectsRender<GaugeBase>(BoundsProperty, MinimumProperty, MaximumProperty, ValueProperty, ShowTicksProperty, TickCountProperty, TickBrushProperty, ShowTickLabelsProperty, TickLabelFormatProperty, TickLabelFormatterProperty, TickLabelBrushProperty, TickLabelFontSizeProperty, TickLabelFontFamilyProperty, TickLabelFontWeightProperty, NeedleBrushProperty, PivotBrushProperty);

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public bool ShowTicks { get => GetValue(ShowTicksProperty); set => SetValue(ShowTicksProperty, value); }
    public int TickCount { get => GetValue(TickCountProperty); set => SetValue(TickCountProperty, value); }
    public IBrush TickBrush { get => GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }
    public bool ShowTickLabels { get => GetValue(ShowTickLabelsProperty); set => SetValue(ShowTickLabelsProperty, value); }
    public string? TickLabelFormat { get => GetValue(TickLabelFormatProperty); set => SetValue(TickLabelFormatProperty, value); }
    public Func<double, string?>? TickLabelFormatter { get => GetValue(TickLabelFormatterProperty); set => SetValue(TickLabelFormatterProperty, value); }
    public IBrush TickLabelBrush { get => GetValue(TickLabelBrushProperty); set => SetValue(TickLabelBrushProperty, value); }
    public double TickLabelFontSize { get => GetValue(TickLabelFontSizeProperty); set => SetValue(TickLabelFontSizeProperty, value); }
    public FontFamily TickLabelFontFamily { get => GetValue(TickLabelFontFamilyProperty); set => SetValue(TickLabelFontFamilyProperty, value); }
    public FontWeight TickLabelFontWeight { get => GetValue(TickLabelFontWeightProperty); set => SetValue(TickLabelFontWeightProperty, value); }
    public IBrush NeedleBrush { get => GetValue(NeedleBrushProperty); set => SetValue(NeedleBrushProperty, value); }
    public IBrush PivotBrush { get => GetValue(PivotBrushProperty); set => SetValue(PivotBrushProperty, value); }

    protected static double NormalizeValue(double value, double minimum, double maximum) =>
        !double.IsFinite(value) || !double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum <= minimum
            ? 0 : Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);

    protected string FormatTickLabel(double value)
    {
        if (TickLabelFormatter != null) return TickLabelFormatter(value) ?? string.Empty;
        try { return value.ToString(TickLabelFormat, CultureInfo.CurrentCulture); }
        catch (FormatException) { return value.ToString(CultureInfo.CurrentCulture); }
    }

    protected static bool ShouldDrawTickLabel(int index, int tickCount, int interval) => index == 0 || index == tickCount || index % Math.Max(1, interval) == 0;

    protected static Point PointOnCircle(double centerX, double centerY, double radius, double angleDegrees)
    {
        double radians = (180 - angleDegrees) * Math.PI / 180;
        return new Point(centerX + radius * Math.Cos(radians), centerY - radius * Math.Sin(radians));
    }
}
