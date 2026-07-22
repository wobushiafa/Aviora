using Aviora.Presentation.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public sealed record ProfileDialogResult(string DisplayName, string Email);

public partial class ProfileDialogViewModel(
    IDialogSession session,
    IDialogService dialogService) : ViewModelBase
{
    [ObservableProperty]
    private string _displayName = "Aviora user";

    [ObservableProperty]
    private string _email = "user@example.com";

    [ObservableProperty]
    private string _nestedDialogStatus = "No child dialog result yet";

    [RelayCommand]
    private Task OpenNavigatedChildDialogAsync() =>
        OpenChildDialogAsync(DialogPresentationMode.Navigate);

    [RelayCommand]
    private Task OpenStackedChildDialogAsync() =>
        OpenChildDialogAsync(DialogPresentationMode.Stack);

    private async Task OpenChildDialogAsync(DialogPresentationMode presentationMode)
    {
        DialogResult result = await dialogService.ShowAsync(new DialogRequest(null)
        {
            ContentFactory = childSession => new ConfirmationDialogViewModel(childSession),
            PresentationMode = presentationMode,
            Width = 408,
        });

        NestedDialogStatus = result.GetValue<bool>()
            ? $"{presentationMode} child confirmed; parent dialog restored"
            : $"Child dialog closed: {result.Reason}";
    }

    [RelayCommand]
    private void Save() => session.Close(new ProfileDialogResult(DisplayName, Email));

    [RelayCommand]
    private void Cancel() => session.Cancel();
}
