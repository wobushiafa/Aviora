using System.ComponentModel;
using Aviora.Presentation.Drawers;

namespace Aviora.Controls;

/// <summary>Provides data for the drawer opening event.</summary>
public sealed class DrawerOpeningEventArgs : EventArgs
{
    internal DrawerOpeningEventArgs(DrawerRequest? request) => Request = request;

    /// <summary>Gets the service request that caused the drawer to open, if any.</summary>
    public DrawerRequest? Request { get; }
}

/// <summary>Provides cancelable data for the drawer closing event.</summary>
public sealed class DrawerClosingEventArgs : CancelEventArgs
{
    internal DrawerClosingEventArgs(DrawerCloseReason reason, object? result)
    {
        Reason = reason;
        Result = result;
    }

    /// <summary>Gets the reason for closing.</summary>
    public DrawerCloseReason Reason { get; }

    /// <summary>Gets the result that will be returned.</summary>
    public object? Result { get; }
}

/// <summary>Provides data for the drawer closed event.</summary>
public sealed class DrawerClosedEventArgs : EventArgs
{
    internal DrawerClosedEventArgs(DrawerCloseReason reason, object? result)
    {
        Reason = reason;
        Result = result;
    }

    /// <summary>Gets the reason the drawer closed.</summary>
    public DrawerCloseReason Reason { get; }

    /// <summary>Gets the returned result.</summary>
    public object? Result { get; }
}
