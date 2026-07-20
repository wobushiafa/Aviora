namespace Aviora.Presentation.Drawers;

/// <summary>
/// Presents drawer content without coupling a view model to a UI framework.
/// </summary>
public interface IDrawerService
{
    /// <summary>Queues and presents a drawer request.</summary>
    Task<DrawerResult> ShowAsync(DrawerRequest request, CancellationToken cancellationToken = default);

    /// <summary>Closes the active presentation on the identified host.</summary>
    bool Close(string hostId = DrawerHost.DefaultId, object? result = null);
}
