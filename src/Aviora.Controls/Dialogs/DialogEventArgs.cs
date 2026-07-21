using System.ComponentModel;
using Aviora.Presentation.Dialogs;

namespace Aviora.Controls;

/// <summary>Provides data for the dialog opening event.</summary>
public sealed class DialogOpeningEventArgs : EventArgs
{
    internal DialogOpeningEventArgs(DialogRequest? request) => Request = request;

    /// <summary>Gets the service request that caused the dialog to open, if any.</summary>
    public DialogRequest? Request { get; }
}

/// <summary>Provides cancelable data for the dialog closing event.</summary>
public sealed class DialogClosingEventArgs : CancelEventArgs
{
    internal DialogClosingEventArgs(DialogCloseReason reason, object? result)
    {
        Reason = reason;
        Result = result;
    }

    /// <summary>Gets the reason for closing.</summary>
    public DialogCloseReason Reason { get; }

    /// <summary>Gets the result that will be returned.</summary>
    public object? Result { get; }
}

/// <summary>Provides data for the dialog closed event.</summary>
public sealed class DialogClosedEventArgs : EventArgs
{
    internal DialogClosedEventArgs(DialogCloseReason reason, object? result)
    {
        Reason = reason;
        Result = result;
    }

    /// <summary>Gets the reason the dialog closed.</summary>
    public DialogCloseReason Reason { get; }

    /// <summary>Gets the returned result.</summary>
    public object? Result { get; }
}
