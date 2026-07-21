namespace Aviora.Presentation.Dialogs;

/// <summary>Presents dialog content without coupling a ViewModel to a UI framework.</summary>
public interface IDialogService
{
    /// <summary>Queues and presents a dialog request.</summary>
    Task<DialogResult> ShowAsync(DialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>Closes the active presentation on the identified host.</summary>
    bool Close(string hostId = DialogHost.DefaultId, object? result = null);
}
