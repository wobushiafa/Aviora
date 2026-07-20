namespace Aviora.Presentation.Drawers;

/// <summary>
/// Describes why a drawer presentation was closed.
/// </summary>
public enum DrawerCloseReason
{
    /// <summary>The drawer was closed through an API call.</summary>
    Programmatic,

    /// <summary>The drawer was closed by interacting with the overlay.</summary>
    LightDismiss,

    /// <summary>The drawer was closed with the Escape key.</summary>
    EscapeKey,

    /// <summary>The drawer's close command was executed.</summary>
    Command,

    /// <summary>The presentation was replaced by another request.</summary>
    Replaced,

    /// <summary>The drawer host left the visual tree.</summary>
    HostDetached,

    /// <summary>The presentation's cancellation token was canceled.</summary>
    Canceled,
}
