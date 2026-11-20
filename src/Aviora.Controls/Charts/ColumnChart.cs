using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays a reusable single-series or multi-series column chart. Use <see cref="CartesianChart.Values"/>,
/// <see cref="CartesianChart.ItemsSource"/>, <see cref="CartesianChart.DataSource"/>, or <see cref="Series"/>.
/// </summary>
public class ColumnChart : CartesianChart
{
    public static readonly StyledProperty<IEnumerable<ColumnChartSeries>?> SeriesProperty =
        AvaloniaProperty.Register<ColumnChart, IEnumerable<ColumnChartSeries>?>(nameof(Series));
    public static readonly StyledProperty<ColumnSeriesLayoutMode> SeriesLayoutModeProperty =
        AvaloniaProperty.Register<ColumnChart, ColumnSeriesLayoutMode>(
            nameof(SeriesLayoutMode), ColumnSeriesLayoutMode.Grouped);
    public static readonly StyledProperty<double> BarWidthRatioProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(BarWidthRatio), 0.55);
    public static readonly StyledProperty<CornerRadius> BarCornerRadiusProperty =
        AvaloniaProperty.Register<ColumnChart, CornerRadius>(nameof(BarCornerRadius));
    public static readonly StyledProperty<double> MinBarWidthProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(MinBarWidth), 1.0);
    public static readonly StyledProperty<double> MaxBarWidthProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(MaxBarWidth), double.PositiveInfinity);
    public static readonly StyledProperty<double> BarSpacingRatioProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(BarSpacingRatio), 0.12);
    public static readonly StyledProperty<IBrush?> ColumnBackgroundBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(ColumnBackgroundBrush));
    public static readonly StyledProperty<IBrush?> HoverSlotBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(HoverSlotBrush));
    public static readonly StyledProperty<IBrush> DefaultBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush>(nameof(DefaultBrush), AvioraControlPalette.Accent);
    public static readonly StyledProperty<IBrush?> SelectedBarBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(SelectedBarBrush));
    public static readonly StyledProperty<IBrush?> SelectionOverlayBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(SelectionOverlayBrush));
    public static readonly StyledProperty<IBrush> SelectionStrokeBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush>(nameof(SelectionStrokeBrush), AvioraControlPalette.AccentStrong);
    public static readonly StyledProperty<double> SelectionStrokeThicknessProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(SelectionStrokeThickness));
    public static readonly StyledProperty<bool> ShowDataLabelsProperty =
        AvaloniaProperty.Register<ColumnChart, bool>(nameof(ShowDataLabels));
    public static readonly StyledProperty<ColumnLabelPosition> DataLabelPositionProperty =
        AvaloniaProperty.Register<ColumnChart, ColumnLabelPosition>(
            nameof(DataLabelPosition), ColumnLabelPosition.Outside);
    public static readonly StyledProperty<IBrush?> DataLabelBrushProperty =
        AvaloniaProperty.Register<ColumnChart, IBrush?>(nameof(DataLabelBrush));
    public static readonly StyledProperty<double> DataLabelFontSizeProperty =
        AvaloniaProperty.Register<ColumnChart, double>(nameof(DataLabelFontSize), 10.0);
    public static readonly StyledProperty<Func<double, string>?> DataLabelFormatterProperty =
        AvaloniaProperty.Register<ColumnChart, Func<double, string>?>(nameof(DataLabelFormatter));

    private readonly ColumnChartRenderer _renderer = new();

    static ColumnChart()
    {
        AffectsRender<ColumnChart>(
            SeriesProperty,
            SeriesLayoutModeProperty,
            BarWidthRatioProperty,
            BarCornerRadiusProperty,
            MinBarWidthProperty,
            MaxBarWidthProperty,
            BarSpacingRatioProperty,
            ColumnBackgroundBrushProperty,
            HoverSlotBrushProperty,
            DefaultBrushProperty,
            SelectedBarBrushProperty,
            SelectionOverlayBrushProperty,
            SelectionStrokeBrushProperty,
            SelectionStrokeThicknessProperty,
            ShowDataLabelsProperty,
            DataLabelPositionProperty,
            DataLabelBrushProperty,
            DataLabelFontSizeProperty,
            DataLabelFormatterProperty);
    }

    public IEnumerable<ColumnChartSeries>? Series { get => GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }
    public ColumnSeriesLayoutMode SeriesLayoutMode { get => GetValue(SeriesLayoutModeProperty); set => SetValue(SeriesLayoutModeProperty, value); }
    public double BarWidthRatio { get => GetValue(BarWidthRatioProperty); set => SetValue(BarWidthRatioProperty, value); }
    public CornerRadius BarCornerRadius { get => GetValue(BarCornerRadiusProperty); set => SetValue(BarCornerRadiusProperty, value); }
    public double MinBarWidth { get => GetValue(MinBarWidthProperty); set => SetValue(MinBarWidthProperty, value); }
    public double MaxBarWidth { get => GetValue(MaxBarWidthProperty); set => SetValue(MaxBarWidthProperty, value); }
    public double BarSpacingRatio { get => GetValue(BarSpacingRatioProperty); set => SetValue(BarSpacingRatioProperty, value); }
    public IBrush? ColumnBackgroundBrush { get => GetValue(ColumnBackgroundBrushProperty); set => SetValue(ColumnBackgroundBrushProperty, value); }
    public IBrush? HoverSlotBrush { get => GetValue(HoverSlotBrushProperty); set => SetValue(HoverSlotBrushProperty, value); }
    public IBrush DefaultBrush { get => GetValue(DefaultBrushProperty); set => SetValue(DefaultBrushProperty, value); }
    public IBrush? SelectedBarBrush { get => GetValue(SelectedBarBrushProperty); set => SetValue(SelectedBarBrushProperty, value); }
    public IBrush? SelectionOverlayBrush { get => GetValue(SelectionOverlayBrushProperty); set => SetValue(SelectionOverlayBrushProperty, value); }
    public IBrush SelectionStrokeBrush { get => GetValue(SelectionStrokeBrushProperty); set => SetValue(SelectionStrokeBrushProperty, value); }
    public double SelectionStrokeThickness { get => GetValue(SelectionStrokeThicknessProperty); set => SetValue(SelectionStrokeThicknessProperty, value); }
    public bool ShowDataLabels { get => GetValue(ShowDataLabelsProperty); set => SetValue(ShowDataLabelsProperty, value); }
    public ColumnLabelPosition DataLabelPosition { get => GetValue(DataLabelPositionProperty); set => SetValue(DataLabelPositionProperty, value); }
    public IBrush? DataLabelBrush { get => GetValue(DataLabelBrushProperty); set => SetValue(DataLabelBrushProperty, value); }
    public double DataLabelFontSize { get => GetValue(DataLabelFontSizeProperty); set => SetValue(DataLabelFontSizeProperty, value); }
    public Func<double, string>? DataLabelFormatter { get => GetValue(DataLabelFormatterProperty); set => SetValue(DataLabelFormatterProperty, value); }

    internal int RenderStateBuildCount => _renderer.StateBuildCount;

    protected override List<IChartDataPoint> BuildChartItems() =>
        Series?.Any() == true ? ChartDataPipeline.BuildColumnSeriesItems(Series) : base.BuildChartItems();

    protected override IEnumerable<object?> GetAdditionalObservedCollections()
    {
        if (Series == null)
        {
            yield break;
        }

        yield return Series;
        foreach (ColumnChartSeries series in Series)
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

        foreach (ColumnChartSeries series in Series)
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
        foreach (ColumnChartSeries series in Series)
        {
            result.Add(new ChartDataPipeline.ChartSeriesMeta(
                index,
                string.IsNullOrWhiteSpace(series.Title) ? $"Series {index + 1}" : series.Title!,
                series.Brush ?? DefaultBrush,
                series.IsVisible,
                series,
                ColumnSeries: series));
            index++;
        }

        return result;
    }

    internal override bool HasHoverVisuals() => base.HasHoverVisuals() || HoverSlotBrush != null;

    internal override bool IsSeriesDataAffectingProperty(AvaloniaProperty property) =>
        property == ColumnChartSeries.ItemsSourceProperty ||
        property == ColumnChartSeries.ValuesProperty ||
        property == ColumnChartSeries.IsVisibleProperty;

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
        if (property == SeriesProperty ||
            property == SeriesLayoutModeProperty ||
            property == BarWidthRatioProperty ||
            property == MinBarWidthProperty ||
            property == MaxBarWidthProperty ||
            property == BarSpacingRatioProperty)
        {
            return ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry;
        }

        if (property == BarCornerRadiusProperty ||
            property == ColumnBackgroundBrushProperty ||
            property == HoverSlotBrushProperty ||
            property == DefaultBrushProperty ||
            property == SelectedBarBrushProperty ||
            property == SelectionOverlayBrushProperty ||
            property == SelectionStrokeBrushProperty ||
            property == SelectionStrokeThicknessProperty ||
            property == ShowDataLabelsProperty ||
            property == DataLabelPositionProperty ||
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
