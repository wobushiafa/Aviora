namespace Aviora.Presentation.Drawers;

/// <summary>
/// Describes a framework-independent drawer presentation request.
/// </summary>
public sealed class DrawerRequest
{
    /// <summary>Initializes a request with the content or view model to present.</summary>
    public DrawerRequest(object? content)
    {
        Content = content;
    }

    /// <summary>Gets the content or view model to present.</summary>
    public object? Content { get; }

    /// <summary>Gets the identifier of the target drawer host.</summary>
    public string HostId { get; init; } = DrawerHost.DefaultId;

    /// <summary>Gets an optional placement override.</summary>
    public DrawerPlacement? Placement { get; init; }

    /// <summary>Gets an optional display mode override.</summary>
    public DrawerDisplayMode? DisplayMode { get; init; }

    /// <summary>Gets an optional pane width or height override.</summary>
    public double? Size { get; init; }

    /// <summary>Gets an optional light-dismiss override.</summary>
    public bool? IsLightDismissEnabled { get; init; }

    /// <summary>Gets an optional Escape-key override.</summary>
    public bool? IsEscapeKeyEnabled { get; init; }

    /// <summary>Gets an optional overlay visibility override.</summary>
    public bool? IsOverlayVisible { get; init; }

    /// <summary>Gets an optional animation enabled override.</summary>
    public bool? IsAnimationEnabled { get; init; }

    /// <summary>Gets an optional pane animation duration override.</summary>
    public TimeSpan? PaneAnimationDuration { get; init; }

    /// <summary>Gets an optional overlay animation duration override.</summary>
    public TimeSpan? OverlayAnimationDuration { get; init; }

    /// <summary>Gets caller-defined metadata associated with the request.</summary>
    public object? Tag { get; init; }
}
