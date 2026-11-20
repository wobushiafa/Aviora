namespace Aviora.Controls;

/// <summary>
/// Built-in motion feel presets for <see cref="SmoothScrollAssist"/>.
/// </summary>
public enum SmoothScrollPreset
{
    /// <summary>
    /// Balanced physical damping suitable for general desktop scrolling.
    /// </summary>
    Default,

    /// <summary>
    /// Crisp, low-latency response with higher damping and shorter settling tail.
    /// </summary>
    Snappy,

    /// <summary>
    /// Longer, softer inertial glide with gentler initial step and decay.
    /// </summary>
    Silky,
}
