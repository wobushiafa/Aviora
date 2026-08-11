using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.VisualTree;
using Aviora.Presentation.Toasts;

namespace Aviora.Controls;

/// <summary>Displays one styleable toast notification.</summary>
[PseudoClasses(":information", ":success", ":warning", ":error", ":dismissible", ":actionable", ":untitled")]
public class Toast : ContentControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Toast, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Severity"/> property.</summary>
    public static readonly StyledProperty<ToastSeverity> SeverityProperty =
        AvaloniaProperty.Register<Toast, ToastSeverity>(nameof(Severity));

    /// <summary>Defines the <see cref="Placement"/> property.</summary>
    public static readonly StyledProperty<ToastPlacement> PlacementProperty =
        AvaloniaProperty.Register<Toast, ToastPlacement>(nameof(Placement), ToastPlacement.TopRight);

    /// <summary>Defines the <see cref="IsDismissible"/> property.</summary>
    public static readonly StyledProperty<bool> IsDismissibleProperty =
        AvaloniaProperty.Register<Toast, bool>(nameof(IsDismissible), true);

    /// <summary>Defines the <see cref="IsClickDismissEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsClickDismissEnabledProperty =
        AvaloniaProperty.Register<Toast, bool>(nameof(IsClickDismissEnabled), true);

    /// <summary>Defines the <see cref="ActionText"/> property.</summary>
    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<Toast, string?>(nameof(ActionText));

    /// <summary>Defines the <see cref="ActionCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<Toast, ICommand?>(nameof(ActionCommand));

    /// <summary>Defines the <see cref="ActionCommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<Toast, object?>(nameof(ActionCommandParameter));

    private readonly DismissCommand _dismissCommand;

    static Toast()
    {
        SeverityProperty.Changed.AddClassHandler<Toast>((toast, _) => toast.UpdatePseudoClasses());
        IsDismissibleProperty.Changed.AddClassHandler<Toast>((toast, _) => toast.UpdatePseudoClasses());
        IsClickDismissEnabledProperty.Changed.AddClassHandler<Toast>((toast, _) => toast.UpdatePseudoClasses());
        ActionTextProperty.Changed.AddClassHandler<Toast>((toast, _) => toast.UpdatePseudoClasses());
        ActionCommandProperty.Changed.AddClassHandler<Toast>((toast, _) => toast.UpdatePseudoClasses());
        TitleProperty.Changed.AddClassHandler<Toast>((toast, _) => toast.UpdatePseudoClasses());
    }

    /// <summary>Initializes a toast notification.</summary>
    public Toast()
    {
        _dismissCommand = new DismissCommand(this);
        CloseCommand = _dismissCommand;
        UpdatePseudoClasses();
    }

    /// <summary>Gets or sets the optional short heading.</summary>
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Gets or sets the semantic severity.</summary>
    public ToastSeverity Severity { get => GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }

    /// <summary>Gets or sets where this toast is displayed by its host.</summary>
    public ToastPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Gets or sets whether the user can dismiss this toast.</summary>
    public bool IsDismissible { get => GetValue(IsDismissibleProperty); set => SetValue(IsDismissibleProperty, value); }

    /// <summary>Gets or sets whether clicking non-interactive toast content requests dismissal.</summary>
    public bool IsClickDismissEnabled
    {
        get => GetValue(IsClickDismissEnabledProperty);
        set => SetValue(IsClickDismissEnabledProperty, value);
    }

    /// <summary>Gets or sets the optional default action label.</summary>
    public string? ActionText { get => GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }

    /// <summary>Gets or sets the command invoked by the default action button.</summary>
    public ICommand? ActionCommand { get => GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    /// <summary>Gets or sets the parameter passed to <see cref="ActionCommand"/>.</summary>
    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    /// <summary>Gets the command that requests user dismissal.</summary>
    public ICommand CloseCommand { get; }

    /// <summary>Occurs when the user invokes <see cref="CloseCommand"/>.</summary>
    public event EventHandler? DismissRequested;

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!e.Handled &&
            IsDismissible &&
            IsClickDismissEnabled &&
            e.InitialPressMouseButton == MouseButton.Left &&
            !IsInteractiveSource(e.Source))
        {
            e.Handled = true;
            DismissRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":information", Severity == ToastSeverity.Information);
        PseudoClasses.Set(":success", Severity == ToastSeverity.Success);
        PseudoClasses.Set(":warning", Severity == ToastSeverity.Warning);
        PseudoClasses.Set(":error", Severity == ToastSeverity.Error);
        PseudoClasses.Set(":dismissible", IsDismissible);
        PseudoClasses.Set(":actionable", !string.IsNullOrWhiteSpace(ActionText) && ActionCommand is not null);
        PseudoClasses.Set(":untitled", string.IsNullOrWhiteSpace(Title));
        _dismissCommand?.RaiseCanExecuteChanged();
    }

    private static bool IsInteractiveSource(object? source)
    {
        for (Visual? visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is Button)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class DismissCommand(Toast owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => owner.IsDismissible;

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                owner.DismissRequested?.Invoke(owner, EventArgs.Empty);
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
