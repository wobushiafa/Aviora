using Aviora.Presentation.Drawers;
using Aviora.Presentation.Dialogs;
using Aviora.Presentation.Loadings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aviora.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private DemoPageViewModel _selectedPage;

    public MainWindowViewModel(
        IDrawerService drawerService,
        IDialogService dialogService,
        ILoadingService loadingService)
    {
        Pages =
        [
            new ChartsPageViewModel(),
            new DialGaugePageViewModel(),
            new ThermometerPageViewModel(),
            new LoadingPageViewModel(loadingService),
            new DrawerPageViewModel(drawerService),
            new DialogPageViewModel(dialogService),
        ];
        _selectedPage = Pages[0];
    }

    public IReadOnlyList<DemoPageViewModel> Pages { get; }
}
