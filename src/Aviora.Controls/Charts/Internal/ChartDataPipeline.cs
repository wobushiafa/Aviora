using System.Globalization;

namespace Aviora.Controls;

internal static class ChartDataPipeline
{
    public static List<IChartDataPoint> BuildItems(
        IEnumerable<IChartDataPoint>? itemsSource,
        IEnumerable<double>? values,
        IEnumerable<string>? labelSource,
        string? fallbackLabels)
    {
        if (itemsSource != null)
        {
            return itemsSource.Where(item => item != null).ToList();
        }

        List<double> materializedValues = values?.ToList() ?? [];
        List<string> labels = ResolveLabels(labelSource, fallbackLabels);
        bool labelsMapOneToOne = labels.Count == materializedValues.Count;
        var items = new List<IChartDataPoint>(materializedValues.Count);
        for (int index = 0; index < materializedValues.Count; index++)
        {
            string label = labelsMapOneToOne && !string.IsNullOrWhiteSpace(labels[index])
                ? labels[index]
                : (index + 1).ToString(CultureInfo.InvariantCulture);
            items.Add(new ChartDataPoint
            {
                Key = index,
                Label = label,
                Value = materializedValues[index],
            });
        }

        return items;
    }

    public static List<string> ResolveLabels(IEnumerable<string>? source, string? fallback)
    {
        if (source != null)
        {
            return source.ToList();
        }

        return string.IsNullOrWhiteSpace(fallback)
            ? []
            : fallback.Split(',', StringSplitOptions.TrimEntries).ToList();
    }

    public static List<double> GetFiniteValues(IEnumerable<IChartDataPoint> items) =>
        items.Select(item => double.IsFinite(item.Value) ? item.Value : 0).ToList();
}
