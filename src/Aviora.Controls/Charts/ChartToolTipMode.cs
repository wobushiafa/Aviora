namespace Aviora.Controls;

/// <summary>
/// Specifies how tooltip content is aggregated when hovering over a <see cref="CartesianChart"/>.
/// </summary>
public enum ChartToolTipMode
{
    /// <summary>Displays the tooltip for the single nearest or hovered data point.</summary>
    SinglePoint,

    /// <summary>Displays aggregated values for all visible series sharing the hovered category index.</summary>
    SharedCategory,
}
