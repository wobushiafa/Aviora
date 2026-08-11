namespace Aviora.Presentation.Toasts;

/// <summary>Shows transient notifications through an attached toast host.</summary>
public interface IToastService
{
    /// <summary>Shows a toast and returns a session that can dismiss or observe it.</summary>
    IToastSession Show(ToastRequest request, CancellationToken cancellationToken = default);

    /// <summary>Dismisses all active and queued notifications for a host.</summary>
    int Clear(string hostId = ToastHosts.DefaultId);
}
