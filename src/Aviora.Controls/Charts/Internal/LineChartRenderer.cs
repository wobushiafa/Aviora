using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

internal sealed class LineChartRenderer : CartesianChartRenderer<LineChart>
{
    private HitSeries[]? _hitSeries;
    private ChartRenderState? _hitState;
    private LineSeriesSegments[]? _cachedSegments;
    private ChartRenderState? _cachedSegmentsState;
    private LineSeriesGeometry[]? _cachedGeometry;
    private ChartRenderState? _cachedGeometryState;
    private Pen? _cachedPointPen;
    private ChartRenderState? _cachedPointPenState;

    public override void Invalidate()
    {
        base.Invalidate();
        _hitSeries = null;
        _hitState = null;
        _cachedSegments = null;
        _cachedSegmentsState = null;
        _cachedGeometry = null;
        _cachedGeometryState = null;
        _cachedPointPen = null;
        _cachedPointPenState = null;
    }

    protected override int GetCategoryCount(IReadOnlyList<IChartDataPoint> items)
    {
        int categoryCount = 0;
        bool hasSeries = false;
        foreach (IChartDataPoint item in items)
        {
            if (item is ChartDataPipeline.SeriesDataPoint series)
            {
                hasSeries = true;
                categoryCount = Math.Max(categoryCount, series.PointIndex + 1);
            }
        }

        return hasSeries ? categoryCount : base.GetCategoryCount(items);
    }

    protected override IReadOnlyList<string> GetDefaultXAxisLabels(IReadOnlyList<IChartDataPoint> items)
    {
        var labels = new SortedDictionary<int, string>();
        foreach (IChartDataPoint item in items)
        {
            if (item is ChartDataPipeline.SeriesDataPoint series && !labels.ContainsKey(series.PointIndex))
            {
                labels.Add(series.PointIndex, series.Label ?? string.Empty);
            }
        }

        return labels.Count > 0 ? labels.Values.ToList() : base.GetDefaultXAxisLabels(items);
    }

