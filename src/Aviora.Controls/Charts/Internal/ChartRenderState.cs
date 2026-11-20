using Avalonia;
using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal sealed record LegendHitItem(
    int SeriesIndex,
    string Title,
    IBrush Brush,
    bool IsVisible,
    AvaloniaObject? SeriesObject,
    Rect Bounds,
    FormattedText Text,
    Point TextPosition,
    Rect SwatchRect);

internal sealed class ChartRenderState
{
    public required IReadOnlyList<IChartDataPoint> Items { get; init; }

    public required int CategoryCount { get; init; }

    public required IReadOnlyList<ChartThreshold> Thresholds { get; init; }

    public required ChartLayout Layout { get; init; }

    public required ChartAxisScale Scale { get; init; }

    public required IReadOnlyList<string> YAxisLabels { get; init; }

    public required IReadOnlyList<string> XAxisLabels { get; init; }

    public IReadOnlyList<ChartDataPipeline.ChartSeriesMeta> SeriesMeta { get; init; } = [];

    public IReadOnlyList<LegendHitItem> LegendItems { get; init; } = [];

    public int GridLineCount => Math.Max(2, YAxisLabels.Count);
}
