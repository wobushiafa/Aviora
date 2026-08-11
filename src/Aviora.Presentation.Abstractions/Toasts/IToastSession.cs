namespace Aviora.Presentation.Toasts;

/// <summary>Controls and observes one toast presentation.</summary>
public interface IToastSession
{
    /// <summary>Gets the stable identifier of this presentation.</summary>
    Guid Id { get; }

    /// <summary>Gets whether this presentation has completed.</summary>
    bool IsDismissed { get; }

    /// <summary>Gets a task that completes with the dismissal reason.</summary>
    Task<ToastDismissReason> Completion { get; }

    /// <summary>Dismisses only this presentation.</summary>
    bool Dismiss();
}
