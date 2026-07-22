namespace Aviora.Presentation.Dialogs;

/// <summary>Defines how a dialog request is scheduled when its host is already presenting content.</summary>
public enum DialogPresentationMode
{
    /// <summary>Waits until the active presentation and earlier queued requests have completed.</summary>
    Queue,

    /// <summary>Replaces the visible presentation and restores the previous one when closed.</summary>
    Navigate,

    /// <summary>Presents immediately as a visual layer above the active presentation.</summary>
    Stack,
}
