using Avalonia;
using Avalonia.Media;
using Aviora.Controls;

namespace Aviora.Controls.Tests;

public class LineChartTests
{
    [Fact]
    public void LineChart_exposes_shared_cartesian_defaults_and_line_styling()
    {
        var chart = new LineChart
        {
            LineBrush = Brushes.Purple,
            LineThickness = 3,
            InterpolationMode = LineInterpolationMode.Linear,
            ShowPoints = false,
            AreaFillBrush = Brushes.Transparent,
            PointRadius = 5,
        };

        Assert.Equal(44, chart.YAxisWidth);
        Assert.Equal(26, chart.XAxisHeight);
        Assert.Same(Brushes.Purple, chart.LineBrush);
        Assert.Equal(3, chart.LineThickness);
        Assert.Equal(LineInterpolationMode.Linear, chart.InterpolationMode);
        Assert.False(chart.ShowPoints);
        Assert.Same(Brushes.Transparent, chart.AreaFillBrush);
        Assert.Equal(5, chart.PointRadius);
        Assert.True(chart.IsAnimationEnabled);
    }

    [Fact]
    public void Smooth_interpolation_generates_controls_around_the_segment()
    {
        (Point first, Point second) = LineChartRenderer.GetBezierControls(
            new Point(0, 0),
            new Point(10, 10),
            new Point(20, 0),
            new Point(30, 10));

        Assert.Equal(13.3333333333, first.X, precision: 8);
        Assert.Equal(10, first.Y, precision: 8);
        Assert.Equal(16.6666666667, second.X, precision: 8);
        Assert.Equal(0, second.Y, precision: 8);
    }

    [Fact]
    public void LineChart_uses_the_same_data_contract_as_column_chart()
    {
        IChartDataPoint[] points =
        [
            new ChartDataPoint { Key = "a", Label = "A", Value = 10 },
            new ChartDataPoint { Key = "b", Label = "B", Value = 20 },
        ];
        var chart = new LineChart
        {
            ItemsSource = points,
        };

        Assert.Same(points, chart.ItemsSource);
    }
}
