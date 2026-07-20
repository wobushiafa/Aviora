using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a reusable single-series line chart with linear or smooth interpolation.
/// </summary>
public class LineChart : CartesianChart
{
    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush>(nameof(LineBrush), Brushes.DodgerBlue);
    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(LineThickness), 2.0);
    public static readonly StyledProperty<LineInterpolationMode> InterpolationModeProperty =
        AvaloniaProperty.Register<LineChart, LineInterpolationMode>(
            nameof(InterpolationMode), LineInterpolationMode.Smooth);
    public static readonly StyledProperty<IBrush?> AreaFillBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(AreaFillBrush));
    public static readonly StyledProperty<bool> ShowPointsProperty =
        AvaloniaProperty.Register<LineChart, bool>(nameof(ShowPoints), true);
    public static readonly StyledProperty<IBrush?> PointBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(PointBrush));
    public static readonly StyledProperty<double> PointRadiusProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(PointRadius), 3.0);
    public static readonly StyledProperty<IBrush?> PointStrokeBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(PointStrokeBrush));
    public static readonly StyledProperty<double> PointStrokeThicknessProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(PointStrokeThickness));
    public static readonly StyledProperty<IBrush?> SelectedPointBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(SelectedPointBrush));
    public static readonly StyledProperty<double> SelectedPointRadiusProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(SelectedPointRadius), double.NaN);

    private readonly LineChartRenderer _renderer = new();

    static LineChart()
    {
        AffectsRender<LineChart>(
            LineBrushProperty,
            LineThicknessProperty,
            InterpolationModeProperty,
            AreaFillBrushProperty,
            ShowPointsProperty,
            PointBrushProperty,
            PointRadiusProperty,
            PointStrokeBrushProperty,
            PointStrokeThicknessProperty,
            SelectedPointBrushProperty,
            SelectedPointRadiusProperty);
    }

    public IBrush LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    public double LineThickness { get => GetValue(LineThicknessProperty); set => SetValue(LineThicknessProperty, value); }
    public LineInterpolationMode InterpolationMode { get => GetValue(InterpolationModeProperty); set => SetValue(InterpolationModeProperty, value); }
    public IBrush? AreaFillBrush { get => GetValue(AreaFillBrushProperty); set => SetValue(AreaFillBrushProperty, value); }
    public bool ShowPoints { get => GetValue(ShowPointsProperty); set => SetValue(ShowPointsProperty, value); }
    public IBrush? PointBrush { get => GetValue(PointBrushProperty); set => SetValue(PointBrushProperty, value); }
    public double PointRadius { get => GetValue(PointRadiusProperty); set => SetValue(PointRadiusProperty, value); }
    public IBrush? PointStrokeBrush { get => GetValue(PointStrokeBrushProperty); set => SetValue(PointStrokeBrushProperty, value); }
    public double PointStrokeThickness { get => GetValue(PointStrokeThicknessProperty); set => SetValue(PointStrokeThicknessProperty, value); }
    public IBrush? SelectedPointBrush { get => GetValue(SelectedPointBrushProperty); set => SetValue(SelectedPointBrushProperty, value); }
    public double SelectedPointRadius { get => GetValue(SelectedPointRadiusProperty); set => SetValue(SelectedPointRadiusProperty, value); }

    protected override void RenderChart(DrawingContext context)
    {
        _renderer.Render(
            context,
            this,
            ChartItems,
            AnimatedValues,
            HoveredIndex,
            PointerPosition);
    }

    protected override int HitTestDataPoint(Point point) =>
        _renderer.HitTest(this, ChartItems, point);

    protected override void InvalidateChartState() => _renderer.Invalidate();
}

#pragma warning restore CS1591
