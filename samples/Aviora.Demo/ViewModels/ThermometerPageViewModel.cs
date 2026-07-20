using Avalonia.Media;
using Aviora.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class ThermometerPageViewModel : DemoPageViewModel
{
    private static readonly IBrush CoolBrush = Brushes.Teal;
    private static readonly IBrush WarmBrush = Brushes.OrangeRed;

    [ObservableProperty]
    private double _value = 42;

    [ObservableProperty]
    private ThermometerRangeOption _selectedRange;

    [ObservableProperty]
    private bool _showTicks = true;

    [ObservableProperty]
    private bool _showTickLabels = true;

    [ObservableProperty]
    private int _tickCount = 7;

    [ObservableProperty]
    private int _tickLabelInterval = 1;

    [ObservableProperty]
    private double _tickLabelFontSize = 10;

    [ObservableProperty]
    private double _tickLabelSpacing = 4;

    [ObservableProperty]
    private LiquidBrushMappingMode _mappingMode = LiquidBrushMappingMode.FullRange;

    [ObservableProperty]
    private double _mappingValue = 35;

    [ObservableProperty]
    private double _transitionValue = 25;

    [ObservableProperty]
    private IBrush _animatedBrush = CoolBrush;

    public ThermometerPageViewModel()
        : base("Thermometer")
    {
        RangeOptions =
        [
            new ThermometerRangeOption("Temperature", -20, 120),
            new ThermometerRangeOption("Percentage", 0, 100),
            new ThermometerRangeOption("Pressure", 0, 250),
            new ThermometerRangeOption("Bipolar", -100, 100),
        ];
        _selectedRange = RangeOptions[0];
    }

    public IReadOnlyList<ThermometerRangeOption> RangeOptions { get; }

    public IReadOnlyList<int> TickCountOptions { get; } = [4, 5, 7, 10, 12, 20];

    public IReadOnlyList<int> TickLabelIntervalOptions { get; } = [1, 2, 3, 4, 5];

    public IReadOnlyList<double> TickLabelFontSizeOptions { get; } = [8, 10, 12, 14, 16];

    public IReadOnlyList<double> TickLabelSpacingOptions { get; } = [0, 2, 4, 8, 12];

    public IReadOnlyList<LiquidBrushMappingMode> MappingModes { get; } =
        Enum.GetValues<LiquidBrushMappingMode>();

    public Func<double, string?> TemperatureTickLabelFormatter { get; } =
        value => $"{value:0} °C";

    [RelayCommand]
    private void ToggleTransitionValue() => TransitionValue = TransitionValue < 50 ? 85 : 20;

    [RelayCommand]
    private void ToggleAnimatedBrush() =>
        AnimatedBrush = ReferenceEquals(AnimatedBrush, CoolBrush) ? WarmBrush : CoolBrush;
}

public sealed record ThermometerRangeOption(string Name, double Minimum, double Maximum);
