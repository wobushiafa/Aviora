using Aviora.Presentation.Drawers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aviora.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private DemoPageViewModel _selectedPage;

    public MainWindowViewModel(IDrawerService drawerService)
    {
        DrawerService = drawerService;
        Pages =
        [
            new ChartsPageViewModel(),
            new DialGaugePageViewModel(),
            new ThermometerPageViewModel(),
            new DrawerPageViewModel(drawerService),
        ];
        _selectedPage = Pages[0];
    }

    public IDrawerService DrawerService { get; }

    public IReadOnlyList<DemoPageViewModel> Pages { get; }
}
