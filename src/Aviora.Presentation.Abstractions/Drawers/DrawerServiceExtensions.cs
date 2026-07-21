namespace Aviora.Presentation.Drawers;

/// <summary>Provides convenience overloads for common drawer presentations.</summary>
public static class DrawerServiceExtensions
{
    /// <summary>Presents content at the specified placement, defaulting to the right.</summary>
    public static Task<DrawerResult> ShowAsync(
        this IDrawerService service,
        object? content,
        DrawerPlacement? placement = null,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return service.ShowAsync(new DrawerRequest(content)
        {
            Placement = placement ?? DrawerPlacement.Right,
        }, cancellationToken);
    }

    /// <summary>Presents session-aware content at the specified placement, defaulting to the right.</summary>
    public static Task<DrawerResult> ShowAsync(
        this IDrawerService service,
        Func<IDrawerSession, object?> contentFactory,
        DrawerPlacement? placement = null,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return service.ShowAsync(new DrawerRequest(null)
        {
            ContentFactory = contentFactory ?? throw new ArgumentNullException(nameof(contentFactory)),
            Placement = placement ?? DrawerPlacement.Right,
        }, cancellationToken);
    }
}
