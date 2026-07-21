using Aviora.Presentation.Dialogs;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class ConfirmationDialogViewModel(IDialogSession session) : ViewModelBase
{
    [RelayCommand]
    private void Confirm() => session.Close(true);

    [RelayCommand]
    private void Cancel() => session.Cancel();
}
