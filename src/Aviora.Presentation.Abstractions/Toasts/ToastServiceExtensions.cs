namespace Aviora.Presentation.Toasts;

/// <summary>Provides concise helpers for common toast severities.</summary>
public static class ToastServiceExtensions
{
    /// <summary>Shows an informational toast.</summary>
    public static IToastSession ShowInformation(
        this IToastService service,
        object? content,
        string? title = null,
        ToastPlacement? placement = null) =>
        Show(service, content, title, ToastSeverity.Information, placement);

    /// <summary>Shows a success toast.</summary>
    public static IToastSession ShowSuccess(
        this IToastService service,
        object? content,
        string? title = null,
        ToastPlacement? placement = null) =>
        Show(service, content, title, ToastSeverity.Success, placement);

    /// <summary>Shows a warning toast.</summary>
    public static IToastSession ShowWarning(
        this IToastService service,
        object? content,
        string? title = null,
        ToastPlacement? placement = null) =>
        Show(service, content, title, ToastSeverity.Warning, placement);

    /// <summary>Shows an error toast.</summary>
    public static IToastSession ShowError(
        this IToastService service,
        object? content,
        string? title = null,
        ToastPlacement? placement = null) =>
        Show(service, content, title, ToastSeverity.Error, placement);

    private static IToastSession Show(
        IToastService service,
        object? content,
        string? title,
        ToastSeverity severity,
        ToastPlacement? placement)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        return service.Show(new ToastRequest(content)
        {
            Title = title,
            Severity = severity,
            Placement = placement,
        });
    }
}
