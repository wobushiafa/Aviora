using CoreCharts = Aviora.Core.Charts;

namespace Aviora.Controls;

internal static class ChartAxisCalculator
{
    public static CoreCharts.ChartAxisScale Calculate(
        IEnumerable<double> values,
        bool autoRange,
        double minimum,
        double maximum,
        int desiredTickCount,
        double paddingRatio = 0.08) =>
        CoreCharts.ChartAxisCalculator.Calculate(
            values,
            autoRange,
            minimum,
            maximum,
            desiredTickCount,
            paddingRatio);

    public static IReadOnlyList<int> SelectLabelIndices(
        IReadOnlyList<double> leftEdges,
        IReadOnlyList<double> rightEdges,
        ChartLabelMode mode,
        int interval,
        double spacing = 6)
    {
        int count = Math.Min(leftEdges.Count, rightEdges.Count);
        if (count == 0)
        {
            return [];
        }

        if (count <= 2 || mode == ChartLabelMode.All)
        {
            return Enumerable.Range(0, count).ToList();
        }

        if (mode == ChartLabelMode.Interval)
        {
            interval = Math.Max(1, interval);
            return Enumerable.Range(0, count)
                .Where(index => index == 0 || index == count - 1 || index % interval == 0)
                .ToList();
        }

        var selected = new List<int> { 0 };
        for (int index = 1; index < count - 1; index++)
        {
            if (leftEdges[index] >= rightEdges[selected[^1]] + spacing)
            {
                selected.Add(index);
            }
        }

        int lastIndex = count - 1;
        while (selected.Count > 1 && leftEdges[lastIndex] < rightEdges[selected[^1]] + spacing)
        {
            selected.RemoveAt(selected.Count - 1);
        }

        if (leftEdges[lastIndex] >= rightEdges[selected[^1]] + spacing || selected.Count == 1)
        {
            selected.Add(lastIndex);
        }

        return selected;
    }
}
