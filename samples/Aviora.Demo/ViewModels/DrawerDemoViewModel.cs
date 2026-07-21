using Aviora.Presentation.Drawers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class DrawerDemoViewModel(IDrawerSession session) : ViewModelBase
{
    [ObservableProperty]
    private string _displayName = "Aviora user";

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _showFooterActions = true;

    [RelayCommand]
    private void Save() => session.Close(DisplayName);

    [RelayCommand]
    private void Cancel() => session.Cancel();
}
