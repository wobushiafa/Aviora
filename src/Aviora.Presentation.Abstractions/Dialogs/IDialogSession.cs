namespace Aviora.Presentation.Dialogs;

/// <summary>Controls one dialog presentation without exposing its host.</summary>
public interface IDialogSession
{
    /// <summary>Gets whether this presentation has completed.</summary>
    bool IsClosed { get; }

    /// <summary>Closes this presentation with an optional result.</summary>
    bool Close(object? result = null);

    /// <summary>Cancels this presentation.</summary>
    bool Cancel();
}
