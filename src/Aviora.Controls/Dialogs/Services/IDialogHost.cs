using Aviora.Presentation.Dialogs;

namespace Aviora.Controls;

/// <summary>Represents a visual host that can present dialog requests.</summary>
public interface IDialogHost
{
    /// <summary>Presents content using the supplied request.</summary>
    void Present(DialogRequest request, object? content);

    /// <summary>Attempts to close the current presentation.</summary>
    bool TryClose(DialogCloseReason reason = DialogCloseReason.Programmatic, object? result = null);
}
