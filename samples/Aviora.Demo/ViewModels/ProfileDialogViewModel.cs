using Aviora.Presentation.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public sealed record ProfileDialogResult(string DisplayName, string Email);

public partial class ProfileDialogViewModel(IDialogSession session) : ViewModelBase
{
    [ObservableProperty]
    private string _displayName = "Aviora user";

    [ObservableProperty]
    private string _email = "user@example.com";

    [RelayCommand]
    private void Save() => session.Close(new ProfileDialogResult(DisplayName, Email));

    [RelayCommand]
    private void Cancel() => session.Cancel();
}
