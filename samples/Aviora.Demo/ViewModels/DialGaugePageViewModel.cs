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
    private double _minimum = -20;

    [ObservableProperty]
    private double _maximum = 120;

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
    private double _tickLabelFontSize = 11;

    [ObservableProperty]
    private DialGaugeTickColorMode _tickColorMode = DialGaugeTickColorMode.Range;

    [ObservableProperty]
    private DialGaugeLabelOption _selectedLabelOption;

    [ObservableProperty]
    private DialGaugeFontFamilyOption _selectedFontFamily;

    [ObservableProperty]
    private DialGaugeFontWeightOption _selectedFontWeight;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedTickBrush;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedLowRangeBrush;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedMediumRangeBrush;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedHighRangeBrush;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedTickLabelBrush;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedNeedleBrush;

    [ObservableProperty]
    private DialGaugeBrushOption _selectedPivotBrush;

    [ObservableProperty]
    private double _transitionValue = 22;

    [ObservableProperty]
    private IBrush _transitionNeedleBrush = BlueNeedleBrush;

    public DialGaugePageViewModel()
        : base("Dial gauge")
    {
        BrushOptions =
        [
            new("Teal", Brush("#0F766E")),
            new("Green", Brush("#16A34A")),
            new("Cyan", Brush("#0891B2")),
            new("Blue", Brush("#2563EB")),
            new("Violet", Brush("#7C3AED")),
            new("Pink", Brush("#DB2777")),
            new("Orange", Brush("#F97316")),
            new("Amber", Brush("#F59E0B")),
            new("Red", Brush("#EF4444")),
            new("Gray", Brush("#64748B")),
            new("Ink", Brush("#1E293B")),
        ];
        LabelOptions =
        [
            new("Integer", "0", null),
            new("Decimal", "0.0", null),
            new("Percentage", null, value => $"{value:0}%"),
            new("Temperature", null, value => $"{value:0} °C"),
        ];
        FontFamilyOptions =
        [
            new("Default", FontFamily.Default),
            new("Inter", new FontFamily("Inter")),
            new("Monospace", new FontFamily("Consolas")),
        ];
        FontWeightOptions =
        [
            new("Normal", FontWeight.Normal),
            new("SemiBold", FontWeight.SemiBold),
            new("Bold", FontWeight.Bold),
        ];

        _selectedLabelOption = LabelOptions[0];
        _selectedFontFamily = FontFamilyOptions[0];
        _selectedFontWeight = FontWeightOptions[0];
        _selectedTickBrush = BrushOptions[2];
        _selectedLowRangeBrush = BrushOptions[1];
        _selectedMediumRangeBrush = BrushOptions[7];
        _selectedHighRangeBrush = BrushOptions[8];
        _selectedTickLabelBrush = BrushOptions[9];
        _selectedNeedleBrush = BrushOptions[3];
        _selectedPivotBrush = BrushOptions[10];
    }

    public IReadOnlyList<DialGaugeTickColorMode> TickColorModes { get; } =
        Enum.GetValues<DialGaugeTickColorMode>();

    public IReadOnlyList<DialGaugeBrushOption> BrushOptions { get; }

    public IReadOnlyList<DialGaugeLabelOption> LabelOptions { get; }

    public IReadOnlyList<DialGaugeFontFamilyOption> FontFamilyOptions { get; }

    public IReadOnlyList<DialGaugeFontWeightOption> FontWeightOptions { get; }

    [RelayCommand]
    private void ToggleTransitionValue() =>
        TransitionValue = TransitionValue < 50 ? 88 : 22;

    [RelayCommand]
    private void ToggleTransitionNeedle() =>
        TransitionNeedleBrush = ReferenceEquals(TransitionNeedleBrush, BlueNeedleBrush)
            ? PinkNeedleBrush
            : BlueNeedleBrush;

    private static IBrush Brush(string color) =>
        new SolidColorBrush(Color.Parse(color));
}

public sealed record DialGaugeBrushOption(string Name, IBrush Brush);

public sealed record DialGaugeLabelOption(
    string Name,
    string? Format,
    Func<double, string?>? Formatter);

public sealed record DialGaugeFontFamilyOption(string Name, FontFamily Value);

public sealed record DialGaugeFontWeightOption(string Name, FontWeight Value);
