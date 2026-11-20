using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Aviora.Core.Charts;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Provides the shared data, axes, animation, selection, legend, crosshair, zoom/pan, and tooltip model
/// for Cartesian charts.
/// </summary>
public abstract class CartesianChart : Control
{
    private static readonly double[] DefaultGridDashArray = [4.0, 4.0];
    private static readonly double[] DefaultCrosshairDashArray = [3.0, 3.0];

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<CartesianChart>();
    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextElement.FontStyleProperty.AddOwner<CartesianChart>();
    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner<CartesianChart>();

    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<double>?>(nameof(Values));
    public static readonly StyledProperty<IEnumerable<IChartDataPoint>?> ItemsSourceProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<IChartDataPoint>?>(nameof(ItemsSource));
    public static readonly StyledProperty<IEnumerable?> DataSourceProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable?>(nameof(DataSource));
    public static readonly StyledProperty<Func<object, double>?> ValueSelectorProperty =
        AvaloniaProperty.Register<CartesianChart, Func<object, double>?>(nameof(ValueSelector));
    public static readonly StyledProperty<Func<object, string?>?> LabelSelectorProperty =
        AvaloniaProperty.Register<CartesianChart, Func<object, string?>?>(nameof(LabelSelector));
    public static readonly StyledProperty<Func<object, object?>?> KeySelectorProperty =
        AvaloniaProperty.Register<CartesianChart, Func<object, object?>?>(nameof(KeySelector));
    public static readonly StyledProperty<Func<object, string?>?> ToolTipSelectorProperty =
        AvaloniaProperty.Register<CartesianChart, Func<object, string?>?>(nameof(ToolTipSelector));
    public static readonly StyledProperty<Func<object, IBrush?>?> BrushSelectorProperty =
        AvaloniaProperty.Register<CartesianChart, Func<object, IBrush?>?>(nameof(BrushSelector));

