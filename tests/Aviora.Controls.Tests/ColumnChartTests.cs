using System.ComponentModel;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Aviora.Controls;
using CoreChartAxisScale = Aviora.Core.Charts.ChartAxisScale;
using CoreIChartPoint = Aviora.Core.Charts.IChartPoint;

namespace Aviora.Controls.Tests;

public class ColumnChartTests
{
    [Fact]
    public void ChartDataPoint_implements_the_public_data_contract()
    {
        IChartDataPoint point = new ChartDataPoint
        {
            Key = "sales",
            Label = "Sales",
            Value = 42,
            ColumnBackgroundBrush = Brushes.LightGray,
            ToolTip = "Sales: 42",
        };

        Assert.Equal("sales", point.Key);
        Assert.Equal("Sales", point.Label);
        Assert.Equal(42, point.Value);
        Assert.Same(Brushes.LightGray, point.ColumnBackgroundBrush);
        Assert.Equal("Sales: 42", point.ToolTip);
        Assert.IsAssignableFrom<CoreIChartPoint>(point);
    }

    [Fact]
    public void ColumnChart_accepts_rich_data_points()
    {
        var points = new IChartDataPoint[]
        {
            new ChartDataPoint { Label = "A", Value = 10 },
            new ChartDataPoint { Label = "B", Value = 20 },
        };
        var chart = new ColumnChart { ItemsSource = points };

        Assert.Same(points, chart.ItemsSource);
    }

    [Fact]
    public void Higher_values_match_the_highest_crossed_threshold()
    {
        ChartThreshold[] thresholds =
        [
            new ChartThreshold { Value = 40, Brush = Brushes.Green },
            new ChartThreshold { Value = 80, Brush = Brushes.Red },
            new ChartThreshold { Value = 65, Brush = Brushes.Orange },
        ];

        IBrush result = ChartThresholdResolver.Resolve(
            72, Brushes.Gray, thresholds, ThresholdDirection.HigherIsMoreSevere);

        Assert.Same(Brushes.Orange, result);
    }

    [Fact]
    public void Lower_values_match_the_lowest_crossed_threshold()
    {
        ChartThreshold[] thresholds =
        [
            new ChartThreshold { Value = 50, Brush = Brushes.Green },
            new ChartThreshold { Value = 30, Brush = Brushes.Red },
            new ChartThreshold { Value = 42, Brush = Brushes.Orange },
        ];

        IBrush result = ChartThresholdResolver.Resolve(
            36, Brushes.Gray, thresholds, ThresholdDirection.LowerIsMoreSevere);

        Assert.Same(Brushes.Orange, result);
    }

    [Fact]
    public void ColumnChart_has_usable_axis_and_animation_defaults()
    {
        var chart = new ColumnChart();

        Assert.Equal(44, chart.YAxisWidth);
        Assert.Equal(26, chart.XAxisHeight);
        Assert.True(chart.IsAnimationEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(400), chart.UpdateThrottleInterval);
        Assert.True(chart.ShowThresholdLabels);
        Assert.Null(chart.SelectionOverlayBrush);
        Assert.Same(AvioraControlPalette.AccentStrong, chart.SelectionStrokeBrush);
        Assert.Null(chart.SelectedBarBrush);
        Assert.Equal(0, chart.SelectionStrokeThickness);
        Assert.Equal(new Thickness(7), chart.ToolTipPadding);
        Assert.Equal(new CornerRadius(4), chart.ToolTipCornerRadius);
        Assert.Null(chart.ToolTipBorderBrush);
        Assert.Equal(default, chart.ToolTipBorderThickness);
        Assert.Equal(10, chart.ToolTipHorizontalOffset);
        Assert.Equal(10, chart.ToolTipVerticalOffset);
    }

