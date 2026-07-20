using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal abstract class CartesianChartRenderer<TChart>
    where TChart : CartesianChart
{
    private readonly ChartTextLayout _textLayout = new();
    private ChartRenderState? _state;

    public void Invalidate()
    {
        _state = null;
        _textLayout.Clear();
    }

    public void Render(
        DrawingContext context,
        TChart chart,
        IReadOnlyList<IChartDataPoint> items,
        IReadOnlyList<double> animatedValues)
    {
        ChartRenderState state = EnsureState(chart, items);
        if (state.Layout.Plot.Width <= 0 || state.Layout.Plot.Height <= 0)
        {
            return;
        }

        DrawChartBackground(context, chart, state);
        DrawGridLines(context, chart, state);
        DrawYAxisLabels(context, chart, state);

        if (state.Items.Count == 0)
        {
            DrawEmptyState(context, chart, state);
            return;
        }

        DrawSeries(context, chart, state, animatedValues);
        DrawThresholdLines(context, chart, state);
        DrawXAxisLabels(context, chart, state);
    }

    public virtual int HitTest(TChart chart, IReadOnlyList<IChartDataPoint> items, Point point)
    {
        ChartRenderState state = EnsureState(chart, items);
        return ChartLayoutCalculator.HitTest(state.Layout, point, items.Count);
    }

    protected abstract void DrawSeries(
        DrawingContext context,
        TChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues);

    protected virtual void DrawChartBackground(
        DrawingContext context,
        TChart chart,
        ChartRenderState state)
    {
    }

    protected virtual double GetItemWidthRatio(TChart chart) => 1;

    protected static double GetItemCenterX(ChartLayout layout, int index) =>
        layout.Plot.Left + (index * layout.SlotWidth) + (layout.SlotWidth / 2);

    protected static double MapY(double value, ChartRenderState state) =>
        ChartLayoutCalculator.MapY(value, state.Layout.Plot, state.Scale);

    private ChartRenderState EnsureState(TChart chart, IReadOnlyList<IChartDataPoint> items)
    {
        if (_state != null)
        {
            return _state;
        }

        IReadOnlyList<ChartThreshold> thresholds = chart.Thresholds?.ToList() ?? [];
        ChartLayout layout = CalculateLayout(chart, items.Count, thresholds);
        ChartAxisScale scale = ChartAxisCalculator.Calculate(
            items.Select(item => item.Value),
            chart.AutoRange,
            chart.MinValue,
            chart.MaxValue,
            chart.GridLineCount,
            chart.AutoRangePaddingRatio);

        List<string> yLabels = ChartDataPipeline.ResolveLabels(chart.YAxisLabelsSource, chart.YAxisLabels);
        if (yLabels.Count == 0)
        {
            Func<double, string> formatter = chart.YAxisLabelFormatter ?? FormatAxisValue;
            yLabels = scale.Ticks.Select(formatter).ToList();
        }

        List<string> xLabels = ChartDataPipeline.ResolveLabels(chart.XAxisLabelsSource, chart.XAxisLabels);
        if (xLabels.Count == 0)
        {
            xLabels = items.Select(item => item.Label ?? string.Empty).ToList();
        }

        _state = new ChartRenderState
        {
            Items = items,
            Thresholds = thresholds,
            Layout = layout,
            Scale = scale,
            YAxisLabels = yLabels,
            XAxisLabels = xLabels,
        };
        return _state;
    }

    private ChartLayout CalculateLayout(
        TChart chart,
        int itemCount,
        IReadOnlyList<ChartThreshold> thresholds)
    {
        double requiredYAxisWidth = 0;
        if (chart.ShowYAxis && chart.ShowThresholds && chart.ShowThresholdLabels)
        {
            requiredYAxisWidth = thresholds
                .Where(item => !string.IsNullOrWhiteSpace(item.Label))
                .Select(item => _textLayout.Format(
                    item.Label!,
                    chart.ThresholdLabelFontSize,
                    item.LabelBrush ?? item.Brush).Width + 8)
                .DefaultIfEmpty(0)
                .Max();
        }

        double verticalInset = 4;
        if (chart.ShowYAxis && Math.Max(chart.YAxisWidth, requiredYAxisWidth) > 0)
        {
            double labelHeight = _textLayout.Format("0", chart.YAxisFontSize, chart.YAxisTextBrush).Height;
            if (chart.ShowThresholds && chart.ShowThresholdLabels)
            {
                double thresholdLabelHeight = thresholds
                    .Where(item => !string.IsNullOrWhiteSpace(item.Label))
                    .Select(item => _textLayout.Format(
                        item.Label!,
                        chart.ThresholdLabelFontSize,
                        item.LabelBrush ?? item.Brush).Height)
                    .DefaultIfEmpty(0)
                    .Max();
                labelHeight = Math.Max(labelHeight, thresholdLabelHeight);
            }

            verticalInset = Math.Max(verticalInset, labelHeight / 2);
        }

        return ChartLayoutCalculator.Calculate(new ChartLayoutOptions(
            chart.Bounds.Size,
            itemCount,
            chart.ShowXAxis,
            chart.XAxisHeight,
            chart.ShowYAxis,
            chart.YAxisWidth,
            GetItemWidthRatio(chart),
            verticalInset,
            requiredYAxisWidth));
    }

    private static void DrawGridLines(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowGridLines || state.GridLineCount < 2)
        {
            return;
        }

        var pen = new Pen(chart.GridLineBrush, 1, new DashStyle([4, 4], 0));
        for (int index = 0; index < state.GridLineCount; index++)
        {
            double y = state.Layout.Plot.Top +
                       (index * state.Layout.Plot.Height / (state.GridLineCount - 1));
            context.DrawLine(
                pen,
                new Point(state.Layout.Plot.Left, y),
                new Point(state.Layout.Plot.Right, y));
        }
    }

    private void DrawThresholdLines(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowThresholds)
        {
            return;
        }

        foreach (ChartThreshold threshold in state.Thresholds)
        {
            if (!double.IsFinite(threshold.Value) ||
                threshold.Value < state.Scale.Minimum ||
                threshold.Value > state.Scale.Maximum)
            {
                continue;
            }

            double y = MapY(threshold.Value, state);
            context.DrawLine(
                new Pen(threshold.Brush, 1.2),
                new Point(state.Layout.Plot.Left, y),
                new Point(state.Layout.Plot.Right, y));
            DrawThresholdLabel(context, chart, state, threshold, y);
        }
    }

    private void DrawThresholdLabel(
        DrawingContext context,
        TChart chart,
        ChartRenderState state,
        ChartThreshold threshold,
        double targetY)
    {
        if (!chart.ShowYAxis || !chart.ShowThresholdLabels || state.Layout.Plot.Left <= 0 ||
            string.IsNullOrWhiteSpace(threshold.Label))
        {
            return;
        }

        IBrush brush = threshold.LabelBrush ?? threshold.Brush;
        FormattedText text = _textLayout.Format(threshold.Label, chart.ThresholdLabelFontSize, brush);
        double y = Math.Clamp(
            targetY - (text.Height / 2),
            0,
            Math.Max(0, chart.Bounds.Height - text.Height));
        double x = Math.Max(0, state.Layout.Plot.Left - text.Width - 4);
        context.DrawText(text, new Point(x, y));
    }

    private void DrawYAxisLabels(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowYAxis || state.Layout.Plot.Left <= 0)
        {
            return;
        }

        for (int index = 0; index < state.YAxisLabels.Count; index++)
        {
            string label = state.YAxisLabels[index];
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            FormattedText text = _textLayout.Format(label, chart.YAxisFontSize, chart.YAxisTextBrush);
            double targetY = state.YAxisLabels.Count == 1
                ? state.Layout.Plot.Center.Y
                : state.Layout.Plot.Top +
                  (index * state.Layout.Plot.Height / (state.YAxisLabels.Count - 1));
            if (OverlapsThresholdLabel(chart, state, targetY, text.Height))
            {
                continue;
            }

            double y = Math.Clamp(
                targetY - (text.Height / 2),
                0,
                Math.Max(0, chart.Bounds.Height - text.Height));
            context.DrawText(
                text,
                new Point(Math.Max(0, state.Layout.Plot.Left - text.Width - 4), y));
        }
    }

    private bool OverlapsThresholdLabel(
        TChart chart,
        ChartRenderState state,
        double targetY,
        double textHeight)
    {
        if (!chart.ShowThresholds || !chart.ShowThresholdLabels)
        {
            return false;
        }

        foreach (ChartThreshold threshold in state.Thresholds)
        {
            if (string.IsNullOrWhiteSpace(threshold.Label) ||
                !double.IsFinite(threshold.Value) ||
                threshold.Value < state.Scale.Minimum ||
                threshold.Value > state.Scale.Maximum)
            {
                continue;
            }

            FormattedText thresholdText = _textLayout.Format(
                threshold.Label,
                chart.ThresholdLabelFontSize,
                threshold.LabelBrush ?? threshold.Brush);
            double thresholdY = MapY(threshold.Value, state);
            if (Math.Abs(thresholdY - targetY) < ((thresholdText.Height + textHeight) / 2) + 2)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawXAxisLabels(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowXAxis || chart.XAxisHeight <= 0 || state.Items.Count == 0 ||
            state.XAxisLabels.Count == 0)
        {
            return;
        }

        var placements = new List<XAxisLabelPlacement>(state.XAxisLabels.Count);
        for (int index = 0; index < state.XAxisLabels.Count; index++)
        {
            string label = state.XAxisLabels[index];
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            FormattedText text = _textLayout.Format(label, chart.XAxisFontSize, chart.XAxisTextBrush);
            double itemIndex = state.XAxisLabels.Count == state.Items.Count
                ? index
                : state.XAxisLabels.Count == 1
                    ? (state.Items.Count - 1) / 2.0
                    : index * (state.Items.Count - 1.0) / (state.XAxisLabels.Count - 1);
            double center = state.Layout.Plot.Left +
                            (itemIndex * state.Layout.SlotWidth) +
                            (state.Layout.SlotWidth / 2);
            double left = Math.Clamp(
                center - (text.Width / 2),
                state.Layout.Plot.Left,
                Math.Max(state.Layout.Plot.Left, state.Layout.Plot.Right - text.Width));
            placements.Add(new XAxisLabelPlacement(text, left));
        }

        IReadOnlyList<int> visible = ChartAxisCalculator.SelectLabelIndices(
            placements.Select(item => item.Left).ToList(),
            placements.Select(item => item.Right).ToList(),
            chart.XAxisLabelMode,
            chart.XAxisLabelInterval);
        foreach (int index in visible)
        {
            XAxisLabelPlacement placement = placements[index];
            context.DrawText(placement.Text, new Point(placement.Left, state.Layout.XAxisTop));
        }
    }

    private void DrawEmptyState(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowEmptyText || string.IsNullOrWhiteSpace(chart.EmptyText))
        {
            return;
        }

        FormattedText text = _textLayout.Format(chart.EmptyText, chart.EmptyTextFontSize, chart.EmptyTextBrush);
        context.DrawText(
            text,
            new Point(
                state.Layout.Plot.Center.X - (text.Width / 2),
                state.Layout.Plot.Center.Y - (text.Height / 2)));
    }

    internal static string FormatAxisValue(double value)
    {
        if (!double.IsFinite(value))
        {
            return "-";
        }

        double rounded = Math.Round(value, 2);
        return Math.Abs(rounded) >= 1_000_000
            ? rounded.ToString("0.##E+0", CultureInfo.InvariantCulture)
            : rounded.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private readonly record struct XAxisLabelPlacement(FormattedText Text, double Left)
    {
        public double Right => Left + Text.Width;
    }
}
