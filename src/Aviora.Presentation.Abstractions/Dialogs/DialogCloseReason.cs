namespace Aviora.Presentation.Dialogs;

/// <summary>Describes why a dialog presentation was closed.</summary>
public enum DialogCloseReason
{
    /// <summary>The dialog was closed through an API call.</summary>
    Programmatic,

    /// <summary>The dialog was closed by interacting with the overlay.</summary>
    LightDismiss,

    /// <summary>The dialog was closed with the Escape key.</summary>
    EscapeKey,

    /// <summary>The dialog's close command was executed.</summary>
    Command,

    /// <summary>The dialog host left the visual tree.</summary>
    HostDetached,

    /// <summary>The presentation was canceled.</summary>
    Canceled,
}
