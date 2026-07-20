namespace Aviora.Presentation.Drawers;

/// <summary>
/// Contains the value and reason returned when a drawer presentation closes.
/// </summary>
public sealed record DrawerResult(object? Value, DrawerCloseReason Reason)
{
    /// <summary>Gets whether the presentation was canceled.</summary>
    public bool IsCanceled => Reason == DrawerCloseReason.Canceled;

    /// <summary>Gets the result value when it is assignable to <typeparamref name="T"/>.</summary>
    public T? GetValue<T>() => Value is T value ? value : default;
}
