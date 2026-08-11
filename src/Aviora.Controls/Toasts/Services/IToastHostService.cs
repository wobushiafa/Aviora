using Aviora.Presentation.Toasts;

namespace Aviora.Controls;

/// <summary>Extends toast operations with Avalonia host coordination.</summary>
public interface IToastHostService : IToastService
{
    /// <summary>Registers a toast host.</summary>
    void Attach(IToastHost host, string hostId);

    /// <summary>Unregisters a toast host.</summary>
    void Detach(IToastHost host, string hostId);

    /// <summary>Requests dismissal from an attached host.</summary>
    bool RequestDismiss(IToastHost host, string hostId, Guid id, ToastDismissReason reason);

    /// <summary>Completes a toast after its exit transition.</summary>
    void Complete(IToastHost host, string hostId, Guid id, ToastDismissReason reason);
}
