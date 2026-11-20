using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a reusable single-series or multi-series line chart with linear, smooth,
/// monotone-smooth, or step interpolation.
/// </summary>
public class LineChart : CartesianChart
{
    public static readonly StyledProperty<IEnumerable<LineChartSeries>?> SeriesProperty =
        AvaloniaProperty.Register<LineChart, IEnumerable<LineChartSeries>?>(nameof(Series));
    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush>(nameof(LineBrush), AvioraControlPalette.Accent);
    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(LineThickness), 2.0);
    public static readonly StyledProperty<IReadOnlyList<double>?> LineDashArrayProperty =
        AvaloniaProperty.Register<LineChart, IReadOnlyList<double>?>(nameof(LineDashArray));
    public static readonly StyledProperty<PenLineCap> LineCapProperty =
        AvaloniaProperty.Register<LineChart, PenLineCap>(nameof(LineCap), PenLineCap.Round);
    public static readonly StyledProperty<LineInterpolationMode> InterpolationModeProperty =
        AvaloniaProperty.Register<LineChart, LineInterpolationMode>(
            nameof(InterpolationMode), LineInterpolationMode.Smooth);
    public static readonly StyledProperty<double> LineTensionProperty =
        AvaloniaProperty.Register<LineChart, double>(
            nameof(LineTension), 1.0, validate: v => double.IsFinite(v) && v >= 0.0 && v <= 2.0);
    public static readonly StyledProperty<IBrush?> AreaFillBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(AreaFillBrush));
    public static readonly StyledProperty<bool> ShowPointsProperty =
        AvaloniaProperty.Register<LineChart, bool>(nameof(ShowPoints), true);
    public static readonly StyledProperty<IBrush?> PointBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(PointBrush));
    public static readonly StyledProperty<double> PointRadiusProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(PointRadius), 3.0);
    public static readonly StyledProperty<ChartPointShape> PointShapeProperty =
        AvaloniaProperty.Register<LineChart, ChartPointShape>(nameof(PointShape), ChartPointShape.Circle);
    public static readonly StyledProperty<IBrush?> PointStrokeBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(PointStrokeBrush));
    public static readonly StyledProperty<double> PointStrokeThicknessProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(PointStrokeThickness));
    public static readonly StyledProperty<IBrush?> SelectedPointBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(SelectedPointBrush));
    public static readonly StyledProperty<double> SelectedPointRadiusProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(SelectedPointRadius), double.NaN);
    public static readonly StyledProperty<double> HoverPointRadiusProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(HoverPointRadius), double.NaN);
    public static readonly StyledProperty<IBrush?> HoverHaloBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(HoverHaloBrush));
    public static readonly StyledProperty<bool> ShowDataLabelsProperty =
        AvaloniaProperty.Register<LineChart, bool>(nameof(ShowDataLabels));
    public static readonly StyledProperty<IBrush?> DataLabelBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(DataLabelBrush));
    public static readonly StyledProperty<double> DataLabelFontSizeProperty =
        AvaloniaProperty.Register<LineChart, double>(nameof(DataLabelFontSize), 10.0);
    public static readonly StyledProperty<Func<double, string>?> DataLabelFormatterProperty =
        AvaloniaProperty.Register<LineChart, Func<double, string>?>(nameof(DataLabelFormatter));

    private readonly LineChartRenderer _renderer = new();

    static LineChart()
    {
        AffectsRender<LineChart>(
            SeriesProperty,
            LineBrushProperty,
            LineThicknessProperty,
            LineDashArrayProperty,
            LineCapProperty,
            InterpolationModeProperty,
            LineTensionProperty,
            AreaFillBrushProperty,
            ShowPointsProperty,
            PointBrushProperty,
            PointRadiusProperty,
            PointShapeProperty,
            PointStrokeBrushProperty,
            PointStrokeThicknessProperty,
            SelectedPointBrushProperty,
            SelectedPointRadiusProperty,
            HoverPointRadiusProperty,
            HoverHaloBrushProperty,
            ShowDataLabelsProperty,
            DataLabelBrushProperty,
            DataLabelFontSizeProperty,
            DataLabelFormatterProperty);
    }

    public IBrush LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    public double LineThickness { get => GetValue(LineThicknessProperty); set => SetValue(LineThicknessProperty, value); }
    public IReadOnlyList<double>? LineDashArray { get => GetValue(LineDashArrayProperty); set => SetValue(LineDashArrayProperty, value); }
    public PenLineCap LineCap { get => GetValue(LineCapProperty); set => SetValue(LineCapProperty, value); }
    public LineInterpolationMode InterpolationMode { get => GetValue(InterpolationModeProperty); set => SetValue(InterpolationModeProperty, value); }
    public double LineTension { get => GetValue(LineTensionProperty); set => SetValue(LineTensionProperty, value); }
    public IBrush? AreaFillBrush { get => GetValue(AreaFillBrushProperty); set => SetValue(AreaFillBrushProperty, value); }
    public bool ShowPoints { get => GetValue(ShowPointsProperty); set => SetValue(ShowPointsProperty, value); }
    public IBrush? PointBrush { get => GetValue(PointBrushProperty); set => SetValue(PointBrushProperty, value); }
    public double PointRadius { get => GetValue(PointRadiusProperty); set => SetValue(PointRadiusProperty, value); }
    public ChartPointShape PointShape { get => GetValue(PointShapeProperty); set => SetValue(PointShapeProperty, value); }
    public IBrush? PointStrokeBrush { get => GetValue(PointStrokeBrushProperty); set => SetValue(PointStrokeBrushProperty, value); }
    public double PointStrokeThickness { get => GetValue(PointStrokeThicknessProperty); set => SetValue(PointStrokeThicknessProperty, value); }
    public IBrush? SelectedPointBrush { get => GetValue(SelectedPointBrushProperty); set => SetValue(SelectedPointBrushProperty, value); }
    public double SelectedPointRadius { get => GetValue(SelectedPointRadiusProperty); set => SetValue(SelectedPointRadiusProperty, value); }
    public double HoverPointRadius { get => GetValue(HoverPointRadiusProperty); set => SetValue(HoverPointRadiusProperty, value); }
    public IBrush? HoverHaloBrush { get => GetValue(HoverHaloBrushProperty); set => SetValue(HoverHaloBrushProperty, value); }
    public bool ShowDataLabels { get => GetValue(ShowDataLabelsProperty); set => SetValue(ShowDataLabelsProperty, value); }
    public IBrush? DataLabelBrush { get => GetValue(DataLabelBrushProperty); set => SetValue(DataLabelBrushProperty, value); }
    public double DataLabelFontSize { get => GetValue(DataLabelFontSizeProperty); set => SetValue(DataLabelFontSizeProperty, value); }
    public Func<double, string>? DataLabelFormatter { get => GetValue(DataLabelFormatterProperty); set => SetValue(DataLabelFormatterProperty, value); }
    public IEnumerable<LineChartSeries>? Series { get => GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }

    internal int RenderStateBuildCount => _renderer.StateBuildCount;

    protected override List<IChartDataPoint> BuildChartItems() =>
        Series?.Any() == true ? ChartDataPipeline.BuildSeriesItems(Series) : base.BuildChartItems();

    protected override IEnumerable<object?> GetAdditionalObservedCollections()
    {
        if (Series == null)
        {
            yield break;
        }

        yield return Series;
        foreach (LineChartSeries series in Series)
        {
            yield return series.ItemsSource ?? (object?)series.Values;
        }
    }

    protected override IEnumerable<AvaloniaObject?> GetAdditionalObservedObjects()
    {
        if (Series == null)
        {
            yield break;
        }

        foreach (LineChartSeries series in Series)
        {
            yield return series;
        }
    }

    internal override IReadOnlyList<ChartDataPipeline.ChartSeriesMeta> GetSeriesMetadata()
    {
        if (Series == null)
        {
            return [];
        }

        var result = new List<ChartDataPipeline.ChartSeriesMeta>();
        int index = 0;
        foreach (LineChartSeries series in Series)
        {
            result.Add(new ChartDataPipeline.ChartSeriesMeta(
                index,
                string.IsNullOrWhiteSpace(series.Title) ? $"Series {index + 1}" : series.Title!,
                series.LineBrush ?? series.PointBrush ?? LineBrush,
                series.IsVisible,
                series,
                LineSeries: series));
            index++;
        }

        return result;
    }

    internal override bool HasHoverVisuals() =>
        base.HasHoverVisuals() ||
        HoverHaloBrush != null ||
        (double.IsFinite(HoverPointRadius) && HoverPointRadius > 0);

    internal override bool IsSeriesDataAffectingProperty(AvaloniaProperty property) =>
        property == LineChartSeries.ItemsSourceProperty ||
        property == LineChartSeries.ValuesProperty ||
        property == LineChartSeries.IsVisibleProperty;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeriesProperty)
        {
            SubscribeToCollections();
            HandleNewData();
        }
    }

    internal override ChartInvalidationFlags ClassifyPropertyInvalidation(AvaloniaProperty property)
    {
        if (property == SeriesProperty)
        {
            return ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry;
        }

        if (property == InterpolationModeProperty ||
            property == LineTensionProperty ||
            property == LineBrushProperty ||
            property == LineThicknessProperty ||
            property == LineDashArrayProperty ||
            property == LineCapProperty ||
            property == AreaFillBrushProperty)
        {
            return ChartInvalidationFlags.SeriesGeometry;
        }

        if (property == ShowPointsProperty ||
            property == PointBrushProperty ||
            property == PointRadiusProperty ||
            property == PointShapeProperty ||
            property == PointStrokeBrushProperty ||
            property == PointStrokeThicknessProperty ||
            property == SelectedPointBrushProperty ||
            property == SelectedPointRadiusProperty ||
            property == HoverPointRadiusProperty ||
            property == HoverHaloBrushProperty ||
            property == ShowDataLabelsProperty ||
            property == DataLabelBrushProperty ||
            property == DataLabelFontSizeProperty ||
            property == DataLabelFormatterProperty)
        {
            return ChartInvalidationFlags.VisualOnly;
        }

        return base.ClassifyPropertyInvalidation(property);
    }

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

    protected override int HitTestCategory(Point point) =>
        _renderer.HitTestCategory(this, ChartItems, point);

    protected override Rect GetCurrentPlotBounds() =>
        _renderer.GetPlotBounds(this, ChartItems);

    protected override int GetCurrentCategoryCount() =>
        _renderer.GetCategoryCount(this, ChartItems);

    protected override bool TryToggleLegendAt(Point point) =>
        _renderer.TryToggleLegendAt(this, ChartItems, point);

    internal override IReadOnlyList<Rect> GetLegendItemBounds() =>
        _renderer.GetLegendItemBounds(this, ChartItems);

    internal override void InvalidateChartState(ChartInvalidationFlags flags) =>
        _renderer.Invalidate(flags);
}

#pragma warning restore CS1591
