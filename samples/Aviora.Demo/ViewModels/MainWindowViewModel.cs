using Aviora.Presentation.Drawers;
using Aviora.Presentation.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aviora.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private DemoPageViewModel _selectedPage;

    public MainWindowViewModel(IDrawerService drawerService, IDialogService dialogService)
    {
        Pages =
        [
            new ChartsPageViewModel(),
            new DialGaugePageViewModel(),
            new ThermometerPageViewModel(),
            new DrawerPageViewModel(drawerService),
            new DialogPageViewModel(dialogService),
        ];
        _selectedPage = Pages[0];
    }

    public IReadOnlyList<DemoPageViewModel> Pages { get; }
}
