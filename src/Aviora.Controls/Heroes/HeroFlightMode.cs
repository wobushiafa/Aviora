namespace Aviora.Controls;

/// <summary>
/// Specifies how a hero shuttle interpolates its position and size between source and destination bounds.
/// </summary>
public enum HeroFlightMode
{
    /// <summary>
    /// Interpolates the bounding rectangle linearly along a straight path.
    /// </summary>
    Linear,

    /// <summary>
    /// Moves the center along a curved arc while interpolating size, similar to Material hero motion.
    /// </summary>
    Arc,
}
