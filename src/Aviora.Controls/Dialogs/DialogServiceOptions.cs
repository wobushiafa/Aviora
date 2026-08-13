namespace Aviora.Controls;

/// <summary>Defines the application-wide defaults applied by <see cref="DialogService"/>.</summary>
public class DialogServiceOptions
{
    private TimeSpan _animationDuration = TimeSpan.FromMilliseconds(180);

    /// <summary>Gets whether interacting with the overlay closes a dialog by default.</summary>
    public bool IsLightDismissEnabled { get; init; }

    /// <summary>Gets whether Escape closes a dialog by default.</summary>
    public bool IsEscapeKeyEnabled { get; init; }

    /// <summary>Gets whether the modal overlay is visible by default.</summary>
    public bool IsOverlayVisible { get; init; } = true;

    /// <summary>Gets whether dialog transitions are enabled by default.</summary>
    public bool IsAnimationEnabled { get; init; } = true;

    /// <summary>Gets the default open and close transition duration.</summary>
    public TimeSpan AnimationDuration
    {
        get => _animationDuration;
        init
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Animation duration cannot be negative.");
            }

            _animationDuration = value;
        }
    }
}

/// <summary>Provides compatibility for the former dialog service options type.</summary>
[Obsolete("Use DialogServiceOptions instead.")]
public sealed class DialogOptions : DialogServiceOptions
{
}
