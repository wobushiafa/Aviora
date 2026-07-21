using Aviora.Presentation.Loadings;

namespace Aviora.Controls;

/// <summary>Extends loading operations with Avalonia host coordination.</summary>
public interface ILoadingHostService : ILoadingService
{
    /// <summary>Registers a loading overlay host.</summary>
    void Attach(ILoadingHost host, string hostId);

    /// <summary>Unregisters a loading overlay host.</summary>
    void Detach(ILoadingHost host, string hostId);
}
