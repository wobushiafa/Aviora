namespace Aviora.Presentation.Dialogs;

/// <summary>Describes a framework-independent dialog presentation request.</summary>
public sealed class DialogRequest
{
    /// <summary>Initializes a request with the content or ViewModel to present.</summary>
    public DialogRequest(object? content)
    {
        Content = content;
    }

    /// <summary>Gets the content or ViewModel to present.</summary>
    public object? Content { get; }

    /// <summary>Gets the optional factory used to create session-aware content.</summary>
    public Func<IDialogSession, object?>? ContentFactory { get; init; }

    /// <summary>Creates a request whose content receives its presentation session.</summary>
    public static DialogRequest Create(Func<IDialogSession, object?> contentFactory)
    {
        return new DialogRequest(null)
        {
            ContentFactory = contentFactory ?? throw new ArgumentNullException(nameof(contentFactory)),
        };
    }

    /// <summary>Gets the identifier of the target dialog host.</summary>
    public string HostId { get; init; } = DialogHost.DefaultId;

    /// <summary>Gets an optional dialog width override.</summary>
    public double? Width { get; init; }

    /// <summary>Gets an optional dialog height override.</summary>
    public double? Height { get; init; }

    /// <summary>Gets an optional light-dismiss override.</summary>
    public bool? IsLightDismissEnabled { get; init; }

    /// <summary>Gets an optional Escape-key override.</summary>
    public bool? IsEscapeKeyEnabled { get; init; }

    /// <summary>Gets an optional overlay visibility override.</summary>
    public bool? IsOverlayVisible { get; init; }

    /// <summary>Gets an optional animation enabled override.</summary>
    public bool? IsAnimationEnabled { get; init; }

    /// <summary>Gets an optional animation duration override.</summary>
    public TimeSpan? AnimationDuration { get; init; }

    /// <summary>Gets caller-defined metadata associated with the request.</summary>
    public object? Tag { get; init; }
}
