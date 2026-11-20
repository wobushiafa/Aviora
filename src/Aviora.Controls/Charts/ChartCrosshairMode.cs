namespace Aviora.Controls;

/// <summary>
/// Specifies which crosshair guide lines are rendered over a <see cref="CartesianChart"/> on pointer hover.
/// </summary>
public enum ChartCrosshairMode
{
    /// <summary>Do not render crosshair guide lines.</summary>
    None,

    /// <summary>Render a vertical guide line across the plot.</summary>
    Vertical,

    /// <summary>Render a horizontal guide line across the plot.</summary>
    Horizontal,

    /// <summary>Render both vertical and horizontal guide lines.</summary>
    Both,
}
