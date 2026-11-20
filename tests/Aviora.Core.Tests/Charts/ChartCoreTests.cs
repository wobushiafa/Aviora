using Aviora.Core.Charts;

namespace Aviora.Core.Tests;

public class ChartCoreTests
{
    [Fact]
    public void ChartPoint_implements_the_framework_independent_contract()
    {
        IChartPoint point = new ChartPoint
        {
            Key = "sales",
            Label = "Sales",
            Value = 42,
            ToolTip = "Sales: 42",
        };

        Assert.Equal("sales", point.Key);
        Assert.Equal("Sales", point.Label);
        Assert.Equal(42, point.Value);
        Assert.Equal("Sales: 42", point.ToolTip);
    }

    [Fact]
    public void Higher_values_match_the_highest_crossed_rule_regardless_of_order()
    {
        ThresholdRule[] rules =
        [
            new ThresholdRule { Value = 80, Label = "Danger" },
            new ThresholdRule { Value = 40, Label = "Normal" },
            new ThresholdRule { Value = 65, Label = "Warning" },
        ];

        ThresholdRule? result = ThresholdResolver.Resolve(
            72,
            rules,
            ThresholdDirection.HigherIsMoreSevere);

        Assert.Equal("Warning", result?.Label);
    }

    [Fact]
    public void Lower_values_match_the_lowest_crossed_rule_regardless_of_order()
    {
        ThresholdRule[] rules =
        [
            new ThresholdRule { Value = 30, Label = "Danger" },
            new ThresholdRule { Value = 50, Label = "Normal" },
            new ThresholdRule { Value = 42, Label = "Warning" },
        ];

        ThresholdRule? result = ThresholdResolver.Resolve(
            36,
            rules,
            ThresholdDirection.LowerIsMoreSevere);

        Assert.Equal("Warning", result?.Label);
    }

    [Fact]
    public void Threshold_resolver_ignores_non_finite_rules()
    {
        ThresholdRule[] rules =
        [
            new ThresholdRule { Value = double.NaN, Label = "Invalid" },
            new ThresholdRule { Value = 10, Label = "Valid" },
        ];

        ThresholdRule? result = ThresholdResolver.Resolve(
            20,
            rules,
            ThresholdDirection.HigherIsMoreSevere);

        Assert.Equal("Valid", result?.Label);
    }

    [Fact]
    public void Automatic_axis_includes_zero_and_uses_descending_ticks()
    {
        ChartAxisScale scale = ChartAxisCalculator.Calculate(
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
    public void Fixed_axis_normalizes_an_invalid_range()
    {
        ChartAxisScale scale = ChartAxisCalculator.Calculate(
            [],
            autoRange: false,
            minimum: 10,
            maximum: 5,
            desiredTickCount: 3);

        Assert.Equal(10, scale.Minimum);
        Assert.Equal(11, scale.Maximum);
        Assert.Equal([11, 10.5, 10], scale.Ticks);
    }
}