    public override int HitTest(LineChart chart, IReadOnlyList<IChartDataPoint> items, Point point)
    {
        ChartRenderState state = EnsureState(chart, items);
        const double hitTolerance = 24;
        Rect plot = state.Layout.Plot;
        var hitBounds = new Rect(
            plot.X - hitTolerance,
            plot.Y - hitTolerance,
            plot.Width + (hitTolerance * 2),
            plot.Height + (hitTolerance * 2));
        if (!hitBounds.Contains(point) || state.CategoryCount == 0)
        {
            return -1;
        }

        EnsureHitSeries(state);
        int nearestIndex = -1;
        double nearestDistanceSquared = double.PositiveInfinity;
        foreach (HitSeries series in _hitSeries!)
        {
            HitPoint[] points = series.Points;
            if (points.Length == 0)
            {
                continue;
            }

            int insertion = LowerBound(points, point.X);
            int start = Math.Max(0, insertion - 1);
            int end = Math.Min(points.Length - 1, insertion + 1);
            for (int index = start; index <= end; index++)
            {
                HitPoint candidate = points[index];
                double candidateDistanceSquared = DistanceToPointSquared(candidate.Position, point);
                Consider(candidate.Index, candidateDistanceSquared);
                if (index > 0)
                {
                    HitPoint previous = points[index - 1];
                    double previousDistanceSquared = DistanceToPointSquared(previous.Position, point);
                    Consider(
                        previousDistanceSquared <= candidateDistanceSquared ? previous.Index : candidate.Index,
                        DistanceToSegmentSquared(point, previous.Position, candidate.Position));
                }
            }
        }

        return nearestIndex;

        void Consider(int index, double distanceSquared)
        {
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestIndex = index;
                nearestDistanceSquared = distanceSquared;
            }
        }
    }

    protected override void DrawSeries(
        DrawingContext context,
        LineChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        LineSeriesSegments[] series = GetSegments(chart, state, animatedValues);
        double baseline = MapY(Math.Clamp(0, state.Scale.Minimum, state.Scale.Maximum), state);
        double lineThickness = NormalizeNonNegative(chart.LineThickness);
        LineSeriesGeometry[]? geometries = lineThickness > 0 || chart.AreaFillBrush != null
            ? GetGeometries(chart, state, series, baseline)
            : null;
        using (context.PushClip(state.Layout.Plot))
        {
            for (int seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
            {
                LineSeriesSegments current = series[seriesIndex];
                if (!current.HasItems)
                {
                    continue;
                }

                LineSeriesGeometry? geometry = geometries?[seriesIndex];
                if (chart.AreaFillBrush != null && geometry != null)
                {
                    foreach (StreamGeometry area in geometry.Areas)
                    {
                        context.DrawGeometry(chart.AreaFillBrush, null, area);
                    }
                }

                if (lineThickness > 0 && geometry != null)
                {
                    foreach (StreamGeometry line in geometry.Lines)
                    {
                        context.DrawGeometry(null, geometry.LinePen, line);
                    }
                }
            }
        }

        DrawPoints(context, chart, state, series);
    }

    private void EnsureHitSeries(ChartRenderState state)
    {
        if (_hitState == state && _hitSeries != null)
        {
            return;
        }

        int seriesCount = GetSeriesCount(state.Items);
        var pointsBySeries = new List<HitPoint>[seriesCount];
        for (int index = 0; index < state.Items.Count; index++)
        {
            IChartDataPoint item = state.Items[index];
            if (!double.IsFinite(item.Value))
            {
                continue;
            }

            int seriesIndex = GetSeriesIndex(item, seriesCount);
            (pointsBySeries[seriesIndex] ??= []).Add(new HitPoint(
                index,
                GetPointIndex(item, index),
                new Point(
                    GetItemCenterX(state.Layout, GetPointIndex(item, index)),
                    MapY(item.Value, state))));
        }

        var result = new HitSeries[seriesCount];
        for (int index = 0; index < seriesCount; index++)
        {
            List<HitPoint>? points = pointsBySeries[index];
            if (points == null)
            {
                result[index] = new HitSeries([]);
                continue;
            }

            points.Sort(static (left, right) => left.PointIndex.CompareTo(right.PointIndex));
            result[index] = new HitSeries(points.ToArray());
        }

        _hitSeries = result;
        _hitState = state;
    }

    private LineSeriesSegments[] GetSegments(
        LineChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        if (!chart.IsAnimationRunning && _cachedSegmentsState == state && _cachedSegments != null)
        {
            return _cachedSegments;
        }

        int seriesCount = GetSeriesCount(state.Items);
        var segmentsBySeries = new List<List<LinePoint>>?[seriesCount];
        var currentBySeries = new List<LinePoint>?[seriesCount];
        var brushes = new IBrush?[seriesCount];
        var hasItems = new bool[seriesCount];
        for (int index = 0; index < state.Items.Count; index++)
        {
            IChartDataPoint item = state.Items[index];
            int seriesIndex = GetSeriesIndex(item, seriesCount);
            hasItems[seriesIndex] = true;
            brushes[seriesIndex] ??= (item as ChartDataPipeline.SeriesDataPoint)?.LineBrush ?? chart.LineBrush;
            if (index >= animatedValues.Count || !double.IsFinite(item.Value))
            {
                currentBySeries[seriesIndex] = null;
                continue;
            }

            List<List<LinePoint>> segments = segmentsBySeries[seriesIndex] ??= [];
            List<LinePoint> current = currentBySeries[seriesIndex] ??= [];
            if (current.Count == 0)
            {
                segments.Add(current);
            }

            double value = Math.Clamp(animatedValues[index], state.Scale.Minimum, state.Scale.Maximum);
            current.Add(new LinePoint(
                index,
                new Point(GetItemCenterX(state.Layout, GetPointIndex(item, index)), MapY(value, state))));
        }

        var result = new LineSeriesSegments[seriesCount];
        for (int index = 0; index < seriesCount; index++)
        {
            result[index] = new LineSeriesSegments(
                brushes[index] ?? chart.LineBrush,
                segmentsBySeries[index] ?? [],
                hasItems[index]);
        }

        if (!chart.IsAnimationRunning)
        {
            _cachedSegments = result;
            _cachedSegmentsState = state;
        }

        return result;
    }

    private LineSeriesGeometry[] GetGeometries(
        LineChart chart,
        ChartRenderState state,
        LineSeriesSegments[] series,
        double baseline)
    {
        if (!chart.IsAnimationRunning && _cachedGeometryState == state && _cachedGeometry != null)
        {
            return _cachedGeometry;
        }

        var result = new LineSeriesGeometry[series.Length];
        for (int seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            LineSeriesSegments current = series[seriesIndex];
            var lines = new List<StreamGeometry>();
            var areas = new List<StreamGeometry>();
            foreach (List<LinePoint> segment in current.Segments)
            {
                if (segment.Count > 1)
                {
                    lines.Add(BuildGeometry(segment, chart.InterpolationMode, isArea: false, baseline));
                }

                if (chart.AreaFillBrush != null && segment.Count > 0)
                {
                    areas.Add(BuildGeometry(segment, chart.InterpolationMode, isArea: true, baseline));
                }
            }

            result[seriesIndex] = new LineSeriesGeometry(
                lines.ToArray(),
                areas.ToArray(),
                NormalizeNonNegative(chart.LineThickness) > 0
                    ? new Pen(current.LineBrush, NormalizeNonNegative(chart.LineThickness))
                    : null);
        }

        if (!chart.IsAnimationRunning)
        {
            _cachedGeometry = result;
            _cachedGeometryState = state;
        }

        return result;
    }

    private void DrawPoints(
        DrawingContext context,
        LineChart chart,
        ChartRenderState state,
        IReadOnlyList<LineSeriesSegments> series)
    {
        if (!chart.ShowPoints && chart.SelectedIndex < 0 && chart.SelectedItem == null)
        {
            return;
        }

        double defaultRadius = NormalizeNonNegative(chart.PointRadius);
        double strokeThickness = NormalizeNonNegative(chart.PointStrokeThickness);
        bool hasPen = chart.PointStrokeBrush != null && strokeThickness > 0;
        Pen? pen;
        if (!hasPen)
        {
            pen = null;
        }
        else if (!chart.IsAnimationRunning && _cachedPointPenState == state)
        {
            pen = _cachedPointPen;
        }
        else
        {
            pen = new Pen(chart.PointStrokeBrush!, strokeThickness);
            if (!chart.IsAnimationRunning)
            {
                _cachedPointPen = pen;
                _cachedPointPenState = state;
            }
        }
        for (int seriesIndex = 0; seriesIndex < series.Count; seriesIndex++)
        {
            foreach (List<LinePoint> segment in series[seriesIndex].Segments)
            {
                foreach (LinePoint point in segment)
                {
                    IChartDataPoint item = state.Items[point.Index];
                    bool isSelected = chart.SelectedIndex == point.Index || ReferenceEquals(chart.SelectedItem, item);
                    bool hasSelectedRadius = isSelected &&
                                             double.IsFinite(chart.SelectedPointRadius) &&
                                             chart.SelectedPointRadius > 0;
                    bool hasSelectedBrush = isSelected && chart.SelectedPointBrush != null;
                    if (!chart.ShowPoints && !hasSelectedRadius && !hasSelectedBrush)
                    {
                        continue;
                    }

                    double radius = hasSelectedRadius ? chart.SelectedPointRadius : defaultRadius;
                    if (radius <= 0)
                    {
                        continue;
                    }

                    IBrush brush = hasSelectedBrush
                        ? chart.SelectedPointBrush!
                        : ResolvePointBrush(chart, state, item);
                    context.DrawEllipse(brush, pen, point.Position, radius, radius);
                }
            }
        }
    }

    private static IBrush ResolvePointBrush(
        LineChart chart,
        ChartRenderState state,
        IChartDataPoint item)
    {
        if (item.Brush != null)
        {
            return item.Brush;
        }

        IBrush defaultBrush = (item as ChartDataPipeline.SeriesDataPoint)?.PointBrush ??
                              (item as ChartDataPipeline.SeriesDataPoint)?.LineBrush ??
                              chart.PointBrush ?? chart.LineBrush;
        return chart.ShowThresholds
            ? ChartThresholdResolver.Resolve(
                item.Value,
                defaultBrush,
                state.Thresholds,
                chart.ThresholdDirection)
            : defaultBrush;
    }

    private static StreamGeometry BuildGeometry(
        IReadOnlyList<LinePoint> segment,
        LineInterpolationMode interpolationMode,
        bool isArea,
        double baseline)
    {
        var geometry = new StreamGeometry();
        using StreamGeometryContext geometryContext = geometry.Open();
        Point first = segment[0].Position;
        if (isArea)
        {
            geometryContext.BeginFigure(new Point(first.X, baseline), true);
            geometryContext.LineTo(first, true);
        }
        else
        {
            geometryContext.BeginFigure(first, false);
        }

        AppendPath(geometryContext, segment, interpolationMode);
        if (isArea)
        {
            Point last = segment[^1].Position;
            geometryContext.LineTo(new Point(last.X, baseline), true);
        }

        geometryContext.EndFigure(isArea);
        return geometry;
    }

    private static void AppendPath(
        StreamGeometryContext context,
        IReadOnlyList<LinePoint> points,
        LineInterpolationMode interpolationMode)
    {
        for (int index = 0; index < points.Count - 1; index++)
        {
            Point current = points[index].Position;
            Point next = points[index + 1].Position;
            if (interpolationMode == LineInterpolationMode.Linear)
            {
                context.LineTo(next, true);
                continue;
            }

            Point previous = index > 0 ? points[index - 1].Position : current;
            Point following = index + 2 < points.Count ? points[index + 2].Position : next;
            (Point firstControl, Point secondControl) = GetBezierControls(previous, current, next, following);
            context.CubicBezierTo(firstControl, secondControl, next, true);
        }
    }

    internal static (Point First, Point Second) GetBezierControls(
        Point previous,
        Point current,
        Point next,
        Point following)
    {
        const double tension = 1.0 / 6.0;
        var first = new Point(
            current.X + ((next.X - previous.X) * tension),
            current.Y + ((next.Y - previous.Y) * tension));
        var second = new Point(
            next.X - ((following.X - current.X) * tension),
            next.Y - ((following.Y - current.Y) * tension));
        return (first, second);
    }

    private static double DistanceToPointSquared(Point first, Point second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        return (dx * dx) + (dy * dy);
    }

    private static double DistanceToSegmentSquared(Point point, Point start, Point end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared <= double.Epsilon)
        {
            return DistanceToPointSquared(point, start);
        }

        double projection = Math.Clamp((((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / lengthSquared, 0, 1);
        double closestX = start.X + (projection * dx);
        double closestY = start.Y + (projection * dy);
        return DistanceToPointSquared(point, new Point(closestX, closestY));
    }

    private static int LowerBound(IReadOnlyList<HitPoint> points, double x)
    {
        int low = 0;
        int high = points.Count;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (points[middle].Position.X < x)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private static int GetSeriesCount(IReadOnlyList<IChartDataPoint> items)
    {
        int count = 1;
        foreach (IChartDataPoint item in items)
        {
            if (item is ChartDataPipeline.SeriesDataPoint series)
            {
                count = Math.Max(count, series.SeriesIndex + 1);
            }
        }

        return count;
    }

    private static int GetSeriesIndex(IChartDataPoint item, int seriesCount) =>
        item is ChartDataPipeline.SeriesDataPoint series
            ? Math.Clamp(series.SeriesIndex, 0, seriesCount - 1)
            : 0;

    private static int GetPointIndex(IChartDataPoint item, int fallback) =>
        item is ChartDataPipeline.SeriesDataPoint series ? series.PointIndex : fallback;

    private static double NormalizeNonNegative(double value) =>
        double.IsFinite(value) && value > 0 ? value : 0;

    private readonly record struct HitPoint(int Index, int PointIndex, Point Position);
    private readonly record struct HitSeries(HitPoint[] Points);
    private readonly record struct LinePoint(int Index, Point Position);
    private sealed record LineSeriesSegments(IBrush LineBrush, List<List<LinePoint>> Segments, bool HasItems);
    private sealed record LineSeriesGeometry(StreamGeometry[] Lines, StreamGeometry[] Areas, Pen? LinePen);
}
