using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

internal sealed class ColumnChartRenderer : CartesianChartRenderer<ColumnChart>
{
    protected override double GetItemWidthRatio(ColumnChart chart) => chart.BarWidthRatio;

    protected override void DrawChartBackground(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state)
    {
        for (int index = 0; index < state.Items.Count; index++)
        {
            IBrush? brush = state.Items[index].ColumnBackgroundBrush ?? chart.ColumnBackgroundBrush;
            if (brush == null)
            {
                continue;
            }

            double x = GetBarLeft(state.Layout, index);
            context.DrawRectangle(
                brush,
                null,
                new Rect(x, state.Layout.Plot.Top, state.Layout.BarWidth, state.Layout.Plot.Height));
        }
    }

    protected override void DrawSeries(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        double baseline = MapY(Math.Clamp(0, state.Scale.Minimum, state.Scale.Maximum), state);
        for (int index = 0; index < state.Items.Count; index++)
        {
            IChartDataPoint item = state.Items[index];
            if (!double.IsFinite(item.Value) || index >= animatedValues.Count)
            {
                continue;
            }

            double valueY = MapY(
                Math.Clamp(animatedValues[index], state.Scale.Minimum, state.Scale.Maximum),
                state);
            var rect = new Rect(
                GetBarLeft(state.Layout, index),
                Math.Min(valueY, baseline),
                state.Layout.BarWidth,
                Math.Abs(valueY - baseline));
            bool isSelected = chart.SelectedIndex == index || ReferenceEquals(chart.SelectedItem, item);
            IBrush barBrush = isSelected && chart.SelectedBarBrush != null
                ? chart.SelectedBarBrush
                : ResolveBarBrush(chart, state, item);
            context.DrawRectangle(barBrush, null, rect);
            if (isSelected && chart.SelectionOverlayBrush != null)
            {
                context.DrawRectangle(chart.SelectionOverlayBrush, null, rect);
            }

            if (isSelected && chart.SelectionStrokeThickness > 0)
            {
                double strokeThickness = Math.Max(0, chart.SelectionStrokeThickness);
                double inset = Math.Min(strokeThickness / 2, Math.Min(rect.Width, rect.Height) / 2);
                context.DrawRectangle(
                    null,
                    new Pen(chart.SelectionStrokeBrush, strokeThickness),
                    rect.Deflate(inset));
            }
        }
    }

    private static IBrush ResolveBarBrush(
        ColumnChart chart,
        ChartRenderState state,
        IChartDataPoint item)
    {
        if (item.Brush != null || !chart.ShowThresholds)
        {
            return item.Brush ?? chart.DefaultBrush;
        }

        return ChartThresholdResolver.Resolve(
            item.Value,
            chart.DefaultBrush,
            state.Thresholds,
            chart.ThresholdDirection);
    }

    private static double GetBarLeft(ChartLayout layout, int index) =>
        layout.Plot.Left +
        (index * layout.SlotWidth) +
        ((layout.SlotWidth - layout.BarWidth) / 2);
}
