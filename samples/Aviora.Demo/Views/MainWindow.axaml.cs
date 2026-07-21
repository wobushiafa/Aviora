using Avalonia.Controls;
using Aviora.Controls;

namespace Aviora.Demo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(
        IDrawerHostService drawerService,
        IDialogHostService dialogService,
        ILoadingHostService loadingService)
        : this()
    {
        DrawerHost.Service = drawerService;
        DialogHost.Service = dialogService;
        LoadingHost.Service = loadingService;
    }
}
