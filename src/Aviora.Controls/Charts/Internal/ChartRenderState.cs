namespace Aviora.Controls;

using Aviora.Core.Charts;

internal sealed class ChartRenderState
{
    public required IReadOnlyList<IChartDataPoint> Items { get; init; }

    public required IReadOnlyList<ChartThreshold> Thresholds { get; init; }

    public required ChartLayout Layout { get; init; }

    public required ChartAxisScale Scale { get; init; }

    public required IReadOnlyList<string> YAxisLabels { get; init; }

    public required IReadOnlyList<string> XAxisLabels { get; init; }

    public int GridLineCount => Math.Max(2, YAxisLabels.Count);
}
