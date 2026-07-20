using Aviora.Presentation.Drawers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class DrawerDemoViewModel(IDrawerService drawerService) : ViewModelBase
{
    [ObservableProperty]
    private string _displayName = "Aviora user";

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _showFooterActions = true;

    [RelayCommand]
    private void Save() => drawerService.Close(result: DisplayName);

    [RelayCommand]
    private void Cancel() => drawerService.Close();
}
