namespace Aviora.Controls;

/// <summary>
/// Specifies the transition style used by <see cref="HeroTransitionControl"/> when switching content.
/// </summary>
public enum HeroContentTransitionMode
{
    /// <summary>Cross-fades between the outgoing snapshot and incoming content.</summary>
    Fade,

    /// <summary>Cross-fades with a subtle scale transition.</summary>
    FadeScale,

    /// <summary>Cross-fades with a subtle vertical slide.</summary>
    SlideVertical,

    /// <summary>Cross-fades with a subtle horizontal slide.</summary>
    SlideHorizontal,
}
