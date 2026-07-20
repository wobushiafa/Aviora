using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Provides the shared data, axes, animation, selection, and interaction model
/// for single-series Cartesian charts.
/// </summary>
public abstract class CartesianChart : Control
{
    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<double>?>(nameof(Values));
    public static readonly StyledProperty<IEnumerable<IChartDataPoint>?> ItemsSourceProperty =
        AvaloniaProperty.Register<CartesianChart, IEnumerable<IChartDataPoint>?>(nameof(ItemsSource));
    public static readonly StyledProperty<double> MinValueProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(MinValue));
    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(MaxValue), 100.0);
    public static readonly StyledProperty<bool> AutoRangeProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(AutoRange));
    public static readonly StyledProperty<double> AutoRangePaddingRatioProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(AutoRangePaddingRatio), 0.08);
    public static readonly StyledProperty<IBrush> GridLineBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(GridLineBrush), Brushes.LightGray);
    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowGridLines), true);
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
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(XAxisTextBrush), Brushes.Gray);
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
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(YAxisTextBrush), Brushes.Gray);
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(IsAnimationEnabled), true);
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<CartesianChart, TimeSpan>(nameof(AnimationDuration), TimeSpan.FromMilliseconds(320));
    public static readonly StyledProperty<int> AnimationItemLimitProperty =
        AvaloniaProperty.Register<CartesianChart, int>(nameof(AnimationItemLimit), 200);
    public static readonly StyledProperty<TimeSpan> UpdateThrottleIntervalProperty =
        AvaloniaProperty.Register<CartesianChart, TimeSpan>(nameof(UpdateThrottleInterval), TimeSpan.FromMilliseconds(400));
    public static readonly StyledProperty<bool> ShowEmptyTextProperty =
        AvaloniaProperty.Register<CartesianChart, bool>(nameof(ShowEmptyText), true);
    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<CartesianChart, string>(nameof(EmptyText), "No data");
    public static readonly StyledProperty<IBrush> EmptyTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(EmptyTextBrush), Brushes.Gray);
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
    public static readonly StyledProperty<IBrush> ToolTipBackgroundProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(
            nameof(ToolTipBackground),
            new SolidColorBrush(Color.FromArgb(235, 32, 38, 46)));
    public static readonly StyledProperty<IBrush> ToolTipTextBrushProperty =
        AvaloniaProperty.Register<CartesianChart, IBrush>(nameof(ToolTipTextBrush), Brushes.White);
    public static readonly StyledProperty<double> ToolTipFontSizeProperty =
        AvaloniaProperty.Register<CartesianChart, double>(nameof(ToolTipFontSize), 11.0);
    public static readonly StyledProperty<Func<double, string>?> YAxisLabelFormatterProperty =
        AvaloniaProperty.Register<CartesianChart, Func<double, string>?>(nameof(YAxisLabelFormatter));
    public static readonly StyledProperty<Func<IChartDataPoint, string>?> ToolTipFormatterProperty =
        AvaloniaProperty.Register<CartesianChart, Func<IChartDataPoint, string>?>(nameof(ToolTipFormatter));

    private readonly ChartAnimationController _animation = new();
    private readonly ChartDataObserver _dataObserver;
    private readonly ChartUpdateScheduler _updateScheduler;
    private List<IChartDataPoint> _items = [];
    private TimeSpan? _animationStartTime;
    private bool _animationFrameRequested;
    private bool _isSynchronizingSelection;
    private int _hoveredIndex = -1;
    private Point _pointerPosition;

    protected CartesianChart()
    {
        _dataObserver = new ChartDataObserver(OnObservedCollectionChanged, OnObservedItemChanged);
        _updateScheduler = new ChartUpdateScheduler(ApplyTargetItems);
        Focusable = true;
    }

    static CartesianChart()
    {
        AffectsRender<CartesianChart>(
            BoundsProperty, ValuesProperty, ItemsSourceProperty, MinValueProperty, MaxValueProperty,
            AutoRangeProperty, AutoRangePaddingRatioProperty, GridLineBrushProperty, ShowGridLinesProperty,
            ShowThresholdsProperty, ThresholdsProperty, ThresholdDirectionProperty,
            ShowThresholdLabelsProperty, ThresholdLabelFontSizeProperty,
            ShowXAxisProperty, XAxisLabelsProperty, XAxisLabelsSourceProperty, XAxisLabelModeProperty,
            XAxisLabelIntervalProperty, XAxisTextBrushProperty, XAxisFontSizeProperty, XAxisHeightProperty,
            ShowYAxisProperty, GridLineCountProperty, YAxisLabelsProperty, YAxisLabelsSourceProperty,
            YAxisWidthProperty, YAxisFontSizeProperty, YAxisTextBrushProperty,
            ShowEmptyTextProperty, EmptyTextProperty, EmptyTextBrushProperty, EmptyTextFontSizeProperty,
            SelectedIndexProperty, SelectedItemProperty, IsToolTipEnabledProperty, ToolTipBackgroundProperty,
            ToolTipTextBrushProperty, ToolTipFontSizeProperty, YAxisLabelFormatterProperty, ToolTipFormatterProperty);
    }

    #region Properties
    public IEnumerable<double>? Values { get => GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public IEnumerable<IChartDataPoint>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public double MinValue { get => GetValue(MinValueProperty); set => SetValue(MinValueProperty, value); }
    public double MaxValue { get => GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }
    public bool AutoRange { get => GetValue(AutoRangeProperty); set => SetValue(AutoRangeProperty, value); }
    public double AutoRangePaddingRatio { get => GetValue(AutoRangePaddingRatioProperty); set => SetValue(AutoRangePaddingRatioProperty, value); }
    public IBrush GridLineBrush { get => GetValue(GridLineBrushProperty); set => SetValue(GridLineBrushProperty, value); }
    public bool ShowGridLines { get => GetValue(ShowGridLinesProperty); set => SetValue(ShowGridLinesProperty, value); }
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
    public int AnimationItemLimit { get => GetValue(AnimationItemLimitProperty); set => SetValue(AnimationItemLimitProperty, value); }
    public TimeSpan UpdateThrottleInterval { get => GetValue(UpdateThrottleIntervalProperty); set => SetValue(UpdateThrottleIntervalProperty, value); }
    public bool ShowEmptyText { get => GetValue(ShowEmptyTextProperty); set => SetValue(ShowEmptyTextProperty, value); }
    public string EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public IBrush EmptyTextBrush { get => GetValue(EmptyTextBrushProperty); set => SetValue(EmptyTextBrushProperty, value); }
    public double EmptyTextFontSize { get => GetValue(EmptyTextFontSizeProperty); set => SetValue(EmptyTextFontSizeProperty, value); }
    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public IChartDataPoint? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public ICommand? ItemClickCommand { get => GetValue(ItemClickCommandProperty); set => SetValue(ItemClickCommandProperty, value); }
    public bool IsToolTipEnabled { get => GetValue(IsToolTipEnabledProperty); set => SetValue(IsToolTipEnabledProperty, value); }
    public IBrush ToolTipBackground { get => GetValue(ToolTipBackgroundProperty); set => SetValue(ToolTipBackgroundProperty, value); }
    public IBrush ToolTipTextBrush { get => GetValue(ToolTipTextBrushProperty); set => SetValue(ToolTipTextBrushProperty, value); }
    public double ToolTipFontSize { get => GetValue(ToolTipFontSizeProperty); set => SetValue(ToolTipFontSizeProperty, value); }
    public Func<double, string>? YAxisLabelFormatter { get => GetValue(YAxisLabelFormatterProperty); set => SetValue(YAxisLabelFormatterProperty, value); }
    public Func<IChartDataPoint, string>? ToolTipFormatter { get => GetValue(ToolTipFormatterProperty); set => SetValue(ToolTipFormatterProperty, value); }
    #endregion

    protected IReadOnlyList<IChartDataPoint> ChartItems => _items;

    protected IReadOnlyList<double> AnimatedValues => _animation.Values;

    protected int HoveredIndex => _hoveredIndex;

    protected Point PointerPosition => _pointerPosition;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        InvalidateChartState();
        if (change.Property == ValuesProperty || change.Property == ItemsSourceProperty)
        {
            SubscribeToCollections();
            HandleNewData();
        }
        else if (change.Property == XAxisLabelsSourceProperty ||
                 change.Property == YAxisLabelsSourceProperty ||
                 change.Property == ThresholdsProperty)
        {
            SubscribeToCollections();
            InvalidateVisual();
        }
        else if (change.Property == IsAnimationEnabledProperty && !IsAnimationEnabled)
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
    }

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
        _dataObserver.Dispose();
        _updateScheduler.Stop();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _pointerPosition = e.GetPosition(this);
        int index = HitTestDataPoint(_pointerPosition);
        if (index != _hoveredIndex)
        {
            _hoveredIndex = index;
            InvalidateVisual();
        }
        else if (_hoveredIndex >= 0 && IsToolTipEnabled)
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoveredIndex = -1;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        int index = HitTestDataPoint(e.GetPosition(this));
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
        return new Size(width, height);
    }

    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderChart(context);
    }

    protected abstract void RenderChart(DrawingContext context);

    protected abstract int HitTestDataPoint(Point point);

    protected virtual void InvalidateChartState()
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

    private void SubscribeToCollections()
    {
        _dataObserver.ObserveCollections(
            ItemsSource ?? (object?)Values,
            XAxisLabelsSource,
            YAxisLabelsSource,
            Thresholds);
    }

    private void OnObservedCollectionChanged(object? sender)
    {
        bool affectsOnlyRendering = ReferenceEquals(sender, XAxisLabelsSource) ||
                                    ReferenceEquals(sender, YAxisLabelsSource) ||
                                    ReferenceEquals(sender, Thresholds);
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(affectsOnlyRendering ? InvalidateChart : HandleNewData);
            return;
        }

        if (affectsOnlyRendering)
        {
            InvalidateChart();
            return;
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

    private void HandleNewData()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(HandleNewData);
            return;
        }

        _updateScheduler.Schedule(
            ChartDataPipeline.BuildItems(ItemsSource, Values, XAxisLabelsSource, XAxisLabels),
            UpdateThrottleInterval);
    }

    private void ApplyTargetItems(List<IChartDataPoint> items)
    {
        _items = items;
        _dataObserver.ObserveItems(items);
        InvalidateChartState();
        _animationStartTime = null;

        if (_animation.SetTargets(ChartDataPipeline.GetFiniteValues(items), ShouldAnimate(items.Count)))
        {
            StartAnimationTimer();
        }

        SynchronizeSelectionAfterItemsChanged();
        _hoveredIndex = _hoveredIndex < _items.Count ? _hoveredIndex : -1;
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
        bool continues = _animation.Advance(progress);
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

    private void InvalidateChart()
    {
        InvalidateChartState();
        InvalidateVisual();
    }
}

#pragma warning restore CS1591
