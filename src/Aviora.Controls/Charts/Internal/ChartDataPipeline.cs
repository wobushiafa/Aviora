using System.Globalization;
using Avalonia.Media;

namespace Aviora.Controls;

internal static class ChartDataPipeline
{
    internal sealed class SeriesDataPoint : IChartDataPoint
    {
        private readonly IChartDataPoint _inner;
        public SeriesDataPoint(
            IChartDataPoint inner,
            int seriesIndex,
            int pointIndex,
            IBrush? lineBrush,
            IBrush? areaFillBrush,
            IBrush? pointBrush)
        {
            _inner = inner;
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            LineBrush = lineBrush;
            AreaFillBrush = areaFillBrush;
            PointBrush = pointBrush;
        }
        public int SeriesIndex { get; }
        public int PointIndex { get; }
        public IChartDataPoint Source => _inner;
        public IBrush? LineBrush { get; }
        public IBrush? AreaFillBrush { get; }
        public IBrush? PointBrush { get; }
        public object? Key => _inner.Key;
        public string? Label => _inner.Label;
        public double Value => _inner.Value;
        public IBrush? Brush => _inner.Brush;
        public IBrush? ColumnBackgroundBrush => _inner.ColumnBackgroundBrush;
        public string? ToolTip => _inner.ToolTip;
    }

    public static List<IChartDataPoint> BuildSeriesItems(IEnumerable<LineChartSeries> series)
    {
        var result = new List<IChartDataPoint>();
        int seriesIndex = 0;
        foreach (LineChartSeries definition in series)
        {
            int pointIndex = 0;
            foreach (IChartDataPoint item in BuildItems(definition.ItemsSource, definition.Values, null, null))
            {
                result.Add(new SeriesDataPoint(
                    item,
                    seriesIndex,
                    pointIndex,
                    definition.LineBrush,
                    definition.AreaFillBrush,
                    definition.PointBrush));
                pointIndex++;
            }
            seriesIndex++;
        }
        return result;
    }
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
