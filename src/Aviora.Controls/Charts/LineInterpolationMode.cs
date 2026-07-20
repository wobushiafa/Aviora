namespace Aviora.Controls;

/// <summary>
/// Defines how adjacent points are connected by a <see cref="LineChart"/>.
/// </summary>
public enum LineInterpolationMode
{
    /// <summary>Connects points with straight line segments.</summary>
    Linear,

    /// <summary>Connects points with a smooth Catmull-Rom-derived curve.</summary>
    Smooth,
}
