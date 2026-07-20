using Avalonia;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal readonly record struct ChartLayout(Rect Plot, double SlotWidth, double BarWidth, double XAxisTop);

internal readonly record struct ChartLayoutOptions(
    Size Bounds,
    int ItemCount,
    bool ShowXAxis,
    double XAxisHeight,
    bool ShowYAxis,
    double YAxisWidth,
    double BarWidthRatio,
    double MinimumVerticalInset,
    double RequiredYAxisWidth);

internal static class ChartLayoutCalculator
{
    public static ChartLayout Calculate(ChartLayoutOptions options)
    {
        double width = Math.Max(0, options.Bounds.Width);
        double height = Math.Max(0, options.Bounds.Height);
        double xAxisHeight = options.ShowXAxis ? Math.Clamp(options.XAxisHeight, 0, height) : 0;
        double yAxisWidth = options.ShowYAxis
            ? Math.Clamp(Math.Max(options.YAxisWidth, options.RequiredYAxisWidth), 0, Math.Max(0, width - 1))
            : 0;
        double chartHeight = Math.Max(0, height - xAxisHeight);
        double verticalInset = Math.Clamp(options.MinimumVerticalInset, 0, chartHeight / 2);
        double plotHeight = Math.Max(0, chartHeight - (verticalInset * 2));
        double plotWidth = Math.Max(0, width - yAxisWidth);
        var plot = new Rect(yAxisWidth, verticalInset, plotWidth, plotHeight);
        double slotWidth = options.ItemCount > 0 ? plotWidth / options.ItemCount : 0;
        double barWidth = Math.Max(0, slotWidth * Math.Clamp(options.BarWidthRatio, 0.05, 1));
        return new ChartLayout(plot, slotWidth, barWidth, height - xAxisHeight + 4);
    }

    public static int HitTest(ChartLayout layout, Point point, int itemCount)
    {
        if (itemCount == 0 || !layout.Plot.Contains(point) || layout.SlotWidth <= 0)
        {
            return -1;
        }

        return Math.Clamp((int)((point.X - layout.Plot.Left) / layout.SlotWidth), 0, itemCount - 1);
    }

    public static double MapY(double value, Rect plot, ChartAxisScale scale)
    {
        if (scale.Range <= 0 || !double.IsFinite(scale.Range))
        {
            return plot.Bottom;
        }

        return plot.Top + ((scale.Maximum - value) / scale.Range * plot.Height);
    }
}
