namespace Aviora.Presentation.Toasts;

/// <summary>Identifies why a toast notification was dismissed.</summary>
public enum ToastDismissReason
{
    /// <summary>The configured display duration elapsed.</summary>
    Timeout,

    /// <summary>The user invoked the dismiss button.</summary>
    User,

    /// <summary>The toast action completed and requested dismissal.</summary>
    Action,

    /// <summary>The presentation session was dismissed programmatically.</summary>
    Programmatic,

    /// <summary>The request cancellation token was canceled.</summary>
    Canceled,

    /// <summary>The host was detached while the toast was active.</summary>
    HostDetached,

    /// <summary>The host queue was cleared.</summary>
    Cleared,
}
