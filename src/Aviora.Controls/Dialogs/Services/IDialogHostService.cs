using Aviora.Presentation.Dialogs;

namespace Aviora.Controls;

/// <summary>Coordinates dialog requests with visual hosts.</summary>
public interface IDialogHostService : IDialogService
{
    /// <summary>Registers a visual host for an identifier.</summary>
    void Attach(IDialogHost host, string hostId);

    /// <summary>Unregisters a visual host.</summary>
    void Detach(IDialogHost host, string hostId);

    /// <summary>Completes the active request after its host closes.</summary>
    void Complete(IDialogHost host, string hostId, object? result, DialogCloseReason reason);
}
