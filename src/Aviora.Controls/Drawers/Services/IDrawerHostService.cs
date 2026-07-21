using Aviora.Presentation.Drawers;

namespace Aviora.Controls;

/// <summary>Coordinates drawer requests with visual hosts.</summary>
public interface IDrawerHostService : IDrawerService
{
    /// <summary>Registers a visual host for an identifier.</summary>
    void Attach(IDrawerHost host, string hostId);

    /// <summary>Unregisters a visual host.</summary>
    void Detach(IDrawerHost host, string hostId);

    /// <summary>Completes the active request after its host closes.</summary>
    void Complete(IDrawerHost host, string hostId, object? result, DrawerCloseReason reason);
}
