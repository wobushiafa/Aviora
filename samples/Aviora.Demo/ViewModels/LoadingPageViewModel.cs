using Aviora.Presentation.Loadings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class LoadingPageViewModel : DemoPageViewModel
{
    private readonly ILoadingService _loadingService;

    [ObservableProperty]
    private string _operationStatus = "Ready";

    public LoadingPageViewModel(ILoadingService loadingService)
        : base("Loading")
    {
        _loadingService = loadingService;
    }

    [RelayCommand]
    private async Task RunGlobalLoadingAsync()
    {
        OperationStatus = "Refreshing";
        await _loadingService.RunAsync(
            async cancellationToken => await Task.Delay(1600, cancellationToken),
            new LoadingRequest("Refreshing workspace"));
        OperationStatus = "Completed";
    }

    [RelayCommand]
    private async Task RunConcurrentLoadingAsync()
    {
        OperationStatus = "Running two operations";
        using (ILoadingSession first = _loadingService.Show(new LoadingRequest("Loading account data")))
        {
            Task firstOperation = Task.Delay(1800);
            await Task.Delay(300);
            using (ILoadingSession second = _loadingService.Show(new LoadingRequest("Synchronizing notifications")))
            {
                await Task.Delay(800);
            }

            await firstOperation;
        }

        OperationStatus = "Both completed";
    }
}
