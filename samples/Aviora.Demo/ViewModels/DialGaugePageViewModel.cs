using Avalonia.Media;
using Aviora.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class DialGaugePageViewModel : DemoPageViewModel
{
    private static readonly IBrush BlueNeedleBrush = Brushes.DodgerBlue;
    private static readonly IBrush PinkNeedleBrush = Brushes.DeepPink;

    [ObservableProperty]
    private double _value = 64;

    [ObservableProperty]
    private bool _showTicks = true;

    [ObservableProperty]
    private bool _showTickLabels = true;

    [ObservableProperty]
    private int _tickCount = 20;

    [ObservableProperty]
    private int _tickLabelInterval = 5;

    [ObservableProperty]
    private DialGaugeTickColorMode _tickColorMode = DialGaugeTickColorMode.Range;

    [ObservableProperty]
    private double _transitionValue = 22;

    [ObservableProperty]
    private IBrush _transitionNeedleBrush = BlueNeedleBrush;

    public DialGaugePageViewModel()
        : base("Dial gauge")
    {
    }

    public IReadOnlyList<DialGaugeTickColorMode> TickColorModes { get; } =
        Enum.GetValues<DialGaugeTickColorMode>();

    [RelayCommand]
    private void ToggleTransitionValue() =>
        TransitionValue = TransitionValue < 50 ? 88 : 22;

    [RelayCommand]
    private void ToggleTransitionNeedle() =>
        TransitionNeedleBrush = ReferenceEquals(TransitionNeedleBrush, BlueNeedleBrush)
            ? PinkNeedleBrush
            : BlueNeedleBrush;
}
