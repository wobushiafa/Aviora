using System.Collections;
using System.Globalization;
using Avalonia;
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
            IBrush? pointBrush,
            IBrush? selectedBrush = null)
        {
            _inner = inner;
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            LineBrush = lineBrush;
            AreaFillBrush = areaFillBrush;
            PointBrush = pointBrush;
            SelectedBrush = selectedBrush;
        }

        public int SeriesIndex { get; }
        public int PointIndex { get; }
        public IChartDataPoint Source => _inner;
        public IBrush? LineBrush { get; }
        public IBrush? AreaFillBrush { get; }
        public IBrush? PointBrush { get; }
        public IBrush? SelectedBrush { get; }
        public object? Key => _inner.Key;
        public string? Label => _inner.Label;
        public double Value => _inner.Value;
        public IBrush? Brush => _inner.Brush;
        public IBrush? ColumnBackgroundBrush => _inner.ColumnBackgroundBrush;
        public string? ToolTip => _inner.ToolTip;
    }

    internal sealed class AdaptedDataPoint : IChartDataPoint
    {
        public AdaptedDataPoint(
            object source,
            object? key,
            string? label,
            double value,
            string? toolTip,
            IBrush? brush)
        {
            Source = source;
            Key = key;
            Label = label;
            Value = value;
            ToolTip = toolTip;
            Brush = brush;
        }

        public object Source { get; }
        public object? Key { get; }
        public string? Label { get; }
        public double Value { get; }
        public IBrush? Brush { get; }
        public IBrush? ColumnBackgroundBrush => null;
        public string? ToolTip { get; }
    }

    internal sealed record ChartSeriesMeta(
        int SeriesIndex,
        string Title,
        IBrush Brush,
        bool IsVisible,
        AvaloniaObject? SeriesObject,
        LineChartSeries? LineSeries = null,
        ColumnChartSeries? ColumnSeries = null);

    public static List<IChartDataPoint> BuildSeriesItems(IEnumerable<LineChartSeries> series)
    {
        var result = new List<IChartDataPoint>();
        int seriesIndex = 0;
        foreach (LineChartSeries definition in series)
        {
            if (definition.IsVisible)
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
            }

            seriesIndex++;
        }

        return result;
    }

    public static List<IChartDataPoint> BuildColumnSeriesItems(IEnumerable<ColumnChartSeries> series)
    {
        var result = new List<IChartDataPoint>();
        int seriesIndex = 0;
        foreach (ColumnChartSeries definition in series)
        {
            if (definition.IsVisible)
            {
                int pointIndex = 0;
                foreach (IChartDataPoint item in BuildItems(definition.ItemsSource, definition.Values, null, null))
                {
                    result.Add(new SeriesDataPoint(
                        item,
                        seriesIndex,
                        pointIndex,
                        definition.Brush,
                        null,
                        null,
                        definition.SelectedBrush));
                    pointIndex++;
                }
            }

            seriesIndex++;
        }

        return result;
    }

    public static List<IChartDataPoint> BuildItems(
        IEnumerable<IChartDataPoint>? itemsSource,
        IEnumerable<double>? values,
        IEnumerable<string>? labelSource,
        string? fallbackLabels) =>
        BuildItems(
            itemsSource,
            dataSource: null,
            valueSelector: null,
            labelSelector: null,
            keySelector: null,
            toolTipSelector: null,
            brushSelector: null,
            values,
            labelSource,
            fallbackLabels);

    public static List<IChartDataPoint> BuildItems(
        IEnumerable<IChartDataPoint>? itemsSource,
        IEnumerable? dataSource,
        Func<object, double>? valueSelector,
        Func<object, string?>? labelSelector,
        Func<object, object?>? keySelector,
        Func<object, string?>? toolTipSelector,
        Func<object, IBrush?>? brushSelector,
        IEnumerable<double>? values,
        IEnumerable<string>? labelSource,
        string? fallbackLabels)
    {
        if (itemsSource != null)
        {
            if (itemsSource is ICollection<IChartDataPoint> col)
            {
                var list = new List<IChartDataPoint>(col.Count);
                foreach (IChartDataPoint item in col)
                {
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                return list;
            }

            return itemsSource.Where(item => item != null).ToList();
        }

        if (dataSource != null && valueSelector != null)
        {
            List<string> labelsFallback = ResolveLabels(labelSource, fallbackLabels);
            var adapted = new List<IChartDataPoint>();
            int index = 0;
            foreach (object? entry in dataSource)
            {
                if (entry == null)
                {
                    continue;
                }

                string? label = labelSelector?.Invoke(entry);
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = index < labelsFallback.Count && !string.IsNullOrWhiteSpace(labelsFallback[index])
                        ? labelsFallback[index]
                        : (index + 1).ToString(CultureInfo.InvariantCulture);
                }

                adapted.Add(new AdaptedDataPoint(
                    entry,
                    keySelector != null ? keySelector(entry) : entry,
                    label,
                    valueSelector(entry),
                    toolTipSelector?.Invoke(entry),
                    brushSelector?.Invoke(entry)));
                index++;
            }

            return adapted;
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

    public static List<double> GetFiniteValues(IEnumerable<IChartDataPoint> items)
    {
        var list = items is ICollection<IChartDataPoint> col
            ? new List<double>(col.Count)
            : new List<double>();
        foreach (IChartDataPoint item in items)
        {
            list.Add(double.IsFinite(item.Value) ? item.Value : 0);
        }

        return list;
    }
}