    [Fact]
    public void Shared_styled_properties_remain_accessible_through_column_chart()
    {
        Assert.Same(CartesianChart.ValuesProperty, ColumnChart.ValuesProperty);
        Assert.Same(CartesianChart.ItemsSourceProperty, ColumnChart.ItemsSourceProperty);
        Assert.Same(CartesianChart.YAxisWidthProperty, ColumnChart.YAxisWidthProperty);
        Assert.Same(CartesianChart.IsAnimationEnabledProperty, ColumnChart.IsAnimationEnabledProperty);
    }

    [Fact]
    public void ColumnChart_exposes_column_background_and_selection_styling()
    {
        var chart = new ColumnChart
        {
            ColumnBackgroundBrush = Brushes.LightGray,
            SelectedBarBrush = Brushes.Purple,
            SelectionOverlayBrush = Brushes.Transparent,
            SelectionStrokeBrush = Brushes.Black,
            SelectionStrokeThickness = 3,
        };

        Assert.Same(Brushes.LightGray, chart.ColumnBackgroundBrush);
        Assert.Same(Brushes.Purple, chart.SelectedBarBrush);
        Assert.Same(Brushes.Transparent, chart.SelectionOverlayBrush);
        Assert.Same(Brushes.Black, chart.SelectionStrokeBrush);
        Assert.Equal(3, chart.SelectionStrokeThickness);
    }

    [AvaloniaFact]
    public void SelectedIndex_and_SelectedItem_stay_synchronized()
    {
        IChartDataPoint[] points =
        [
            new ChartDataPoint { Key = "a", Value = 10 },
            new ChartDataPoint { Key = "b", Value = 20 },
        ];
        var chart = new ColumnChart
        {
            UpdateThrottleInterval = TimeSpan.Zero,
            IsAnimationEnabled = false,
            ItemsSource = points,
        };

        chart.SelectedIndex = 1;

        Assert.Same(points[1], chart.SelectedItem);

        chart.SelectedItem = points[0];

        Assert.Equal(0, chart.SelectedIndex);
    }

    [Fact]
    public void Selection_can_follow_an_item_with_the_same_key_after_refresh()
    {
        var original = new ChartDataPoint { Key = "stable", Value = 10 };
        var replacement = new ChartDataPoint { Key = "stable", Value = 20 };

        Assert.Equal(0, ChartSelectionState.FindIndex([replacement], original));
    }

    [Theory]
    [InlineData(Key.Right, -1, 0)]
    [InlineData(Key.Right, 0, 1)]
    [InlineData(Key.Left, 0, 0)]
    [InlineData(Key.End, 0, 2)]
    [InlineData(Key.Home, 2, 0)]
    public void Keyboard_navigation_selects_a_valid_item(Key key, int current, int expected)
    {
        Assert.Equal(expected, ChartSelectionState.Move(current, 3, key));
    }

    [Fact]
    public void Data_pipeline_maps_simple_values_and_fallback_labels()
    {
        List<IChartDataPoint> items = ChartDataPipeline.BuildItems(
            null,
            [12, 24],
            null,
            "First, Second");

        Assert.Collection(
            items,
            item =>
            {
                Assert.Equal("First", item.Label);
                Assert.Equal(12, item.Value);
            },
            item =>
            {
                Assert.Equal("Second", item.Label);
                Assert.Equal(24, item.Value);
            });
    }

    [Fact]
    public void Axis_calculator_includes_zero_and_builds_descending_ticks()
    {
        CoreChartAxisScale scale = ChartAxisCalculator.Calculate(
            [20, 80],
            autoRange: true,
            minimum: 0,
            maximum: 100,
            desiredTickCount: 5);

        Assert.True(scale.Minimum <= 0);
        Assert.True(scale.Maximum >= 80);
        Assert.True(scale.Ticks.Count >= 2);
        Assert.Equal(scale.Maximum, scale.Ticks[0]);
        Assert.Equal(scale.Minimum, scale.Ticks[^1]);
    }

