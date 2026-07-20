using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a reusable single-series column chart. Use <see cref="CartesianChart.Values"/> for
/// simple numeric sequences or <see cref="CartesianChart.ItemsSource"/> for rich data points.
/// </summary>
public class ColumnChart : CartesianChart
{
    public static readonly StyledProperty<double> BarWidthRatioProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(BarWidthRatio), 0.55);
    public static readonly StyledProperty<IBrush?> ColumnBackgroundBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(ColumnBackgroundBrush));
    public static readonly StyledProperty<IBrush> DefaultBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush>(nameof(DefaultBrush), Brushes.Green);
    public static readonly StyledProperty<IBrush?> SelectedBarBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(SelectedBarBrush));
    public static readonly StyledProperty<IBrush?> SelectionOverlayBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(SelectionOverlayBrush));
    public static readonly StyledProperty<IBrush> SelectionStrokeBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush>(nameof(SelectionStrokeBrush), Brushes.DodgerBlue);
    public static readonly StyledProperty<double> SelectionStrokeThicknessProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(SelectionStrokeThickness));

    private readonly ColumnChartRenderer _renderer = new();

    static ColumnChart()
    {
        AffectsRender<ColumnChart>(
            BarWidthRatioProperty,
            ColumnBackgroundBrushProperty,
            DefaultBrushProperty,
            SelectedBarBrushProperty,
            SelectionOverlayBrushProperty,
            SelectionStrokeBrushProperty,
            SelectionStrokeThicknessProperty);
    }

    public double BarWidthRatio { get => GetValue(BarWidthRatioProperty); set => SetValue(BarWidthRatioProperty, value); }
    public IBrush? ColumnBackgroundBrush { get => GetValue(ColumnBackgroundBrushProperty); set => SetValue(ColumnBackgroundBrushProperty, value); }
    public IBrush DefaultBrush { get => GetValue(DefaultBrushProperty); set => SetValue(DefaultBrushProperty, value); }
    public IBrush? SelectedBarBrush { get => GetValue(SelectedBarBrushProperty); set => SetValue(SelectedBarBrushProperty, value); }
    public IBrush? SelectionOverlayBrush { get => GetValue(SelectionOverlayBrushProperty); set => SetValue(SelectionOverlayBrushProperty, value); }
    public IBrush SelectionStrokeBrush { get => GetValue(SelectionStrokeBrushProperty); set => SetValue(SelectionStrokeBrushProperty, value); }
    public double SelectionStrokeThickness { get => GetValue(SelectionStrokeThicknessProperty); set => SetValue(SelectionStrokeThicknessProperty, value); }

    protected override void RenderChart(DrawingContext context)
    {
        _renderer.Render(
            context,
            this,
            ChartItems,
            AnimatedValues);
    }

    protected override int HitTestDataPoint(Point point) =>
        _renderer.HitTest(this, ChartItems, point);

    protected override void InvalidateChartState() => _renderer.Invalidate();
}

#pragma warning restore CS1591
