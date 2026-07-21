namespace Aviora.Presentation.Dialogs;

/// <summary>Provides convenience overloads for common dialog presentations.</summary>
public static class DialogServiceExtensions
{
    /// <summary>Presents content using the host's default options.</summary>
    public static Task<DialogResult> ShowAsync(
        this IDialogService service,
        object? content,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return service.ShowAsync(new DialogRequest(content), cancellationToken);
    }

    /// <summary>Presents session-aware content using the host's default options.</summary>
    public static Task<DialogResult> ShowAsync(
        this IDialogService service,
        Func<IDialogSession, object?> contentFactory,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return service.ShowAsync(DialogRequest.Create(contentFactory), cancellationToken);
    }
}
