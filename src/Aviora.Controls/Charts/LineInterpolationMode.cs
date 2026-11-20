namespace Aviora.Controls;

/// <summary>
/// Defines how adjacent points are connected by a <see cref="LineChart"/>.
/// </summary>
public enum LineInterpolationMode
{
    /// <summary>Connects points with straight line segments.</summary>
    Linear,

    /// <summary>Connects points with a smooth Catmull-Rom-derived cubic curve.</summary>
    Smooth,

    /// <summary>Connects points with a shape-preserving Fritsch-Carlson monotone cubic curve that avoids overshoot.</summary>
    MonotoneSmooth,

    /// <summary>Draws a vertical step before moving horizontally to the next point.</summary>
    StepBefore,

    /// <summary>Draws a horizontal segment before stepping vertically to the next point.</summary>
    StepAfter,

    /// <summary>Steps vertically at the horizontal midpoint between adjacent points.</summary>
    StepMiddle,
}
