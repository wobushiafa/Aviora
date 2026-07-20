using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

internal sealed class LineChartRenderer : CartesianChartRenderer<LineChart>
{
    protected override void DrawSeries(
        DrawingContext context,
        LineChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        List<List<LinePoint>> segments = BuildSegments(state, animatedValues);
        double baseline = MapY(Math.Clamp(0, state.Scale.Minimum, state.Scale.Maximum), state);
        double lineThickness = NormalizeNonNegative(chart.LineThickness);
        using (context.PushClip(state.Layout.Plot))
        {
            foreach (List<LinePoint> segment in segments)
            {
                if (chart.AreaFillBrush != null)
                {
                    DrawArea(context, chart, segment, baseline);
                }

                if (segment.Count > 1 && lineThickness > 0)
                {
                    StreamGeometry geometry = BuildGeometry(segment, chart.InterpolationMode, isArea: false, baseline);
                    context.DrawGeometry(
                        null,
                        new Pen(chart.LineBrush, lineThickness),
                        geometry);
                }
            }
        }

        DrawPoints(context, chart, state, segments);
    }

    private static List<List<LinePoint>> BuildSegments(
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        var segments = new List<List<LinePoint>>();
        List<LinePoint>? current = null;
        for (int index = 0; index < state.Items.Count; index++)
        {
            if (index >= animatedValues.Count || !double.IsFinite(state.Items[index].Value))
            {
                current = null;
                continue;
            }

            current ??= [];
            if (current.Count == 0)
            {
                segments.Add(current);
            }

            double value = Math.Clamp(animatedValues[index], state.Scale.Minimum, state.Scale.Maximum);
            current.Add(new LinePoint(
                index,
                new Point(GetItemCenterX(state.Layout, index), MapY(value, state))));
        }

        return segments;
    }

    private static void DrawArea(
        DrawingContext context,
        LineChart chart,
        IReadOnlyList<LinePoint> segment,
        double baseline)
    {
        if (segment.Count == 0)
        {
            return;
        }

        StreamGeometry geometry = BuildGeometry(segment, chart.InterpolationMode, isArea: true, baseline);
        context.DrawGeometry(chart.AreaFillBrush, null, geometry);
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

    private static void DrawPoints(
        DrawingContext context,
        LineChart chart,
        ChartRenderState state,
        IEnumerable<List<LinePoint>> segments)
    {
        double defaultRadius = NormalizeNonNegative(chart.PointRadius);
        foreach (LinePoint point in segments.SelectMany(segment => segment))
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
            double strokeThickness = NormalizeNonNegative(chart.PointStrokeThickness);
            Pen? pen = chart.PointStrokeBrush != null && strokeThickness > 0
                ? new Pen(chart.PointStrokeBrush, strokeThickness)
                : null;
            context.DrawEllipse(brush, pen, point.Position, radius, radius);
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

        IBrush defaultBrush = chart.PointBrush ?? chart.LineBrush;
        return chart.ShowThresholds
            ? ChartThresholdResolver.Resolve(
                item.Value,
                defaultBrush,
                state.Thresholds,
                chart.ThresholdDirection)
            : defaultBrush;
    }

    private static double NormalizeNonNegative(double value) =>
        double.IsFinite(value) && value > 0 ? value : 0;

    private readonly record struct LinePoint(int Index, Point Position);
}
