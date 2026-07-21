using Aviora.Presentation.Drawers;

namespace Aviora.Controls;

/// <summary>Represents a visual host that can present drawer requests.</summary>
public interface IDrawerHost
{
    /// <summary>Presents content using the supplied request.</summary>
    void Present(DrawerRequest request, object? content);

    /// <summary>Attempts to close the current presentation.</summary>
    bool TryClose(DrawerCloseReason reason = DrawerCloseReason.Programmatic, object? result = null);
}
