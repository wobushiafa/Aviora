using Aviora.Presentation.Toasts;

namespace Aviora.Controls;

/// <summary>Provides data for a toast opened event.</summary>
public sealed class ToastOpenedEventArgs : EventArgs
{
    internal ToastOpenedEventArgs(Guid id, ToastRequest request, Toast toast)
    {
        Id = id;
        Request = request;
        Toast = toast;
    }

    /// <summary>Gets the presentation identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the originating request.</summary>
    public ToastRequest Request { get; }

    /// <summary>Gets the generated toast control.</summary>
    public Toast Toast { get; }
}

/// <summary>Provides data for a toast closed event.</summary>
public sealed class ToastClosedEventArgs : EventArgs
{
    internal ToastClosedEventArgs(Guid id, ToastRequest request, ToastDismissReason reason)
    {
        Id = id;
        Request = request;
        Reason = reason;
    }

    /// <summary>Gets the presentation identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the originating request.</summary>
    public ToastRequest Request { get; }

    /// <summary>Gets why the toast was dismissed.</summary>
    public ToastDismissReason Reason { get; }
}