    [Fact]
    public void Layout_calculator_reserves_axes_and_hit_tests_slots()
    {
        ChartLayout layout = ChartLayoutCalculator.Calculate(new ChartLayoutOptions(
            new Size(300, 200),
            ItemCount: 3,
            ShowXAxis: true,
            XAxisHeight: 20,
            ShowYAxis: true,
            YAxisWidth: 40,
            BarWidthRatio: 0.5,
            MinimumVerticalInset: 5,
            RequiredYAxisWidth: 0));

        Assert.Equal(new Rect(40, 5, 260, 170), layout.Plot);
        Assert.Equal(1, ChartLayoutCalculator.HitTest(layout, new Point(170, 50), 3));
        Assert.Equal(-1, ChartLayoutCalculator.HitTest(layout, new Point(20, 50), 3));
    }

    [Fact]
    public void Animation_controller_interpolates_and_completes_targets()
    {
        var animation = new ChartAnimationController();
        animation.SetTargets([10], animate: true);

        Assert.True(animation.Advance(0.5));
        Assert.Equal(8.75, animation.Values[0], 6);

        Assert.False(animation.Advance(1));
        Assert.Equal(10, animation.Values[0]);
    }

    [Fact]
    public void Data_observer_reports_item_property_changes_and_unsubscribes()
    {
        var point = new MutableChartDataPoint();
        int changeCount = 0;
        var observer = new ChartDataObserver(_ => { }, () => changeCount++);
        observer.ObserveItems([point]);

        point.Value = 42;

        Assert.Equal(1, changeCount);

        observer.Dispose();
        point.Value = 84;

        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void Text_layout_reuses_measurements_until_invalidated()
    {
        var layout = new ChartTextLayout();

        FormattedText first = layout.Format("42", 10, Brushes.Gray);
        FormattedText second = layout.Format("42", 10, Brushes.Gray);

        Assert.Same(first, second);

        layout.Clear();

        Assert.NotSame(first, layout.Format("42", 10, Brushes.Gray));
    }

    [Fact]
    public void Chart_value_formatter_preserves_default_axis_and_tooltip_formatting()
    {
        Assert.Equal("-", ChartValueFormatter.Format(double.NaN));
        Assert.Equal("12.35", ChartValueFormatter.Format(12.346));
        Assert.Equal("1.5E+6", ChartValueFormatter.Format(1_500_000));
    }

    [Fact]
    public void Tooltip_state_keeps_one_anchor_while_pointer_stays_on_the_same_item()
    {
        var state = new ChartToolTipState();

        Assert.True(state.Update(1, new Point(100, 80)));
        Assert.False(state.Update(1, new Point(120, 95)));

        Assert.Equal(1, state.HoveredIndex);
        Assert.Equal(new Point(100, 80), state.AnchorPosition);

        Assert.True(state.Update(2, new Point(180, 70)));
        Assert.Equal(2, state.HoveredIndex);
        Assert.Equal(new Point(180, 70), state.AnchorPosition);
    }

    [Fact]
    public void Tooltip_style_properties_update_the_single_presenter()
    {
        var chart = new ColumnChart
        {
            ToolTipBackground = Brushes.Navy,
            ToolTipTextBrush = Brushes.Yellow,
            ToolTipFontSize = 14,
            ToolTipPadding = new Thickness(12, 8),
            ToolTipCornerRadius = new CornerRadius(6),
            ToolTipBorderBrush = Brushes.White,
            ToolTipBorderThickness = new Thickness(2),
        };

        Assert.Same(Brushes.Navy, chart.ToolTipPresenter.Background);
        Assert.Equal(new Thickness(12, 8), chart.ToolTipPresenter.Padding);
        Assert.Equal(new CornerRadius(6), chart.ToolTipPresenter.CornerRadius);
        Assert.Same(Brushes.White, chart.ToolTipPresenter.BorderBrush);
        Assert.Equal(new Thickness(2), chart.ToolTipPresenter.BorderThickness);
    }

    private sealed class MutableChartDataPoint : IChartDataPoint, INotifyPropertyChanged
    {
        private double _value;

        public object? Key => "mutable";

        public string? Label => "Mutable";

        public double Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value))
                {
                    return;
                }

                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public IBrush? Brush => null;

        public string? ToolTip => null;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
