namespace Aviora.Controls;

/// <summary>
/// Defines how a thermometer maps its liquid brush to the filled region.
/// </summary>
public enum LiquidBrushMappingMode
{
    /// <summary>
    /// Maps the brush to the currently filled area, so the complete brush is visible at every value.
    /// </summary>
    FilledArea,

    /// <summary>
    /// Maps the brush to the complete minimum-to-maximum range and reveals it as the value increases.
    /// </summary>
    FullRange,
}
