using Aviora.Presentation.Toasts;

namespace Aviora.Controls;

/// <summary>Receives toast presentations from a host service.</summary>
public interface IToastHost
{
    /// <summary>Presents or queues one toast.</summary>
    void Present(ToastPresentation presentation);

    /// <summary>Begins dismissal of an active or queued toast.</summary>
    bool Dismiss(Guid id, ToastDismissReason reason);
}

/// <summary>Contains the resolved content and session for one toast request.</summary>
public sealed record ToastPresentation(Guid Id, ToastRequest Request, object? Content, IToastSession Session);
