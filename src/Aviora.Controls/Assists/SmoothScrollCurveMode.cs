namespace Aviora.Controls;

/// <summary>
/// Interpolation curve mode used by <see cref="SmoothScrollAssist"/>.
/// </summary>
public enum SmoothScrollCurveMode
{
    /// <summary>
    /// Frame-rate-independent exponential damping controlled by
    /// <see cref="SmoothScrollAssist.InitialStepFactorProperty"/>,
    /// <see cref="SmoothScrollAssist.DecayRateProperty"/>, and
    /// <see cref="SmoothScrollAssist.MinSpeedProperty"/>.
    /// </summary>
    Exponential,

    /// <summary>
    /// Fixed-duration interpolation controlled by
    /// <see cref="SmoothScrollAssist.DurationProperty"/> and
    /// <see cref="SmoothScrollAssist.EasingProperty"/>.
    /// </summary>
    Easing,
}
