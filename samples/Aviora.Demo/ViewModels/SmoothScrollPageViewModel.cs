using Avalonia.Animation.Easings;
using Aviora.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public sealed record EasingOption(string Name, Easing Easing)
{
    public override string ToString() => Name;
}

public sealed record SmoothScrollRowItem(int Index, string Title, string Subtitle, string BadgeHex);

public sealed record SmoothScrollGalleryItem(string Tag, string Title, string Detail, string AccentHex);

public sealed partial class SmoothScrollPageViewModel : DemoPageViewModel
{
    [ObservableProperty]
    private bool _isSmoothEnabled = true;

    [ObservableProperty]
    private SmoothScrollPreset _selectedPreset = SmoothScrollPreset.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExponentialMode))]
    [NotifyPropertyChangedFor(nameof(IsEasingMode))]
    private SmoothScrollCurveMode _selectedCurveMode = SmoothScrollCurveMode.Exponential;

    [ObservableProperty]
    private SmoothScrollOrientation _selectedOrientation = SmoothScrollOrientation.Both;

    [ObservableProperty]
    private double _stepSize = 96;

    [ObservableProperty]
    private double _initialStepFactor = 0.22;

    [ObservableProperty]
    private double _decayRate = 18;

    [ObservableProperty]
    private double _minSpeed = 48;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private double _durationMs = 220;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveEasing))]
    private EasingOption _selectedEasingOption;

    [ObservableProperty]
    private double _maxQueueDistance = 640;

    [ObservableProperty]
    private double _trackpadThreshold = 0.5;

    [ObservableProperty]
    private bool _isShiftWheelHorizontalEnabled = true;

    [ObservableProperty]
    private bool _autoHorizontalOnGallery = true;

    public SmoothScrollPageViewModel()
        : base("SmoothScroll")
    {
        EasingOptions =
        [
            new EasingOption("CubicEaseOut", new CubicEaseOut()),
            new EasingOption("SineEaseOut", new SineEaseOut()),
            new EasingOption("QuadraticEaseOut", new QuadraticEaseOut()),
            new EasingOption("ExponentialEaseOut", new ExponentialEaseOut()),
            new EasingOption("LinearEasing", new LinearEasing()),
        ];
        _selectedEasingOption = EasingOptions[0];

        string[] palette = ["#0F766E", "#1D4ED8", "#7C3AED", "#B45309", "#BE123C", "#0369A1"];
        SampleRows = Enumerable.Range(1, 36)
            .Select(i => new SmoothScrollRowItem(
                i,
                $"Smooth scroll item #{i:00}",
                i % 3 == 0
                    ? "Adjustable step size, damping, and easing curve."
                    : "Works on ScrollViewer, ListBox, TreeView, and ItemsControl.",
                palette[(i - 1) % palette.Length]))
            .ToArray();

        GalleryItems = Enumerable.Range(1, 16)
            .Select(i => new SmoothScrollGalleryItem(
                $"CARD {i:00}",
                $"Horizon {i:00}",
                "Hold Shift or turn on AutoHorizontalWhenVerticalDisabled.",
                palette[(i - 1) % palette.Length]))
            .ToArray();
    }

    public SmoothScrollPreset[] Presets { get; } = Enum.GetValues<SmoothScrollPreset>();

    public SmoothScrollCurveMode[] CurveModes { get; } = Enum.GetValues<SmoothScrollCurveMode>();

    public SmoothScrollOrientation[] Orientations { get; } = Enum.GetValues<SmoothScrollOrientation>();

    public IReadOnlyList<EasingOption> EasingOptions { get; }

    public IReadOnlyList<SmoothScrollRowItem> SampleRows { get; }

    public IReadOnlyList<SmoothScrollGalleryItem> GalleryItems { get; }

    public bool IsExponentialMode => SelectedCurveMode == SmoothScrollCurveMode.Exponential;

    public bool IsEasingMode => SelectedCurveMode == SmoothScrollCurveMode.Easing;

    public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

    public Easing? ActiveEasing => SelectedEasingOption.Easing;

    partial void OnSelectedPresetChanged(SmoothScrollPreset value) => ApplyPresetDefaults(value);

    [RelayCommand]
    private void ResetPreset() => ApplyPresetDefaults(SelectedPreset);

    private void ApplyPresetDefaults(SmoothScrollPreset preset)
    {
        switch (preset)
        {
            case SmoothScrollPreset.Snappy:
                StepSize = 96;
                InitialStepFactor = 0.32;
                DecayRate = 26;
                MinSpeed = 72;
                DurationMs = 150;
                break;
            case SmoothScrollPreset.Silky:
                StepSize = 112;
                InitialStepFactor = 0.14;
                DecayRate = 11.5;
                MinSpeed = 32;
                DurationMs = 340;
                break;
            default:
                StepSize = 96;
                InitialStepFactor = 0.22;
                DecayRate = 18;
                MinSpeed = 48;
                DurationMs = 220;
                break;
        }
    }
}
