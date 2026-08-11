using Aviora.Presentation.Toasts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class ToastPageViewModel(IToastService toastService) : DemoPageViewModel("Toast")
{
    private IToastSession? _lastSession;

    public IReadOnlyList<ToastPlacement> Placements { get; } = Enum.GetValues<ToastPlacement>();

    [ObservableProperty]
    private ToastPlacement _selectedPlacement = ToastPlacement.TopRight;

    [ObservableProperty]
    private bool _isPersistent;

    [ObservableProperty]
    private string _toastStatus = "Ready";

    [RelayCommand]
    private void ShowInformation() => Show(
        "A background sync has started.",
        "Syncing workspace",
        ToastSeverity.Information);

    [RelayCommand]
    private void ShowSuccess() => Show(
        "Your changes are available across all devices.",
        "Settings saved",
        ToastSeverity.Success);

    [RelayCommand]
    private void ShowWarning() => Show(
        "The current session expires in five minutes.",
        "Session expiring",
        ToastSeverity.Warning);

    [RelayCommand]
    private void ShowError() => Show(
        "Check your connection and try again.",
        "Upload failed",
        ToastSeverity.Error);

    [RelayCommand]
    private void ShowAction()
    {
        _lastSession = toastService.Show(new ToastRequest("The selected item was moved to the archive.")
        {
            Title = "Item archived",
            Severity = ToastSeverity.Information,
            Placement = SelectedPlacement,
            Duration = IsPersistent ? Timeout.InfiniteTimeSpan : null,
            ActionText = "Undo",
            ActionCommand = UndoCommand,
        });
        ToastStatus = $"Action toast shown at {SelectedPlacement}";
    }

    [RelayCommand]
    private void DismissLast()
    {
        ToastStatus = _lastSession?.Dismiss() == true
            ? "Last toast dismissed"
            : "No active toast to dismiss";
    }

    [RelayCommand]
    private void Undo() => ToastStatus = "Archive action undone";

    private void Show(object content, string title, ToastSeverity severity)
    {
        _lastSession = toastService.Show(new ToastRequest(content)
        {
            Title = title,
            Severity = severity,
            Placement = SelectedPlacement,
            Duration = IsPersistent ? Timeout.InfiniteTimeSpan : null,
        });
        ToastStatus = $"{severity} toast shown at {SelectedPlacement}";
    }
}
