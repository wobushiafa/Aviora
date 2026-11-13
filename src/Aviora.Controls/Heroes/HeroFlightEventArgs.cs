using Avalonia;

namespace Aviora.Controls;

/// <summary>
/// Provides data for hero flight lifecycle events.
/// </summary>
public sealed class HeroFlightEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeroFlightEventArgs"/> class.
    /// </summary>
    /// <param name="tag">The shared hero tag.</param>
    /// <param name="sourceBounds">The starting bounds in <see cref="HeroHost"/> coordinates.</param>
    /// <param name="destinationBounds">The target bounds in <see cref="HeroHost"/> coordinates.</param>
    /// <param name="duration">The configured flight duration.</param>
    /// <param name="flightMode">The flight interpolation mode.</param>
    public HeroFlightEventArgs(
        object tag,
        Rect sourceBounds,
        Rect destinationBounds,
        TimeSpan duration,
        HeroFlightMode flightMode)
    {
        Tag = tag;
        SourceBounds = sourceBounds;
        DestinationBounds = destinationBounds;
        Duration = duration;
        FlightMode = flightMode;
    }

    /// <summary>Gets the shared hero tag.</summary>
    public object Tag { get; }

    /// <summary>Gets the starting bounds in <see cref="HeroHost"/> coordinates.</summary>
    public Rect SourceBounds { get; }

    /// <summary>Gets the target bounds in <see cref="HeroHost"/> coordinates.</summary>
    public Rect DestinationBounds { get; }

    /// <summary>Gets the configured flight duration.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the flight interpolation mode.</summary>
    public HeroFlightMode FlightMode { get; }
}
