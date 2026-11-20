namespace Aviora.Controls;

/// <summary>
/// Scroll axes handled by <see cref="SmoothScrollAssist"/>.
/// </summary>
public enum SmoothScrollOrientation
{
    /// <summary>
    /// Smoothly interpolate both vertical and horizontal wheel scrolling.
    /// </summary>
    Both,

    /// <summary>
    /// Smoothly interpolate only vertical scrolling.
    /// </summary>
    Vertical,

    /// <summary>
    /// Smoothly interpolate only horizontal scrolling.
    /// </summary>
    Horizontal,
}
