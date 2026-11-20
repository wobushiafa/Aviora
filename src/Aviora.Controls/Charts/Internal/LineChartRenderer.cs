using System.Collections;
using Avalonia;
using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal sealed class LineChartRenderer : CartesianChartRenderer<LineChart>
{
    private readonly Dictionary<(ChartPointShape Shape, double Radius), StreamGeometry> _markerGeometryCache = [];
    private HitSeries[]? _hitSeries;
    private ChartRenderState? _hitState;
    private LineSeriesSegments[]? _cachedSegments;
    private ChartRenderState? _cachedSegmentsState;
    private LineSeriesGeometry[]? _cachedGeometry;
    private ChartRenderState? _cachedGeometryState;
    private Pen? _cachedPointPen;
    private IBrush? _cachedPointPenBrush;
    private double _cachedPointPenThickness;
    private ChartRenderState? _cachedPointPenState;

    public override void Invalidate(ChartInvalidationFlags flags)
    {
        base.Invalidate(flags);

        if ((flags & ChartInvalidationFlags.LayoutAndScale) != 0)
        {
            _hitSeries = null;
            _hitState = null;
        }

        if ((flags & (ChartInvalidationFlags.LayoutAndScale | ChartInvalidationFlags.SeriesGeometry)) != 0)
        {
            _cachedSegments = null;
            _cachedSegmentsState = null;
            _cachedGeometry = null;
            _cachedGeometryState = null;
            _cachedPointPen = null;
            _cachedPointPenBrush = null;
            _cachedPointPenThickness = 0;
            _cachedPointPenState = null;
        }
    }

    protected override int ResolveCategoryCount(LineChart chart, IReadOnlyList<IChartDataPoint> items)
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

        if (!hasSeries && chart.Series != null)
        {
            foreach (LineChartSeries series in chart.Series)
            {
                hasSeries = true;
                if (series.ItemsSource is ICollection itemsCol)
                {
                    categoryCount = Math.Max(categoryCount, itemsCol.Count);
                }
                else if (series.Values is ICollection valCol)
                {
                    categoryCount = Math.Max(categoryCount, valCol.Count);
                }
            }
        }

        return hasSeries ? categoryCount : base.ResolveCategoryCount(chart, items);
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
        bool needsGeometry = series.Any(current =>
            current.HasItems && (current.LineThickness > 0 || current.AreaFillBrush != null));
        LineSeriesGeometry[]? geometries = needsGeometry
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
                if (current.AreaFillBrush != null && geometry != null)
                {
                    foreach (StreamGeometry area in geometry.Areas)
                    {
                        context.DrawGeometry(current.AreaFillBrush, null, area);
                    }
                }

                if (current.LineThickness > 0 && geometry != null && geometry.LinePen != null)
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
        int viewStart = state.Layout.ViewportStartIndex;
        int viewEnd = viewStart + state.Layout.ViewportCount - 1;
        var pointsBySeries = new List<HitPoint>[seriesCount];
        for (int index = 0; index < state.Items.Count; index++)
        {
            IChartDataPoint item = state.Items[index];
            if (!double.IsFinite(item.Value))
            {
                continue;
            }

            int pointIndex = GetPointIndex(item, index);
            if (pointIndex < viewStart || pointIndex > viewEnd)
            {
                continue;
            }

            int seriesIndex = GetSeriesIndex(item, seriesCount);
            (pointsBySeries[seriesIndex] ??= []).Add(new HitPoint(
                index,
                pointIndex,
                new Point(
                    GetItemCenterX(state.Layout, pointIndex),
                    MapY(Math.Clamp(item.Value, state.Scale.Minimum, state.Scale.Maximum), state))));
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

        int seriesCount = Math.Max(GetSeriesCount(state.Items), state.SeriesMeta.Count);
        int viewStart = state.Layout.ViewportStartIndex;
        int viewEnd = viewStart + state.Layout.ViewportCount - 1;
        bool isZoomed = viewStart > 0 || state.Layout.ViewportCount < state.CategoryCount;
        int sliceMin = isZoomed ? Math.Max(0, viewStart - 1) : 0;
        int sliceMax = isZoomed ? Math.Min(state.CategoryCount - 1, viewEnd + 1) : state.CategoryCount - 1;

        var rawItemsBySeries = new List<(int ItemIndex, int PointIndex, double Value)>[seriesCount];
        var hasItems = new bool[seriesCount];

        for (int index = 0; index < state.Items.Count; index++)
        {
            IChartDataPoint item = state.Items[index];
            int seriesIndex = GetSeriesIndex(item, seriesCount);
            hasItems[seriesIndex] = true;

            int pointIndex = GetPointIndex(item, index);
            if (pointIndex < sliceMin || pointIndex > sliceMax)
            {
                continue;
            }

            double val = index < animatedValues.Count && double.IsFinite(item.Value)
                ? animatedValues[index]
                : double.NaN;
            (rawItemsBySeries[seriesIndex] ??= []).Add((index, pointIndex, val));
        }

        var result = new LineSeriesSegments[seriesCount];
        for (int seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
        {
            LineChartSeries? seriesDef = seriesIndex < state.SeriesMeta.Count
                ? state.SeriesMeta[seriesIndex].LineSeries
                : null;

            IBrush lineBrush = seriesDef?.LineBrush ?? chart.LineBrush;
            IBrush? areaBrush = seriesDef?.AreaFillBrush ?? chart.AreaFillBrush;
            double lineThickness = NormalizeNonNegative(seriesDef?.LineThickness ?? chart.LineThickness);
            IReadOnlyList<double>? dashArray = seriesDef?.StrokeDashArray ?? chart.LineDashArray;
            LineInterpolationMode interpolation = seriesDef?.InterpolationMode ?? chart.InterpolationMode;
            bool showPoints = seriesDef?.ShowPoints ?? chart.ShowPoints;
            double pointRadius = NormalizeNonNegative(seriesDef?.PointRadius ?? chart.PointRadius);
            ChartPointShape pointShape = seriesDef?.PointShape ?? chart.PointShape;
            IBrush? pointBrush = seriesDef?.PointBrush ?? chart.PointBrush;
            IBrush? pointStrokeBrush = seriesDef?.PointStrokeBrush ?? chart.PointStrokeBrush;
            double pointStrokeThickness = NormalizeNonNegative(seriesDef?.PointStrokeThickness ?? chart.PointStrokeThickness);

            List<(int ItemIndex, int PointIndex, double Value)>? raw = rawItemsBySeries[seriesIndex];
            var segments = new List<List<LinePoint>>();

            if (raw != null && raw.Count > 0)
            {
                IEnumerable<(int ItemIndex, int PointIndex, double Value)> pointsToBuild = raw;

                if (chart.DownsampleMode != ChartDownsampleMode.None &&
                    chart.DownsampleThreshold > 0 &&
                    raw.Count > chart.DownsampleThreshold)
                {
                    int targetCount = Math.Max(16, Math.Min((int)Math.Ceiling(state.Layout.Plot.Width * 2.0), chart.DownsampleThreshold));
                    if (ChartDownsampler.NeedsDownsampling(raw.Count, targetCount, chart.DownsampleMode))
                    {
                        var values = new double[raw.Count];
                        for (int i = 0; i < raw.Count; i++)
                        {
                            values[i] = raw[i].Value;
                        }

                        int[] picked = ChartDownsampler.SelectIndices(values, targetCount, chart.DownsampleMode);
                        var filtered = new List<(int ItemIndex, int PointIndex, double Value)>(picked.Length);
                        foreach (int idx in picked)
                        {
                            filtered.Add(raw[idx]);
                        }

                        pointsToBuild = filtered;
                    }
                }

                List<LinePoint>? current = null;
                foreach (var entry in pointsToBuild)
                {
                    if (!double.IsFinite(entry.Value))
                    {
                        current = null;
                        continue;
                    }

                    if (current == null)
                    {
                        current = [];
                        segments.Add(current);
                    }

                    double clamped = Math.Clamp(entry.Value, state.Scale.Minimum, state.Scale.Maximum);
                    current.Add(new LinePoint(
                        entry.ItemIndex,
                        entry.Value,
                        new Point(GetItemCenterX(state.Layout, entry.PointIndex), MapY(clamped, state))));
                }
            }

            result[seriesIndex] = new LineSeriesSegments(
                lineBrush,
                areaBrush,
                lineThickness,
                dashArray,
                interpolation,
                showPoints,
                pointRadius,
                pointShape,
                pointBrush,
                pointStrokeBrush,
                pointStrokeThickness,
                segments,
                hasItems[seriesIndex]);
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

        double tension = chart.LineTension;
        PenLineCap lineCap = chart.LineCap;
        var result = new LineSeriesGeometry[series.Length];
        for (int seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            LineSeriesSegments current = series[seriesIndex];
            var lines = new List<StreamGeometry>();
            var areas = new List<StreamGeometry>();
            foreach (List<LinePoint> segment in current.Segments)
            {
                if (segment.Count > 1 && current.LineThickness > 0)
                {
                    lines.Add(BuildGeometry(segment, current.InterpolationMode, tension, isArea: false, baseline));
                }

                if (current.AreaFillBrush != null && segment.Count > 0)
                {
                    areas.Add(BuildGeometry(segment, current.InterpolationMode, tension, isArea: true, baseline));
                }
            }

            Pen? pen = null;
            if (current.LineThickness > 0)
            {
                DashStyle? dash = current.DashArray is { Count: > 0 }
                    ? new DashStyle(current.DashArray, 0)
                    : null;
                pen = new Pen(current.LineBrush, current.LineThickness, dash, lineCap);
            }

            result[seriesIndex] = new LineSeriesGeometry(lines.ToArray(), areas.ToArray(), pen);
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
        bool anySeriesShowsPoints = series.Any(s => s.ShowPoints);
        bool hasHoverHighlight = chart.HoveredItemIndex >= 0 &&
                                 (chart.HoverHaloBrush != null ||
                                  (double.IsFinite(chart.HoverPointRadius) && chart.HoverPointRadius > 0));
        if (!anySeriesShowsPoints && chart.SelectedIndex < 0 && chart.SelectedItem == null && !hasHoverHighlight && !chart.ShowDataLabels)
        {
            return;
        }

        for (int seriesIndex = 0; seriesIndex < series.Count; seriesIndex++)
        {
            LineSeriesSegments current = series[seriesIndex];
            Pen? pen = GetPointStrokePen(chart, state, current.PointStrokeBrush, current.PointStrokeThickness);

            foreach (List<LinePoint> segment in current.Segments)
            {
                foreach (LinePoint point in segment)
                {
                    if (!state.Layout.Plot.Contains(point.Position) &&
                        (point.Position.X < state.Layout.Plot.Left - 1 || point.Position.X > state.Layout.Plot.Right + 1))
                    {
                        continue;
                    }

                    IChartDataPoint item = state.Items[point.Index];
                    bool isSelected = chart.SelectedIndex == point.Index || ReferenceEquals(chart.SelectedItem, item);
                    bool isHovered = chart.HoveredItemIndex == point.Index;

                    bool hasSelectedRadius = isSelected &&
                                             double.IsFinite(chart.SelectedPointRadius) &&
                                             chart.SelectedPointRadius > 0;
                    bool hasSelectedBrush = isSelected && chart.SelectedPointBrush != null;
                    bool hasHoverRadius = isHovered &&
                                          double.IsFinite(chart.HoverPointRadius) &&
                                          chart.HoverPointRadius > 0;
                    bool hasHoverHalo = isHovered && chart.HoverHaloBrush != null;

                    if (current.ShowPoints || hasSelectedRadius || hasSelectedBrush || hasHoverRadius || hasHoverHalo)
                    {
                        double radius = hasSelectedRadius
                            ? chart.SelectedPointRadius
                            : hasHoverRadius
                                ? chart.HoverPointRadius
                                : current.PointRadius;

                        if (hasHoverHalo && radius > 0)
                        {
                            context.DrawEllipse(chart.HoverHaloBrush!, null, point.Position, radius * 2.1, radius * 2.1);
                        }

                        if (radius > 0)
                        {
                            IBrush brush = hasSelectedBrush
                                ? chart.SelectedPointBrush!
                                : ResolvePointBrush(chart, state, current, item);
                            DrawMarker(context, current.PointShape, brush, pen, point.Position, radius);
                        }
                    }

                    if (chart.ShowDataLabels)
                    {
                        string textStr = chart.DataLabelFormatter?.Invoke(point.Value) ??
                                         ChartValueFormatter.Format(point.Value);
                        if (!string.IsNullOrWhiteSpace(textStr))
                        {
                            IBrush labelBrush = chart.DataLabelBrush ?? chart.XAxisTextBrush;
                            FormattedText text = FormatText(chart, textStr, chart.DataLabelFontSize, labelBrush);
                            double x = Math.Clamp(
                                point.Position.X - (text.Width / 2),
                                state.Layout.Plot.Left,
                                Math.Max(state.Layout.Plot.Left, state.Layout.Plot.Right - text.Width));
                            double y = Math.Max(state.Layout.Plot.Top, point.Position.Y - current.PointRadius - text.Height - 3);
                            context.DrawText(text, new Point(x, y));
                        }
                    }
                }
            }
        }
    }

    private Pen? GetPointStrokePen(
        LineChart chart,
        ChartRenderState state,
        IBrush? strokeBrush,
        double strokeThickness)
    {
        if (strokeBrush == null || strokeThickness <= 0)
        {
            return null;
        }

        if (!chart.IsAnimationRunning &&
            _cachedPointPenState == state &&
            ReferenceEquals(_cachedPointPenBrush, strokeBrush) &&
            _cachedPointPenThickness.Equals(strokeThickness))
        {
            return _cachedPointPen;
        }

        var pen = new Pen(strokeBrush, strokeThickness);
        if (!chart.IsAnimationRunning)
        {
            _cachedPointPen = pen;
            _cachedPointPenBrush = strokeBrush;
            _cachedPointPenThickness = strokeThickness;
            _cachedPointPenState = state;
        }

        return pen;
    }

    private void DrawMarker(
        DrawingContext context,
        ChartPointShape shape,
        IBrush brush,
        Pen? pen,
        Point center,
        double radius)
    {
        if (shape == ChartPointShape.Circle)
        {
            context.DrawEllipse(brush, pen, center, radius, radius);
            return;
        }

        if (shape == ChartPointShape.Square)
        {
            context.DrawRectangle(
                brush,
                pen,
                new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));
            return;
        }

        StreamGeometry geometry = GetOrCreateMarkerGeometry(shape, radius);
        using (context.PushTransform(Matrix.CreateTranslation(center.X, center.Y)))
        {
            context.DrawGeometry(brush, pen, geometry);
        }
    }

    private StreamGeometry GetOrCreateMarkerGeometry(ChartPointShape shape, double radius)
    {
        var key = (shape, Math.Round(radius, 2));
        if (_markerGeometryCache.TryGetValue(key, out StreamGeometry? cached))
        {
            return cached;
        }

        var geometry = new StreamGeometry();
        using (StreamGeometryContext gc = geometry.Open())
        {
            double r = key.Item2;
            if (shape == ChartPointShape.Diamond)
            {
                double d = r * 1.18;
                gc.BeginFigure(new Point(0, -d), true);
                gc.LineTo(new Point(d, 0));
                gc.LineTo(new Point(0, d));
                gc.LineTo(new Point(-d, 0));
                gc.EndFigure(true);
            }
            else
            {
                double h = r * 1.25;
                double w = r * 1.15;
                gc.BeginFigure(new Point(0, -h), true);
                gc.LineTo(new Point(w, h * 0.8));
                gc.LineTo(new Point(-w, h * 0.8));
                gc.EndFigure(true);
            }
        }

        _markerGeometryCache[key] = geometry;
        return geometry;
    }

    private static IBrush ResolvePointBrush(
        LineChart chart,
        ChartRenderState state,
        LineSeriesSegments series,
        IChartDataPoint item)
    {
        if (item.Brush != null)
        {
            return item.Brush;
        }

        IBrush defaultBrush = (item as ChartDataPipeline.SeriesDataPoint)?.PointBrush ??
                              series.PointBrush ??
                              (item as ChartDataPipeline.SeriesDataPoint)?.LineBrush ??
                              series.LineBrush;
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
        double lineTension,
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

        AppendPath(geometryContext, segment, interpolationMode, lineTension);
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
        LineInterpolationMode interpolationMode,
        double lineTension)
    {
        if (points.Count < 2)
        {
            return;
        }

        if (interpolationMode == LineInterpolationMode.MonotoneSmooth)
        {
            (Point First, Point Second)[] controls = ComputeMonotoneBezierControls(points);
            for (int index = 0; index < points.Count - 1; index++)
            {
                (Point firstControl, Point secondControl) = controls[index];
                context.CubicBezierTo(firstControl, secondControl, points[index + 1].Position, true);
            }

            return;
        }

        double tensionFactor = (1.0 / 6.0) * Math.Clamp(lineTension, 0.0, 2.0);
        for (int index = 0; index < points.Count - 1; index++)
        {
            Point current = points[index].Position;
            Point next = points[index + 1].Position;

            switch (interpolationMode)
            {
                case LineInterpolationMode.Linear:
                    context.LineTo(next, true);
                    break;
                case LineInterpolationMode.StepBefore:
                    context.LineTo(new Point(current.X, next.Y), true);
                    context.LineTo(next, true);
                    break;
                case LineInterpolationMode.StepAfter:
                    context.LineTo(new Point(next.X, current.Y), true);
                    context.LineTo(next, true);
                    break;
                case LineInterpolationMode.StepMiddle:
                    double midX = (current.X + next.X) / 2.0;
                    context.LineTo(new Point(midX, current.Y), true);
                    context.LineTo(new Point(midX, next.Y), true);
                    context.LineTo(next, true);
                    break;
                default:
                    Point previous = index > 0 ? points[index - 1].Position : current;
                    Point following = index + 2 < points.Count ? points[index + 2].Position : next;
                    (Point firstControl, Point secondControl) =
                        GetBezierControls(previous, current, next, following, tensionFactor);
                    context.CubicBezierTo(firstControl, secondControl, next, true);
                    break;
            }
        }
    }

    internal static (Point First, Point Second) GetBezierControls(
        Point previous,
        Point current,
        Point next,
        Point following) =>
        GetBezierControls(previous, current, next, following, 1.0 / 6.0);

    internal static (Point First, Point Second) GetBezierControls(
        Point previous,
        Point current,
        Point next,
        Point following,
        double tension)
    {
        var first = new Point(
            current.X + ((next.X - previous.X) * tension),
            current.Y + ((next.Y - previous.Y) * tension));
        var second = new Point(
            next.X - ((following.X - current.X) * tension),
            next.Y - ((following.Y - current.Y) * tension));
        return (first, second);
    }

    internal static (Point First, Point Second)[] ComputeMonotoneBezierControls(IReadOnlyList<Point> points)
    {
        int n = points.Count;
        if (n < 2)
        {
            return [];
        }

        int segmentCount = n - 1;
        var h = new double[segmentCount];
        var d = new double[segmentCount];
        for (int k = 0; k < segmentCount; k++)
        {
            h[k] = Math.Max(1e-6, points[k + 1].X - points[k].X);
            d[k] = (points[k + 1].Y - points[k].Y) / h[k];
        }

        var m = new double[n];
        m[0] = d[0];
        m[n - 1] = d[segmentCount - 1];

        for (int k = 1; k < n - 1; k++)
        {
            if (d[k - 1] * d[k] <= 0)
            {
                m[k] = 0;
            }
            else
            {
                double w1 = (2 * h[k]) + h[k - 1];
                double w2 = h[k] + (2 * h[k - 1]);
                m[k] = (w1 + w2) / ((w1 / d[k - 1]) + (w2 / d[k]));
            }
        }

        for (int k = 0; k < segmentCount; k++)
        {
            if (Math.Abs(d[k]) <= 1e-12)
            {
                m[k] = 0;
                m[k + 1] = 0;
                continue;
            }

            double alpha = m[k] / d[k];
            double beta = m[k + 1] / d[k];
            double mag = (alpha * alpha) + (beta * beta);
            if (mag > 9.0)
            {
                double tau = 3.0 / Math.Sqrt(mag);
                m[k] = tau * alpha * d[k];
                m[k + 1] = tau * beta * d[k];
            }
        }

        var controls = new (Point First, Point Second)[segmentCount];
        for (int k = 0; k < segmentCount; k++)
        {
            double dx3 = h[k] / 3.0;
            controls[k] = (
                new Point(points[k].X + dx3, points[k].Y + (m[k] * dx3)),
                new Point(points[k + 1].X - dx3, points[k + 1].Y - (m[k + 1] * dx3)));
        }

        return controls;
    }

    private static (Point First, Point Second)[] ComputeMonotoneBezierControls(IReadOnlyList<LinePoint> points)
    {
        var raw = new Point[points.Count];
        for (int index = 0; index < points.Count; index++)
        {
            raw[index] = points[index].Position;
        }

        return ComputeMonotoneBezierControls(raw);
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
    private readonly record struct LinePoint(int Index, double Value, Point Position);

    private sealed record LineSeriesSegments(
        IBrush LineBrush,
        IBrush? AreaFillBrush,
        double LineThickness,
        IReadOnlyList<double>? DashArray,
        LineInterpolationMode InterpolationMode,
        bool ShowPoints,
        double PointRadius,
        ChartPointShape PointShape,
        IBrush? PointBrush,
        IBrush? PointStrokeBrush,
        double PointStrokeThickness,
        List<List<LinePoint>> Segments,
        bool HasItems);

    private sealed record LineSeriesGeometry(StreamGeometry[] Lines, StreamGeometry[] Areas, Pen? LinePen);
}
