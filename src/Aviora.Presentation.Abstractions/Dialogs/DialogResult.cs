namespace Aviora.Presentation.Dialogs;

/// <summary>Contains the value and reason returned when a dialog closes.</summary>
public sealed record DialogResult(object? Value, DialogCloseReason Reason)
{
    /// <summary>Gets whether the presentation was canceled.</summary>
    public bool IsCanceled => Reason == DialogCloseReason.Canceled;

    /// <summary>Gets the result value when it is assignable to <typeparamref name="T"/>.</summary>
    public T? GetValue<T>() => Value is T value ? value : default;
}
