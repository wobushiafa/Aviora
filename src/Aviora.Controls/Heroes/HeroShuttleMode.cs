namespace Aviora.Controls;

/// <summary>
/// Specifies how a hero transition renders and animates the moving visual during flight.
/// </summary>
public enum HeroShuttleMode
{
    /// <summary>
    /// Renders a shuttle in the host overlay layer using <see cref="Hero.ShuttleTemplate"/> when provided,
    /// or cross-faded bitmap snapshots otherwise.
    /// </summary>
    Snapshot,

    /// <summary>
    /// Animates the live <see cref="Avalonia.Rendering.Composition.CompositionVisual"/> directly via
    /// <c>Translation</c> and <c>Scale</c> without capturing any bitmap snapshots.
    /// Recommended for live video surfaces and high-resolution fullscreen transitions.
    /// </summary>
    Direct,
}
