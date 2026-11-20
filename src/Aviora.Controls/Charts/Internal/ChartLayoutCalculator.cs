using Avalonia;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal readonly record struct ChartLayout(
    Rect Plot,
    double SlotWidth,
    double BarWidth,
    double XAxisTop,
    int ViewportStartIndex = 0,
    int ViewportCount = 0);

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
    public static ChartLayout Calculate(ChartLayoutOptions options) =>
        Calculate(
            options,
            default,
            legendTopHeight: 0,
            legendBottomHeight: 0,
            zoomStartRatio: 0,
            zoomEndRatio: 1,
            minVisiblePoints: 3);

    public static ChartLayout Calculate(
        ChartLayoutOptions options,
        Thickness plotPadding,
        double legendTopHeight,
        double legendBottomHeight,
        double zoomStartRatio,
        double zoomEndRatio,
        int minVisiblePoints)
    {
        double width = Math.Max(0, options.Bounds.Width);
        double height = Math.Max(0, options.Bounds.Height);

        double padLeft = Math.Max(0, plotPadding.Left);
        double padTop = Math.Max(0, plotPadding.Top);
        double padRight = Math.Max(0, plotPadding.Right);
        double padBottom = Math.Max(0, plotPadding.Bottom);

        double topOffset = Math.Max(0, legendTopHeight) + padTop;
        double bottomReserved = Math.Max(0, legendBottomHeight) + padBottom;
        double availableHeight = Math.Max(0, height - topOffset - bottomReserved);
        double availableWidth = Math.Max(0, width - padLeft - padRight);

        double xAxisHeight = options.ShowXAxis ? Math.Clamp(options.XAxisHeight, 0, availableHeight) : 0;
        double yAxisWidth = options.ShowYAxis
            ? Math.Clamp(Math.Max(options.YAxisWidth, options.RequiredYAxisWidth), 0, Math.Max(0, availableWidth - 1))
            : 0;
        double chartHeight = Math.Max(0, availableHeight - xAxisHeight);
        double verticalInset = Math.Clamp(options.MinimumVerticalInset, 0, chartHeight / 2);
        double plotHeight = Math.Max(0, chartHeight - (verticalInset * 2));
        double plotWidth = Math.Max(0, availableWidth - yAxisWidth);
        var plot = new Rect(padLeft + yAxisWidth, topOffset + verticalInset, plotWidth, plotHeight);

        (int viewportStart, int viewportCount) = ResolveViewport(
            options.ItemCount,
            zoomStartRatio,
            zoomEndRatio,
            minVisiblePoints);

        double slotWidth = viewportCount > 0 ? plotWidth / viewportCount : 0;
        double barWidth = Math.Max(0, slotWidth * Math.Clamp(options.BarWidthRatio, 0.05, 1));
        double xAxisTop = topOffset + chartHeight + 4;
        return new ChartLayout(plot, slotWidth, barWidth, xAxisTop, viewportStart, viewportCount);
    }

    public static (int StartIndex, int Count) ResolveViewport(
        int categoryCount,
        double zoomStartRatio,
        double zoomEndRatio,
        int minVisiblePoints)
    {
        if (categoryCount <= 0)
        {
            return (0, 0);
        }

        double startRatio = double.IsFinite(zoomStartRatio) ? Math.Clamp(zoomStartRatio, 0, 1) : 0;
        double endRatio = double.IsFinite(zoomEndRatio) ? Math.Clamp(zoomEndRatio, startRatio, 1) : 1;
        if (startRatio <= 1e-6 && endRatio >= 1.0 - 1e-6)
        {
            return (0, categoryCount);
        }

        int minCount = Math.Min(categoryCount, Math.Max(1, minVisiblePoints));
        int startIndex = Math.Clamp((int)Math.Floor(startRatio * categoryCount), 0, Math.Max(0, categoryCount - minCount));
        int endIndex = Math.Clamp((int)Math.Ceiling(endRatio * categoryCount) - 1, startIndex + minCount - 1, categoryCount - 1);
        return (startIndex, endIndex - startIndex + 1);
    }

    public static int HitTest(ChartLayout layout, Point point, int itemCount)
    {
        if (itemCount == 0 || !layout.Plot.Contains(point) || layout.SlotWidth <= 0)
        {
            return -1;
        }

        int visibleCount = layout.ViewportCount > 0 ? layout.ViewportCount : itemCount;
        int localOffset = Math.Clamp((int)((point.X - layout.Plot.Left) / layout.SlotWidth), 0, visibleCount - 1);
        return Math.Clamp(layout.ViewportStartIndex + localOffset, 0, itemCount - 1);
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
