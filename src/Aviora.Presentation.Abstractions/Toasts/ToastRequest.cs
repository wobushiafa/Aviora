using System.Windows.Input;

namespace Aviora.Presentation.Toasts;

/// <summary>Describes a framework-independent toast notification request.</summary>
public sealed class ToastRequest
{
    /// <summary>Initializes a request with content or a ViewModel.</summary>
    public ToastRequest(object? content)
    {
        Content = content;
    }

    /// <summary>Gets the content or ViewModel displayed by the toast.</summary>
    public object? Content { get; }

    /// <summary>Gets an optional factory used to create session-aware content.</summary>
    public Func<IToastSession, object?>? ContentFactory { get; init; }

    /// <summary>Creates a request whose content receives its presentation session.</summary>
    public static ToastRequest Create(Func<IToastSession, object?> contentFactory)
    {
        if (contentFactory is null)
        {
            throw new ArgumentNullException(nameof(contentFactory));
        }

        return new ToastRequest(null) { ContentFactory = contentFactory };
    }

    /// <summary>Gets the identifier of the target toast host.</summary>
    public string HostId { get; init; } = ToastHosts.DefaultId;

    /// <summary>Gets an optional short heading.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the semantic severity used by the default visual.</summary>
    public ToastSeverity Severity { get; init; }

    /// <summary>Gets an optional host placement override.</summary>
    public ToastPlacement? Placement { get; init; }

    /// <summary>Gets an optional display duration override. Use <see cref="Timeout.InfiniteTimeSpan"/> for a persistent toast.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Gets an optional user-dismiss behavior override.</summary>
    public bool? IsDismissible { get; init; }

    /// <summary>Gets an optional override for dismissing the toast by clicking its non-interactive content.</summary>
    public bool? IsClickDismissEnabled { get; init; }

    /// <summary>Gets optional text for the default action button.</summary>
    public string? ActionText { get; init; }

    /// <summary>Gets the optional command invoked by the default action button.</summary>
    public ICommand? ActionCommand { get; init; }

    /// <summary>Gets the parameter passed to <see cref="ActionCommand"/>.</summary>
    public object? ActionCommandParameter { get; init; }

    /// <summary>Gets whether a successful action invocation dismisses the toast.</summary>
    public bool DismissOnAction { get; init; } = true;

    /// <summary>Gets caller-defined metadata associated with the request.</summary>
    public object? Tag { get; init; }
}
