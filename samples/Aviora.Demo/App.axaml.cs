using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Aviora.Controls;
using Aviora.Demo.ViewModels;
using Aviora.Demo.Views;
using Aviora.Presentation.Drawers;

namespace Aviora.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var drawerService = new DrawerService();
            var dialogService = new DialogService();
            var loadingService = new LoadingService();
            var toastService = new ToastService();
            desktop.MainWindow = new MainWindow(drawerService, dialogService, loadingService, toastService)
            {
                DataContext = new MainWindowViewModel(drawerService, dialogService, loadingService, toastService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
