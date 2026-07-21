namespace Aviora.Presentation.Loadings;

/// <summary>Describes a framework-independent loading overlay request.</summary>
public sealed class LoadingRequest
{
    /// <summary>Initializes a loading request with optional content or a ViewModel.</summary>
    public LoadingRequest(object? content = null)
    {
        Content = content;
    }

    /// <summary>Gets the optional content or ViewModel displayed by the host.</summary>
    public object? Content { get; }

    /// <summary>Gets the identifier of the target loading overlay host.</summary>
    public string HostId { get; init; } = LoadingHost.DefaultId;

    /// <summary>Gets caller-defined metadata associated with the request.</summary>
    public object? Tag { get; init; }
}