    public static readonly StyledProperty<double> MinValueProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(MinValue));
    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(MaxValue), 100.0);
    public static readonly StyledProperty<bool> AutoRangeProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(AutoRange));
    public static readonly StyledProperty<bool> AutoRangeIncludeZeroProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(AutoRangeIncludeZero), true);
    public static readonly StyledProperty<double> AutoRangePaddingRatioProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(AutoRangePaddingRatio), 0.08);

    public static readonly StyledProperty<Thickness> PlotPaddingProperty =
        AvaloniaProperty.Register<CartesianChart, Thickness>(nameof(PlotPadding));
    public static readonly StyledProperty<IBrush?> PlotBackgroundProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush?>(nameof(PlotBackground));
    public static readonly StyledProperty<IBrush?> PlotBorderBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush?>(nameof(PlotBorderBrush));
    public static readonly StyledProperty<Thickness> PlotBorderThicknessProperty =
        AvaloniaProperty.Register<CartesianChart, Thickness>(nameof(PlotBorderThickness));
    public static readonly StyledProperty<CornerRadius> PlotCornerRadiusProperty =
        AvaloniaProperty.Register<CartesianChart, CornerRadius>(nameof(PlotCornerRadius));

    public static readonly StyledProperty<IBrush> GridLineBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(GridLineBrush), AvioraControlPalette.Border);
    public static readonly StyledProperty<double> GridLineThicknessProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(GridLineThickness), 1.0);
    public static readonly StyledProperty<IReadOnlyList<double>?> GridLineDashArrayProperty =
        AvaloniaProperty.Register<CartesianChart, IReadOnlyList<double>?>(nameof(GridLineDashArray), DefaultGridDashArray);
    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowGridLines), true);
    public static readonly StyledProperty<bool> ShowVerticalGridLinesProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowVerticalGridLines));
    public static readonly StyledProperty<IBrush?> VerticalGridLineBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush?>(nameof(VerticalGridLineBrush));

    public static readonly StyledProperty<bool> ShowAxisLinesProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowAxisLines));
    public static readonly StyledProperty<IBrush> AxisLineBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(AxisLineBrush), AvioraControlPalette.Border);
    public static readonly StyledProperty<double> AxisLineThicknessProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(AxisLineThickness), 1.0);
    public static readonly StyledProperty<bool> ShowZeroBaselineProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowZeroBaseline));
    public static readonly StyledProperty<IBrush?> ZeroBaselineBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush?>(nameof(ZeroBaselineBrush));
    public static readonly StyledProperty<double> ZeroBaselineThicknessProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(ZeroBaselineThickness), 1.0);

    public static readonly StyledProperty<bool> ShowThresholdsProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowThresholds));
    public static readonly StyledProperty<IEnumerable<ChartThreshold>?> ThresholdsProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<ChartThreshold>?>(nameof(Thresholds));
    public static readonly StyledProperty<ThresholdDirection> ThresholdDirectionProperty =
        AvaloniaProperty.Register<CartesianChart, ThresholdDirection>(
            nameof(ThresholdDirection), global::Aviora.Controls.ThresholdDirection.HigherIsMoreSevere);
    public static readonly StyledProperty<bool> ShowThresholdLabelsProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowThresholdLabels), true);
    public static readonly StyledProperty<double> ThresholdLabelFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(ThresholdLabelFontSize), 10.0);

    public static readonly StyledProperty<bool> ShowXAxisProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowXAxis), true);
    public static readonly StyledProperty<string?> XAxisLabelsProperty =
        AvaloniaProperty.Register<CartesianChart, string?>(nameof(XAxisLabels));
    public static readonly StyledProperty<IEnumerable<string>?> XAxisLabelsSourceProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<string>?>(nameof(XAxisLabelsSource));
    public static readonly StyledProperty<ChartLabelMode> XAxisLabelModeProperty =
        AvaloniaProperty.Register<CartesianChart, ChartLabelMode>(nameof(XAxisLabelMode), ChartLabelMode.Auto);
    public static readonly StyledProperty<int> XAxisLabelIntervalProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(XAxisLabelInterval), 1);
    public static readonly StyledProperty<IBrush> XAxisTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(XAxisTextBrush), AvioraControlPalette.TextMuted);
    public static readonly StyledProperty<double> XAxisFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(XAxisFontSize), 10.0);
    public static readonly StyledProperty<double> XAxisHeightProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(XAxisHeight), 26.0);

    public static readonly StyledProperty<bool> ShowYAxisProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowYAxis), true);
    public static readonly StyledProperty<int> GridLineCountProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(GridLineCount), 5);
    public static readonly StyledProperty<string?> YAxisLabelsProperty =
        AvaloniaProperty.Register<CartesianChart, string?>(nameof(YAxisLabels));
    public static readonly StyledProperty<IEnumerable<string>?> YAxisLabelsSourceProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<string>?>(nameof(YAxisLabelsSource));
    public static readonly StyledProperty<double> YAxisWidthProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(YAxisWidth), 44.0);
    public static readonly StyledProperty<double> YAxisFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(YAxisFontSize), 10.0);
    public static readonly StyledProperty<IBrush> YAxisTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(YAxisTextBrush), AvioraControlPalette.TextMuted);

    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(IsAnimationEnabled), true);
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<CartesianChart, TimeSpan>(nameof(AnimationDuration), TimeSpan.FromMilliseconds(320));
    public static readonly StyledProperty<Easing?> AnimationEasingProperty =
        AvaloniaProperty.Register<CartesianChart, Easing?>(nameof(AnimationEasing));
    public static readonly StyledProperty<int> AnimationItemLimitProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(AnimationItemLimit), 200);
    public static readonly StyledProperty<TimeSpan> UpdateThrottleIntervalProperty =
        AvaloniaProperty.Register<CartesianChart, TimeSpan>(nameof(UpdateThrottleInterval), TimeSpan.FromMilliseconds(400));

    public static readonly StyledProperty<ChartDownsampleMode> DownsampleModeProperty =
        AvaloniaProperty.Register<CartesianChart, ChartDownsampleMode>(nameof(DownsampleMode), ChartDownsampleMode.Lttb);
    public static readonly StyledProperty<int> DownsampleThresholdProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(DownsampleThreshold), 500);

    public static readonly StyledProperty<bool> ShowEmptyTextProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowEmptyText), true);
    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<CartesianChart, string>(nameof(EmptyText), "No data");
    public static readonly StyledProperty<IBrush> EmptyTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(EmptyTextBrush), AvioraControlPalette.TextMuted);
    public static readonly StyledProperty<double> EmptyTextFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(EmptyTextFontSize), 12.0);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<IChartDataPoint?> SelectedItemProperty =
        AvaloniaProperty.Register<CartesianChart, IChartDataPoint?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<ICommand?> ItemClickCommandProperty =
        AvaloniaProperty.Register<CartesianChart, ICommand?>(nameof(ItemClickCommand));

    public static readonly StyledProperty<bool> IsToolTipEnabledProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(IsToolTipEnabled), true);
    public static readonly StyledProperty<ChartToolTipMode> ToolTipModeProperty =
        AvaloniaProperty.Register<CartesianChart, ChartToolTipMode>(nameof(ToolTipMode), ChartToolTipMode.SinglePoint);
    public static readonly StyledProperty<IBrush> ToolTipBackgroundProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(
            nameof(ToolTipBackground),
            AvioraControlPalette.SurfaceRaised);
    public static readonly StyledProperty<IBrush> ToolTipTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(ToolTipTextBrush), AvioraControlPalette.Text);
    public static readonly StyledProperty<double> ToolTipFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(ToolTipFontSize), 11.0);
    public static readonly StyledProperty<IDataTemplate?> ToolTipTemplateProperty =
        AvaloniaProperty.Register<CartesianChart, IDataTemplate?>(nameof(ToolTipTemplate));
    public static readonly StyledProperty<Thickness> ToolTipPaddingProperty =
        AvaloniaProperty.Register<CartesianChart, Thickness>(nameof(ToolTipPadding), new Thickness(7));
    public static readonly StyledProperty<CornerRadius> ToolTipCornerRadiusProperty =
        AvaloniaProperty.Register<CartesianChart, CornerRadius>(nameof(ToolTipCornerRadius), new CornerRadius(4));
    public static readonly StyledProperty<IBrush?> ToolTipBorderBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush?>(nameof(ToolTipBorderBrush));
    public static readonly StyledProperty<Thickness> ToolTipBorderThicknessProperty =
        AvaloniaProperty.Register<CartesianChart, Thickness>(nameof(ToolTipBorderThickness));
    public static readonly StyledProperty<BoxShadows> ToolTipBoxShadowProperty =
        AvaloniaProperty.Register<CartesianChart, BoxShadows>(nameof(ToolTipBoxShadow));
    public static readonly StyledProperty<double> ToolTipHorizontalOffsetProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(ToolTipHorizontalOffset), 10.0);
    public static readonly StyledProperty<double> ToolTipVerticalOffsetProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(ToolTipVerticalOffset), 10.0);
    public static readonly StyledProperty<TimeSpan> ToolTipHideDelayProperty =
        AvaloniaProperty.Register<CartesianChart, TimeSpan>(
            nameof(ToolTipHideDelay), TimeSpan.FromMilliseconds(250), validate: value => value >= TimeSpan.Zero);
    public static readonly StyledProperty<Func<double, string>?> YAxisLabelFormatterProperty =
        AvaloniaProperty.Register<CartesianChart, Func<double, string>?>(nameof(YAxisLabelFormatter));
    public static readonly StyledProperty<Func<IChartDataPoint, string>?> ToolTipFormatterProperty =
        AvaloniaProperty.Register<CartesianChart, Func<IChartDataPoint, string>?>(nameof(ToolTipFormatter));

    public static readonly StyledProperty<ChartCrosshairMode> CrosshairModeProperty =
        AvaloniaProperty.Register<CartesianChart, ChartCrosshairMode>(nameof(CrosshairMode), ChartCrosshairMode.None);
    public static readonly StyledProperty<IBrush> CrosshairBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(CrosshairBrush), AvioraControlPalette.Muted);
    public static readonly StyledProperty<double> CrosshairThicknessProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(CrosshairThickness), 1.0);
    public static readonly StyledProperty<IReadOnlyList<double>?> CrosshairDashArrayProperty =
        AvaloniaProperty.Register<CartesianChart, IReadOnlyList<double>?>(nameof(CrosshairDashArray), DefaultCrosshairDashArray);
    public static readonly StyledProperty<bool> SnapCrosshairToDataPointProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(SnapCrosshairToDataPoint), true);

    public static readonly StyledProperty<bool> ShowLegendProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowLegend));
    public static readonly StyledProperty<ChartLegendPlacement> LegendPlacementProperty =
        AvaloniaProperty.Register<CartesianChart, ChartLegendPlacement>(nameof(LegendPlacement), ChartLegendPlacement.Top);
    public static readonly StyledProperty<bool> IsLegendInteractiveProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(IsLegendInteractive), true);
    public static readonly StyledProperty<double> LegendFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(LegendFontSize), 11.0);
    public static readonly StyledProperty<IBrush> LegendTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(LegendTextBrush), AvioraControlPalette.Text);

    public static readonly StyledProperty<bool> IsZoomEnabledProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(IsZoomEnabled));
    public static readonly StyledProperty<bool> IsPanEnabledProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(IsPanEnabled));
    public static readonly StyledProperty<int> MinVisiblePointsProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(MinVisiblePoints), 3);
    public static readonly StyledProperty<double> ZoomStartRatioProperty =
        AvaloniaProperty.Register<CartesianChart, double>(
            nameof(ZoomStartRatio), 0.0, defaultBindingMode: BindingMode.TwoWay, validate: v => double.IsFinite(v) && v >= 0 && v <= 1);
    public static readonly StyledProperty<double> ZoomEndRatioProperty =
        AvaloniaProperty.Register<CartesianChart, double>(
            nameof(ZoomEndRatio), 1.0, defaultBindingMode: BindingMode.TwoWay, validate: v => double.IsFinite(v) && v >= 0 && v <= 1);

    public static readonly StyledProperty<Action<DrawingContext, ChartPlotContext>?> PlotBackgroundRendererProperty =
        AvaloniaProperty.Register<CartesianChart, Action<DrawingContext, ChartPlotContext>?>(nameof(PlotBackgroundRenderer));
    public static readonly StyledProperty<Action<DrawingContext, ChartPlotContext>?> PlotOverlayRendererProperty =
        AvaloniaProperty.Register<CartesianChart, Action<DrawingContext, ChartPlotContext>?>(nameof(PlotOverlayRenderer));

    private readonly ChartAnimationController _animation = new();
    private readonly ChartDataObserver _dataObserver;
    private readonly ChartToolTipPresenter _toolTip;
    private readonly ChartUpdateScheduler _updateScheduler;
    private List<IChartDataPoint> _items = [];
    private TimeSpan? _animationStartTime;
    private bool _animationFrameRequested;
    private bool _isSynchronizingSelection;
    private Point _pointerPosition;
    private bool _isPointerInPlot;
    private int _hoveredItemIndex = -1;
    private int _hoveredCategoryIndex = -1;
    private bool _isPanning;
    private bool _panDragged;
    private Point _panStartPoint;
    private double _panStartZoomStart;
    private double _panStartZoomEnd;

    protected CartesianChart()
    {
        _dataObserver = new ChartDataObserver(
            OnObservedCollectionChanged,
            OnObservedItemChanged,
            OnObservedSeriesPropertyChanged);
        _updateScheduler = new ChartUpdateScheduler(ApplyTargetItems);
        _toolTip = new ChartToolTipPresenter(this);
        VisualChildren.Add(_toolTip.Visual);
        LogicalChildren.Add(_toolTip.Visual);
        Focusable = true;
    }

    static CartesianChart()
    {
        AffectsRender<CartesianChart>(
            BoundsProperty, FontFamilyProperty, FontStyleProperty, FontWeightProperty,
            ValuesProperty, ItemsSourceProperty, DataSourceProperty, ValueSelectorProperty,
            LabelSelectorProperty, KeySelectorProperty, ToolTipSelectorProperty, BrushSelectorProperty,
            MinValueProperty, MaxValueProperty, AutoRangeProperty, AutoRangeIncludeZeroProperty,
            AutoRangePaddingRatioProperty, PlotPaddingProperty, PlotBackgroundProperty,
            PlotBorderBrushProperty, PlotBorderThicknessProperty, PlotCornerRadiusProperty,
            GridLineBrushProperty, GridLineThicknessProperty, GridLineDashArrayProperty, ShowGridLinesProperty,
            ShowVerticalGridLinesProperty, VerticalGridLineBrushProperty,
            ShowAxisLinesProperty, AxisLineBrushProperty, AxisLineThicknessProperty,
            ShowZeroBaselineProperty, ZeroBaselineBrushProperty, ZeroBaselineThicknessProperty,
            ShowThresholdsProperty, ThresholdsProperty, ThresholdDirectionProperty,
            ShowThresholdLabelsProperty, ThresholdLabelFontSizeProperty,
            ShowXAxisProperty, XAxisLabelsProperty, XAxisLabelsSourceProperty, XAxisLabelModeProperty,
            XAxisLabelIntervalProperty, XAxisTextBrushProperty, XAxisFontSizeProperty, XAxisHeightProperty,
            ShowYAxisProperty, GridLineCountProperty, YAxisLabelsProperty, YAxisLabelsSourceProperty,
            YAxisWidthProperty, YAxisFontSizeProperty, YAxisTextBrushProperty,
            DownsampleModeProperty, DownsampleThresholdProperty,
            ShowEmptyTextProperty, EmptyTextProperty, EmptyTextBrushProperty, EmptyTextFontSizeProperty,
            SelectedIndexProperty, SelectedItemProperty, YAxisLabelFormatterProperty,
            CrosshairModeProperty, CrosshairBrushProperty, CrosshairThicknessProperty, CrosshairDashArrayProperty,
            SnapCrosshairToDataPointProperty,
            ShowLegendProperty, LegendPlacementProperty, LegendFontSizeProperty, LegendTextBrushProperty,
            MinVisiblePointsProperty, ZoomStartRatioProperty, ZoomEndRatioProperty,
            PlotBackgroundRendererProperty, PlotOverlayRendererProperty);
    }

    #region Properties
    public FontFamily FontFamily { get => GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public FontStyle FontStyle { get => GetValue(FontStyleProperty); set => SetValue(FontStyleProperty, value); }
    public FontWeight FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    public IEnumerable<double>? Values { get => GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public IEnumerable<IChartDataPoint>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public IEnumerable? DataSource { get => GetValue(DataSourceProperty); set => SetValue(DataSourceProperty, value); }
    public Func<object, double>? ValueSelector { get => GetValue(ValueSelectorProperty); set => SetValue(ValueSelectorProperty, value); }
    public Func<object, string?>? LabelSelector { get => GetValue(LabelSelectorProperty); set => SetValue(LabelSelectorProperty, value); }
    public Func<object, object?>? KeySelector { get => GetValue(KeySelectorProperty); set => SetValue(KeySelectorProperty, value); }
    public Func<object, string?>? ToolTipSelector { get => GetValue(ToolTipSelectorProperty); set => SetValue(ToolTipSelectorProperty, value); }
    public Func<object, IBrush?>? BrushSelector { get => GetValue(BrushSelectorProperty); set => SetValue(BrushSelectorProperty, value); }

    public double MinValue { get => GetValue(MinValueProperty); set => SetValue(MinValueProperty, value); }
    public double MaxValue { get => GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }
    public bool AutoRange { get => GetValue(AutoRangeProperty); set => SetValue(AutoRangeProperty, value); }
    public bool AutoRangeIncludeZero { get => GetValue(AutoRangeIncludeZeroProperty); set => SetValue(AutoRangeIncludeZeroProperty, value); }
    public double AutoRangePaddingRatio { get => GetValue(AutoRangePaddingRatioProperty); set => SetValue(AutoRangePaddingRatioProperty, value); }

    public Thickness PlotPadding { get => GetValue(PlotPaddingProperty); set => SetValue(PlotPaddingProperty, value); }
    public IBrush? PlotBackground { get => GetValue(PlotBackgroundProperty); set => SetValue(PlotBackgroundProperty, value); }
    public IBrush? PlotBorderBrush { get => GetValue(PlotBorderBrushProperty); set => SetValue(PlotBorderBrushProperty, value); }
    public Thickness PlotBorderThickness { get => GetValue(PlotBorderThicknessProperty); set => SetValue(PlotBorderThicknessProperty, value); }
    public CornerRadius PlotCornerRadius { get => GetValue(PlotCornerRadiusProperty); set => SetValue(PlotCornerRadiusProperty, value); }

    public IBrush GridLineBrush { get => GetValue(GridLineBrushProperty); set => SetValue(GridLineBrushProperty, value); }
    public double GridLineThickness { get => GetValue(GridLineThicknessProperty); set => SetValue(GridLineThicknessProperty, value); }
    public IReadOnlyList<double>? GridLineDashArray { get => GetValue(GridLineDashArrayProperty); set => SetValue(GridLineDashArrayProperty, value); }
    public bool ShowGridLines { get => GetValue(ShowGridLinesProperty); set => SetValue(ShowGridLinesProperty, value); }
    public bool ShowVerticalGridLines { get => GetValue(ShowVerticalGridLinesProperty); set => SetValue(ShowVerticalGridLinesProperty, value); }
    public IBrush? VerticalGridLineBrush { get => GetValue(VerticalGridLineBrushProperty); set => SetValue(VerticalGridLineBrushProperty, value); }

    public bool ShowAxisLines { get => GetValue(ShowAxisLinesProperty); set => SetValue(ShowAxisLinesProperty, value); }
    public IBrush AxisLineBrush { get => GetValue(AxisLineBrushProperty); set => SetValue(AxisLineBrushProperty, value); }
    public double AxisLineThickness { get => GetValue(AxisLineThicknessProperty); set => SetValue(AxisLineThicknessProperty, value); }
    public bool ShowZeroBaseline { get => GetValue(ShowZeroBaselineProperty); set => SetValue(ShowZeroBaselineProperty, value); }
    public IBrush? ZeroBaselineBrush { get => GetValue(ZeroBaselineBrushProperty); set => SetValue(ZeroBaselineBrushProperty, value); }
    public double ZeroBaselineThickness { get => GetValue(ZeroBaselineThicknessProperty); set => SetValue(ZeroBaselineThicknessProperty, value); }

    public bool ShowThresholds { get => GetValue(ShowThresholdsProperty); set => SetValue(ShowThresholdsProperty, value); }
    public IEnumerable<ChartThreshold>? Thresholds { get => GetValue(ThresholdsProperty); set => SetValue(ThresholdsProperty, value); }
    public ThresholdDirection ThresholdDirection { get => GetValue(ThresholdDirectionProperty); set => SetValue(ThresholdDirectionProperty, value); }
    public bool ShowThresholdLabels { get => GetValue(ShowThresholdLabelsProperty); set => SetValue(ShowThresholdLabelsProperty, value); }
    public double ThresholdLabelFontSize { get => GetValue(ThresholdLabelFontSizeProperty); set => SetValue(ThresholdLabelFontSizeProperty, value); }

    public bool ShowXAxis { get => GetValue(ShowXAxisProperty); set => SetValue(ShowXAxisProperty, value); }
    public string? XAxisLabels { get => GetValue(XAxisLabelsProperty); set => SetValue(XAxisLabelsProperty, value); }
    public IEnumerable<string>? XAxisLabelsSource { get => GetValue(XAxisLabelsSourceProperty); set => SetValue(XAxisLabelsSourceProperty, value); }
    public ChartLabelMode XAxisLabelMode { get => GetValue(XAxisLabelModeProperty); set => SetValue(XAxisLabelModeProperty, value); }
    public int XAxisLabelInterval { get => GetValue(XAxisLabelIntervalProperty); set => SetValue(XAxisLabelIntervalProperty, value); }
    public IBrush XAxisTextBrush { get => GetValue(XAxisTextBrushProperty); set => SetValue(XAxisTextBrushProperty, value); }
    public double XAxisFontSize { get => GetValue(XAxisFontSizeProperty); set => SetValue(XAxisFontSizeProperty, value); }
    public double XAxisHeight { get => GetValue(XAxisHeightProperty); set => SetValue(XAxisHeightProperty, value); }

    public bool ShowYAxis { get => GetValue(ShowYAxisProperty); set => SetValue(ShowYAxisProperty, value); }
    public int GridLineCount { get => GetValue(GridLineCountProperty); set => SetValue(GridLineCountProperty, value); }
    public string? YAxisLabels { get => GetValue(YAxisLabelsProperty); set => SetValue(YAxisLabelsProperty, value); }
    public IEnumerable<string>? YAxisLabelsSource { get => GetValue(YAxisLabelsSourceProperty); set => SetValue(YAxisLabelsSourceProperty, value); }
    public double YAxisWidth { get => GetValue(YAxisWidthProperty); set => SetValue(YAxisWidthProperty, value); }
    public double YAxisFontSize { get => GetValue(YAxisFontSizeProperty); set => SetValue(YAxisFontSizeProperty, value); }
    public IBrush YAxisTextBrush { get => GetValue(YAxisTextBrushProperty); set => SetValue(YAxisTextBrushProperty, value); }

    public bool IsAnimationEnabled { get => GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }
    public TimeSpan AnimationDuration { get => GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }
    public Easing? AnimationEasing { get => GetValue(AnimationEasingProperty); set => SetValue(AnimationEasingProperty, value); }
    public int AnimationItemLimit { get => GetValue(AnimationItemLimitProperty); set => SetValue(AnimationItemLimitProperty, value); }
    public TimeSpan UpdateThrottleInterval { get => GetValue(UpdateThrottleIntervalProperty); set => SetValue(UpdateThrottleIntervalProperty, value); }

    public ChartDownsampleMode DownsampleMode { get => GetValue(DownsampleModeProperty); set => SetValue(DownsampleModeProperty, value); }
    public int DownsampleThreshold { get => GetValue(DownsampleThresholdProperty); set => SetValue(DownsampleThresholdProperty, value); }

    public bool ShowEmptyText { get => GetValue(ShowEmptyTextProperty); set => SetValue(ShowEmptyTextProperty, value); }
    public string EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public IBrush EmptyTextBrush { get => GetValue(EmptyTextBrushProperty); set => SetValue(EmptyTextBrushProperty, value); }
    public double EmptyTextFontSize { get => GetValue(EmptyTextFontSizeProperty); set => SetValue(EmptyTextFontSizeProperty, value); }

    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public IChartDataPoint? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public ICommand? ItemClickCommand { get => GetValue(ItemClickCommandProperty); set => SetValue(ItemClickCommandProperty, value); }

    public bool IsToolTipEnabled { get => GetValue(IsToolTipEnabledProperty); set => SetValue(IsToolTipEnabledProperty, value); }
    public ChartToolTipMode ToolTipMode { get => GetValue(ToolTipModeProperty); set => SetValue(ToolTipModeProperty, value); }
    public IBrush ToolTipBackground { get => GetValue(ToolTipBackgroundProperty); set => SetValue(ToolTipBackgroundProperty, value); }
    public IBrush ToolTipTextBrush { get => GetValue(ToolTipTextBrushProperty); set => SetValue(ToolTipTextBrushProperty, value); }
    public double ToolTipFontSize { get => GetValue(ToolTipFontSizeProperty); set => SetValue(ToolTipFontSizeProperty, value); }
    public IDataTemplate? ToolTipTemplate { get => GetValue(ToolTipTemplateProperty); set => SetValue(ToolTipTemplateProperty, value); }
    public Thickness ToolTipPadding { get => GetValue(ToolTipPaddingProperty); set => SetValue(ToolTipPaddingProperty, value); }
    public CornerRadius ToolTipCornerRadius { get => GetValue(ToolTipCornerRadiusProperty); set => SetValue(ToolTipCornerRadiusProperty, value); }
    public IBrush? ToolTipBorderBrush { get => GetValue(ToolTipBorderBrushProperty); set => SetValue(ToolTipBorderBrushProperty, value); }
    public Thickness ToolTipBorderThickness { get => GetValue(ToolTipBorderThicknessProperty); set => SetValue(ToolTipBorderThicknessProperty, value); }
    public BoxShadows ToolTipBoxShadow { get => GetValue(ToolTipBoxShadowProperty); set => SetValue(ToolTipBoxShadowProperty, value); }
    public double ToolTipHorizontalOffset { get => GetValue(ToolTipHorizontalOffsetProperty); set => SetValue(ToolTipHorizontalOffsetProperty, value); }
    public double ToolTipVerticalOffset { get => GetValue(ToolTipVerticalOffsetProperty); set => SetValue(ToolTipVerticalOffsetProperty, value); }
    public TimeSpan ToolTipHideDelay { get => GetValue(ToolTipHideDelayProperty); set => SetValue(ToolTipHideDelayProperty, value); }
    public Func<double, string>? YAxisLabelFormatter { get => GetValue(YAxisLabelFormatterProperty); set => SetValue(YAxisLabelFormatterProperty, value); }
    public Func<IChartDataPoint, string>? ToolTipFormatter { get => GetValue(ToolTipFormatterProperty); set => SetValue(ToolTipFormatterProperty, value); }

    public ChartCrosshairMode CrosshairMode { get => GetValue(CrosshairModeProperty); set => SetValue(CrosshairModeProperty, value); }
    public IBrush CrosshairBrush { get => GetValue(CrosshairBrushProperty); set => SetValue(CrosshairBrushProperty, value); }
    public double CrosshairThickness { get => GetValue(CrosshairThicknessProperty); set => SetValue(CrosshairThicknessProperty, value); }
    public IReadOnlyList<double>? CrosshairDashArray { get => GetValue(CrosshairDashArrayProperty); set => SetValue(CrosshairDashArrayProperty, value); }
    public bool SnapCrosshairToDataPoint { get => GetValue(SnapCrosshairToDataPointProperty); set => SetValue(SnapCrosshairToDataPointProperty, value); }

    public bool ShowLegend { get => GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public ChartLegendPlacement LegendPlacement { get => GetValue(LegendPlacementProperty); set => SetValue(LegendPlacementProperty, value); }
    public bool IsLegendInteractive { get => GetValue(IsLegendInteractiveProperty); set => SetValue(IsLegendInteractiveProperty, value); }
    public double LegendFontSize { get => GetValue(LegendFontSizeProperty); set => SetValue(LegendFontSizeProperty, value); }
    public IBrush LegendTextBrush { get => GetValue(LegendTextBrushProperty); set => SetValue(LegendTextBrushProperty, value); }

    public bool IsZoomEnabled { get => GetValue(IsZoomEnabledProperty); set => SetValue(IsZoomEnabledProperty, value); }
    public bool IsPanEnabled { get => GetValue(IsPanEnabledProperty); set => SetValue(IsPanEnabledProperty, value); }
    public int MinVisiblePoints { get => GetValue(MinVisiblePointsProperty); set => SetValue(MinVisiblePointsProperty, value); }
    public double ZoomStartRatio { get => GetValue(ZoomStartRatioProperty); set => SetValue(ZoomStartRatioProperty, value); }
    public double ZoomEndRatio { get => GetValue(ZoomEndRatioProperty); set => SetValue(ZoomEndRatioProperty, value); }

    public Action<DrawingContext, ChartPlotContext>? PlotBackgroundRenderer { get => GetValue(PlotBackgroundRendererProperty); set => SetValue(PlotBackgroundRendererProperty, value); }
    public Action<DrawingContext, ChartPlotContext>? PlotOverlayRenderer { get => GetValue(PlotOverlayRendererProperty); set => SetValue(PlotOverlayRendererProperty, value); }
    #endregion

    protected IReadOnlyList<IChartDataPoint> ChartItems => _items;

    protected IReadOnlyList<double> AnimatedValues => _animation.Values;

    internal bool IsAnimationRunning => _animation.IsAnimating;

    internal Border ToolTipPresenter => _toolTip.Visual;

    internal Point? HoverPosition => _isPointerInPlot ? _pointerPosition : null;

    internal int HoveredItemIndex => _hoveredItemIndex;

    internal int HoveredCategoryIndex => _hoveredCategoryIndex;

    /// <summary>Resets zoom range back to show the full dataset.</summary>
    public void ResetZoom()
    {
        if (ZoomStartRatio == 0.0 && ZoomEndRatio == 1.0)
        {
            return;
        }

        SetCurrentValue(ZoomStartRatioProperty, 0.0);
        SetCurrentValue(ZoomEndRatioProperty, 1.0);
    }

    protected virtual List<IChartDataPoint> BuildChartItems() =>
        ChartDataPipeline.BuildItems(
            ItemsSource,
            DataSource,
            ValueSelector,
            LabelSelector,
            KeySelector,
            ToolTipSelector,
            BrushSelector,
            Values,
            XAxisLabelsSource,
            XAxisLabels);

    protected virtual IEnumerable<object?> GetAdditionalObservedCollections() => [];

    protected virtual IEnumerable<AvaloniaObject?> GetAdditionalObservedObjects() => [];

    internal virtual IReadOnlyList<ChartDataPipeline.ChartSeriesMeta> GetSeriesMetadata() => [];

    internal virtual bool HasHoverVisuals() => CrosshairMode != ChartCrosshairMode.None;

    internal virtual bool IsSeriesDataAffectingProperty(AvaloniaProperty property) => false;

    internal virtual string? ResolveCategoryLabel(int categoryIndex, string? fallback)
    {
        List<string> labels = ChartDataPipeline.ResolveLabels(XAxisLabelsSource, XAxisLabels);
        if (categoryIndex >= 0 && categoryIndex < labels.Count && !string.IsNullOrWhiteSpace(labels[categoryIndex]))
        {
            return labels[categoryIndex];
        }

        return fallback;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (ChartToolTipPresenter.IsProperty(change.Property))
        {
            _toolTip.ApplyStyle();
            _toolTip.Refresh(_items);
        }

        if (IsDataSourceProperty(change.Property))
        {
            SubscribeToCollections();
            HandleNewData();
            return;
        }

        if (change.Property == XAxisLabelsSourceProperty ||
            change.Property == YAxisLabelsSourceProperty ||
            change.Property == ThresholdsProperty)
        {
            SubscribeToCollections();
            InvalidateChartState(ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry);
            InvalidateVisual();
            return;
        }

        if (change.Property == IsAnimationEnabledProperty && !IsAnimationEnabled)
        {
            CompleteAnimation();
        }
        else if (change.Property == UpdateThrottleIntervalProperty)
        {
            _updateScheduler.Flush();
        }
        else if (change.Property == SelectedIndexProperty)
        {
            SynchronizeSelectionFromIndex();
        }
        else if (change.Property == SelectedItemProperty)
        {
            SynchronizeSelectionFromItem();
        }

        ChartInvalidationFlags flags = ClassifyPropertyInvalidation(change.Property);
        if (flags != ChartInvalidationFlags.None)
        {
            InvalidateChartState(flags);
        }
    }

    internal virtual ChartInvalidationFlags ClassifyPropertyInvalidation(AvaloniaProperty property)
    {
        if (property == FontFamilyProperty ||
            property == FontStyleProperty ||
            property == FontWeightProperty ||
            property == XAxisFontSizeProperty ||
            property == YAxisFontSizeProperty ||
            property == ThresholdLabelFontSizeProperty ||
            property == EmptyTextFontSizeProperty ||
            property == LegendFontSizeProperty ||
            property == XAxisTextBrushProperty ||
            property == YAxisTextBrushProperty ||
            property == EmptyTextBrushProperty ||
            property == LegendTextBrushProperty)
        {
            return ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry | ChartInvalidationFlags.TextCache;
        }

        if (property == BoundsProperty ||
            property == MinValueProperty ||
            property == MaxValueProperty ||
            property == AutoRangeProperty ||
            property == AutoRangeIncludeZeroProperty ||
            property == AutoRangePaddingRatioProperty ||
            property == PlotPaddingProperty ||
            property == ShowThresholdsProperty ||
            property == ThresholdDirectionProperty ||
            property == ShowThresholdLabelsProperty ||
            property == ShowXAxisProperty ||
            property == XAxisLabelsProperty ||
            property == XAxisLabelModeProperty ||
            property == XAxisLabelIntervalProperty ||
            property == XAxisHeightProperty ||
            property == ShowYAxisProperty ||
            property == GridLineCountProperty ||
            property == YAxisLabelsProperty ||
            property == YAxisWidthProperty ||
            property == YAxisLabelFormatterProperty ||
            property == ShowLegendProperty ||
            property == LegendPlacementProperty ||
            property == MinVisiblePointsProperty ||
            property == ZoomStartRatioProperty ||
            property == ZoomEndRatioProperty ||
            property == DownsampleModeProperty ||
            property == DownsampleThresholdProperty)
        {
            return ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry;
        }

        if (property == SelectedIndexProperty ||
            property == SelectedItemProperty ||
            property == GridLineBrushProperty ||
            property == GridLineThicknessProperty ||
            property == GridLineDashArrayProperty ||
            property == ShowGridLinesProperty ||
            property == ShowVerticalGridLinesProperty ||
            property == VerticalGridLineBrushProperty ||
            property == ShowAxisLinesProperty ||
            property == AxisLineBrushProperty ||
            property == AxisLineThicknessProperty ||
            property == ShowZeroBaselineProperty ||
            property == ZeroBaselineBrushProperty ||
            property == ZeroBaselineThicknessProperty ||
            property == PlotBackgroundProperty ||
            property == PlotBorderBrushProperty ||
            property == PlotBorderThicknessProperty ||
            property == PlotCornerRadiusProperty ||
            property == CrosshairModeProperty ||
            property == CrosshairBrushProperty ||
            property == CrosshairThicknessProperty ||
            property == CrosshairDashArrayProperty ||
            property == SnapCrosshairToDataPointProperty ||
            property == ShowEmptyTextProperty ||
            property == EmptyTextProperty ||
            property == PlotBackgroundRendererProperty ||
            property == PlotOverlayRendererProperty)
        {
            return ChartInvalidationFlags.VisualOnly;
        }

        return ChartInvalidationFlags.None;
    }

    private static bool IsDataSourceProperty(AvaloniaProperty property) =>
        property == ValuesProperty ||
        property == ItemsSourceProperty ||
        property == DataSourceProperty ||
        property == ValueSelectorProperty ||
        property == LabelSelectorProperty ||
        property == KeySelectorProperty ||
        property == ToolTipSelectorProperty ||
        property == BrushSelectorProperty;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeToCollections();
        HandleNewData();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animation.Stop();
        _animationStartTime = null;
        _animationFrameRequested = false;
        _isPanning = false;
        _toolTip.Hide();
        _dataObserver.Dispose();
        _updateScheduler.Stop();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Point pos = e.GetPosition(this);

        if (_isPanning)
        {
            Rect plot = GetCurrentPlotBounds();
            double dx = pos.X - _panStartPoint.X;
            if (!_panDragged && Math.Abs(dx) >= 4.0)
            {
                _panDragged = true;
                _toolTip.Hide();
            }

            if (_panDragged && plot.Width > 0)
            {
                double span = _panStartZoomEnd - _panStartZoomStart;
                double deltaRatio = -(dx / plot.Width) * span;
                double newStart = Math.Clamp(_panStartZoomStart + deltaRatio, 0.0, Math.Max(0.0, 1.0 - span));
                SetCurrentValue(ZoomStartRatioProperty, newStart);
                SetCurrentValue(ZoomEndRatioProperty, newStart + span);
                e.Handled = true;
            }

            return;
        }

        _pointerPosition = pos;
        int prevItemIndex = _hoveredItemIndex;
        int prevCategoryIndex = _hoveredCategoryIndex;
        bool prevInPlot = _isPointerInPlot;

        _isPointerInPlot = GetCurrentPlotBounds().Contains(pos);
        _hoveredItemIndex = HitTestDataPoint(_pointerPosition);
        _hoveredCategoryIndex = HitTestCategory(_pointerPosition);

        if (_toolTip.Update(_hoveredItemIndex, _pointerPosition))
        {
            _toolTip.Refresh(_items);
        }

        if (HasHoverVisuals() &&
            (_isPointerInPlot || prevInPlot || prevItemIndex != _hoveredItemIndex || prevCategoryIndex != _hoveredCategoryIndex))
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _pointerPosition = e.GetPosition(this);
        bool hadHover = _isPointerInPlot || _hoveredItemIndex >= 0 || _hoveredCategoryIndex >= 0;
        _isPointerInPlot = false;
        _hoveredItemIndex = -1;
        _hoveredCategoryIndex = -1;
        _toolTip.Clear();
        _toolTip.RequestHide(reevaluatePointer: IsPointerWithinChart());
        if (hadHover && HasHoverVisuals())
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled || !IsZoomEnabled || Math.Abs(e.Delta.Y) <= double.Epsilon)
        {
            return;
        }

        Point pos = e.GetPosition(this);
        Rect plot = GetCurrentPlotBounds();
        int categoryCount = GetCurrentCategoryCount();
        int minVisible = Math.Max(1, MinVisiblePoints);
        if (plot.Width <= 0 || !plot.Contains(pos) || categoryCount <= minVisible)
        {
            return;
        }

        double anchor = Math.Clamp((pos.X - plot.Left) / plot.Width, 0.0, 1.0);
        double span = Math.Max(1e-6, ZoomEndRatio - ZoomStartRatio);
        double factor = e.Delta.Y > 0 ? 0.8 : 1.25;
        double minSpan = Math.Min(1.0, (double)minVisible / categoryCount);
        double newSpan = Math.Clamp(span * factor, minSpan, 1.0);
        double focus = ZoomStartRatio + (anchor * span);
        double newStart = focus - (anchor * newSpan);
        double newEnd = newStart + newSpan;
        if (newStart < 0.0)
        {
            newStart = 0.0;
            newEnd = newSpan;
        }

        if (newEnd > 1.0)
        {
            newEnd = 1.0;
            newStart = Math.Max(0.0, 1.0 - newSpan);
        }

        SetCurrentValue(ZoomStartRatioProperty, newStart);
        SetCurrentValue(ZoomEndRatioProperty, newEnd);
        e.Handled = true;
    }

    internal void ReevaluateToolTip()
    {
        if (!IsPointerWithinChart())
        {
            _toolTip.Hide();
            return;
        }

        int index = HitTestDataPoint(_pointerPosition);
        _hoveredItemIndex = index;
        _hoveredCategoryIndex = HitTestCategory(_pointerPosition);
        _toolTip.Reevaluate(index, _pointerPosition, _items);
    }

    private bool IsPointerWithinChart()
    {
        return IsPointerOver || _toolTip.Visual.IsPointerOver;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        PointerPoint pointerPoint = e.GetCurrentPoint(this);
        if (!pointerPoint.Properties.IsLeftButtonPressed)
        {
            return;
        }

        Point pos = e.GetPosition(this);
        if (ShowLegend && IsLegendInteractive && TryToggleLegendAt(pos))
        {
            e.Handled = true;
            return;
        }

        Rect plot = GetCurrentPlotBounds();
        if (e.ClickCount >= 2 && IsZoomEnabled && (ZoomStartRatio > 1e-6 || ZoomEndRatio < 1.0 - 1e-6) && plot.Contains(pos))
        {
            ResetZoom();
            e.Handled = true;
            return;
        }

        if (IsPanEnabled && (ZoomStartRatio > 1e-6 || ZoomEndRatio < 1.0 - 1e-6) && plot.Contains(pos))
        {
            _isPanning = true;
            _panDragged = false;
            _panStartPoint = pos;
            _panStartZoomStart = ZoomStartRatio;
            _panStartZoomEnd = ZoomEndRatio;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        SelectPointAt(pos, e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }

        if (!_panDragged)
        {
            SelectPointAt(e.GetPosition(this), e);
        }

        e.Handled = true;
    }

    private void SelectPointAt(Point pos, RoutedEventArgs e)
    {
        int index = HitTestDataPoint(pos);
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        Focus();
        SelectIndex(index, executeCommand: true);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Enter or Key.Space)
        {
            ExecuteSelectedItemCommand();
            e.Handled = SelectedItem != null;
            return;
        }

        int index = ChartSelectionState.Move(SelectedIndex, _items.Count, e.Key);
        if (index != SelectedIndex)
        {
            SelectIndex(index, executeCommand: false);
            e.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsInfinity(availableSize.Width) ? 100 : Math.Max(0, availableSize.Width);
        double height = double.IsInfinity(availableSize.Height) ? 150 : Math.Max(0, availableSize.Height);
        var desired = new Size(width, height);
        _toolTip.Measure(desired);
        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _toolTip.Arrange(finalSize);
        return finalSize;
    }

    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderChart(context);
    }

    protected abstract void RenderChart(DrawingContext context);

    protected abstract int HitTestDataPoint(Point point);

    protected virtual int HitTestCategory(Point point) => -1;

    protected virtual Rect GetCurrentPlotBounds() => default;

    protected virtual int GetCurrentCategoryCount() => _items.Count;

    protected virtual bool TryToggleLegendAt(Point point) => false;

    internal virtual IReadOnlyList<Rect> GetLegendItemBounds() => [];

    protected virtual void InvalidateChartState() =>
        InvalidateChartState(ChartInvalidationFlags.All);

    internal virtual void InvalidateChartState(ChartInvalidationFlags flags)
    {
    }

    private void SelectIndex(int index, bool executeCommand)
    {
        index = ChartSelectionState.NormalizeIndex(index, _items.Count);
        _isSynchronizingSelection = true;
        try
        {
            SelectedIndex = index;
            SelectedItem = index >= 0 ? _items[index] : null;
        }
        finally
        {
            _isSynchronizingSelection = false;
        }

        if (executeCommand)
        {
            ExecuteSelectedItemCommand();
        }
    }

    private void SynchronizeSelectionFromIndex()
    {
        if (!_isSynchronizingSelection)
        {
            SelectIndex(SelectedIndex, executeCommand: false);
        }
    }

    private void SynchronizeSelectionFromItem()
    {
        if (!_isSynchronizingSelection)
        {
            SelectIndex(ChartSelectionState.FindIndex(_items, SelectedItem), executeCommand: false);
        }
    }

    private void SynchronizeSelectionAfterItemsChanged()
    {
        int index = SelectedItem != null
            ? ChartSelectionState.FindIndex(_items, SelectedItem)
            : ChartSelectionState.NormalizeIndex(SelectedIndex, _items.Count);
        SelectIndex(index, executeCommand: false);
    }

    private void ExecuteSelectedItemCommand()
    {
        if (SelectedItem != null && ItemClickCommand?.CanExecute(SelectedItem) == true)
        {
            ItemClickCommand.Execute(SelectedItem);
        }
    }

    protected void SubscribeToCollections()
    {
        object?[] sources =
        [
            ItemsSource ?? (object?)DataSource ?? Values,
            XAxisLabelsSource,
            YAxisLabelsSource,
            Thresholds,
            .. GetAdditionalObservedCollections(),
        ];
        _dataObserver.ObserveCollections(sources);
        _dataObserver.ObserveAvaloniaObjects(GetAdditionalObservedObjects());
    }

    private void OnObservedCollectionChanged(object? sender, bool isIncremental)
    {
        bool affectsOnlyRendering = ReferenceEquals(sender, XAxisLabelsSource) ||
                                    ReferenceEquals(sender, YAxisLabelsSource) ||
                                    ReferenceEquals(sender, Thresholds);
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnObservedCollectionChanged(sender, isIncremental));
            return;
        }

        if (affectsOnlyRendering)
        {
            InvalidateChart(ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry);
            return;
        }

        bool primaryItemsOnly = isIncremental &&
                                (ReferenceEquals(sender, ItemsSource) || ReferenceEquals(sender, DataSource));
        if (!primaryItemsOnly)
        {
            SubscribeToCollections();
        }

        HandleNewData();
    }

    private void OnObservedItemChanged()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(HandleNewData);
            return;
        }

        HandleNewData();
    }

    private void OnObservedSeriesPropertyChanged(AvaloniaObject series, AvaloniaProperty property)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnObservedSeriesPropertyChanged(series, property));
            return;
        }

        if (IsSeriesDataAffectingProperty(property))
        {
            SubscribeToCollections();
            HandleNewData();
            return;
        }

        InvalidateChart(ChartInvalidationFlags.SeriesGeometry | ChartInvalidationFlags.LayoutAndScale);
    }

    protected void HandleNewData()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(HandleNewData);
            return;
        }

        if (UpdateThrottleInterval <= TimeSpan.Zero || IsKnownEmptyPrimarySource())
        {
            _updateScheduler.Schedule(BuildChartItems(), UpdateThrottleInterval);
            return;
        }

        _updateScheduler.Schedule(BuildChartItems, UpdateThrottleInterval);
    }

    private bool IsKnownEmptyPrimarySource()
    {
        if (ItemsSource is ICollection col)
        {
            return col.Count == 0;
        }

        if (DataSource is ICollection dataCol)
        {
            return dataCol.Count == 0;
        }

        if (Values is ICollection valCol)
        {
            return valCol.Count == 0;
        }

        return false;
    }

    private void ApplyTargetItems(List<IChartDataPoint> items)
    {
        _items = items;
        _dataObserver.ObserveItems(items);
        InvalidateChartState(ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry);
        _animationStartTime = null;

        if (_animation.SetTargets(ChartDataPipeline.GetFiniteValues(items), ShouldAnimate(items.Count)))
        {
            StartAnimationTimer();
        }

        SynchronizeSelectionAfterItemsChanged();
        _toolTip.Normalize(_items.Count);
        _toolTip.Refresh(_items);
        InvalidateVisual();
    }

    private bool ShouldAnimate(int itemCount) => IsAnimationEnabled && AnimationDuration > TimeSpan.Zero &&
                                                itemCount > 0 && itemCount <= Math.Max(0, AnimationItemLimit);

    private void StartAnimationTimer()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (!_animation.IsAnimating || _animationFrameRequested || topLevel == null)
        {
            return;
        }

        _animationFrameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan elapsed)
    {
        _animationFrameRequested = false;
        if (!_animation.IsAnimating)
        {
            return;
        }

        _animationStartTime ??= elapsed;
        double progress = Math.Clamp(
            (elapsed - _animationStartTime.Value).TotalMilliseconds /
            Math.Max(1, AnimationDuration.TotalMilliseconds),
            0,
            1);
        bool continues = _animation.Advance(progress, AnimationEasing);
        InvalidateVisual();
        if (continues)
        {
            StartAnimationTimer();
        }
    }

    private void CompleteAnimation()
    {
        _animation.Complete();
        _animationStartTime = null;
        InvalidateVisual();
    }

    private void InvalidateChart(ChartInvalidationFlags flags)
    {
        InvalidateChartState(flags);
        InvalidateVisual();
    }
}

#pragma warning restore CS1591
