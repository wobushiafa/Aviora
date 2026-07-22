using Aviora.Presentation.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class DialogPageViewModel(IDialogService dialogService) : DemoPageViewModel("Dialog")
{
    [ObservableProperty]
    private string _dialogStatus = "No dialog result yet";

    [ObservableProperty]
    private bool _isLightDismissEnabled;

    [ObservableProperty]
    private bool _isEscapeKeyEnabled = true;

    [ObservableProperty]
    private bool _isOverlayVisible = true;

    [ObservableProperty]
    private bool _isAnimationEnabled = true;

    [ObservableProperty]
    private double _dialogWidth = 440;

    [RelayCommand]
    private async Task OpenProfileDialogAsync()
    {
        DialogResult result = await dialogService.ShowAsync(new DialogRequest(null)
        {
            ContentFactory = session => new ProfileDialogViewModel(session, dialogService),
            Width = DialogWidth,
            IsLightDismissEnabled = IsLightDismissEnabled,
            IsEscapeKeyEnabled = IsEscapeKeyEnabled,
            IsOverlayVisible = IsOverlayVisible,
            IsAnimationEnabled = IsAnimationEnabled,
        });

        DialogStatus = result.Value is ProfileDialogResult profile
            ? $"Saved {profile.DisplayName} ({profile.Email})"
            : $"Profile dialog closed: {result.Reason}";
    }

    [RelayCommand]
    private async Task OpenConfirmationAsync()
    {
        DialogResult result = await dialogService.ShowAsync(
            session => new ConfirmationDialogViewModel(session));

        DialogStatus = result.GetValue<bool>()
            ? "Action confirmed"
            : $"Confirmation closed: {result.Reason}";
    }
}
