using Aviora.Presentation.Drawers;
using Aviora.Presentation.Dialogs;
using Aviora.Presentation.Loadings;
using Aviora.Presentation.Toasts;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aviora.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private DemoPageViewModel _selectedPage;

    public MainWindowViewModel(
        IDrawerService drawerService,
        IDialogService dialogService,
        ILoadingService loadingService,
        IToastService toastService)
    {
        Pages =
        [
            new ChartsPageViewModel(),
            new DialGaugePageViewModel(),
            new ThermometerPageViewModel(),
            new LoadingPageViewModel(loadingService),
            new ProgressPageViewModel(),
            new DrawerPageViewModel(drawerService),
            new DialogPageViewModel(dialogService),
            new ToastPageViewModel(toastService),
        ];
        _selectedPage = Pages[0];
    }

    public IReadOnlyList<DemoPageViewModel> Pages { get; }
}
