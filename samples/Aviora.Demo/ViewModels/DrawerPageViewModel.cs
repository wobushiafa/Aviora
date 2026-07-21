using Aviora.Presentation.Drawers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class DrawerPageViewModel(IDrawerService drawerService) : DemoPageViewModel("Drawer")
{
    public IReadOnlyList<DrawerDisplayMode> DisplayModes { get; } = Enum.GetValues<DrawerDisplayMode>();

    [ObservableProperty]
    private string _drawerStatus = "No drawer result yet";

    [ObservableProperty]
    private DrawerDisplayMode _displayMode = DrawerDisplayMode.Overlay;

    [ObservableProperty]
    private bool _isLightDismissEnabled = true;

    [ObservableProperty]
    private bool _isEscapeKeyEnabled = true;

    [ObservableProperty]
    private bool _isOverlayVisible = true;

    [ObservableProperty]
    private bool _isAnimationEnabled = true;

    [ObservableProperty]
    private bool _showFooterActions = true;

    [ObservableProperty]
    private double _drawerSize = 380;

    [RelayCommand]
    private Task OpenLeftDrawerAsync() => OpenDrawerAsync(DrawerPlacement.Left);

    [RelayCommand]
    private Task OpenTopDrawerAsync() => OpenDrawerAsync(DrawerPlacement.Top);

    [RelayCommand]
    private Task OpenRightDrawerAsync() => OpenDrawerAsync(DrawerPlacement.Right);

    [RelayCommand]
    private Task OpenBottomDrawerAsync() => OpenDrawerAsync(DrawerPlacement.Bottom);

    private async Task OpenDrawerAsync(DrawerPlacement placement)
    {
        DrawerResult result = await drawerService.ShowAsync(new DrawerRequest(null)
        {
            ContentFactory = session =>
                new DrawerDemoViewModel(session) { ShowFooterActions = ShowFooterActions },
            Placement = placement,
            DisplayMode = DisplayMode,
            Size = DrawerSize,
            IsLightDismissEnabled = IsLightDismissEnabled,
            IsEscapeKeyEnabled = IsEscapeKeyEnabled,
            IsOverlayVisible = IsOverlayVisible,
            IsAnimationEnabled = IsAnimationEnabled,
        });

        DrawerStatus = result.Value is string name
            ? $"{placement} returned: {name}"
            : $"{placement} closed: {result.Reason}";
    }
}
