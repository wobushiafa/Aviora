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
            IDrawerService drawerService = new DrawerService();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(drawerService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
