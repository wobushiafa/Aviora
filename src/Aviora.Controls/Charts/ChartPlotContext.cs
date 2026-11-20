using Avalonia;
using Aviora.Core.Charts;

namespace Aviora.Controls;

/// <summary>
/// Exposes plot geometry, active axis scale, and coordinate helpers to custom chart render callbacks.
/// </summary>
public readonly struct ChartPlotContext
{
    internal ChartPlotContext(
        Rect plot,
        ChartAxisScale scale,
        int categoryCount,
        int viewportStartIndex,
        int viewportCount,
        double slotWidth)
    {
        Plot = plot;
        Scale = scale;
        CategoryCount = categoryCount;
        ViewportStartIndex = viewportStartIndex;
        ViewportCount = viewportCount;
        SlotWidth = slotWidth;
    }

    /// <summary>Gets the plot bounding rectangle in control DIPs.</summary>
    public Rect Plot { get; }

    /// <summary>Gets the resolved Y-axis scale.</summary>
    public ChartAxisScale Scale { get; }

    /// <summary>Gets the total number of categories in the chart.</summary>
    public int CategoryCount { get; }

    /// <summary>Gets the first visible category index in the current zoom viewport.</summary>
    public int ViewportStartIndex { get; }

    /// <summary>Gets the number of visible categories in the current zoom viewport.</summary>
    public int ViewportCount { get; }

    /// <summary>Gets the horizontal width of one visible category slot in DIPs.</summary>
    public double SlotWidth { get; }

    /// <summary>Maps a category index to its horizontal center X coordinate in control DIPs.</summary>
    public double MapCategoryCenterX(int categoryIndex) =>
        Plot.Left + ((categoryIndex - ViewportStartIndex) * SlotWidth) + (SlotWidth / 2);

    /// <summary>Maps a data value to its vertical Y coordinate in control DIPs.</summary>
    public double MapY(double value) =>
        ChartLayoutCalculator.MapY(value, Plot, Scale);
}
