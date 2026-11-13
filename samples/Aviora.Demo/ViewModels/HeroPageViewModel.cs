using Aviora.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public sealed partial class HeroPageViewModel : DemoPageViewModel
{
    [ObservableProperty]
    private HeroGalleryItem? _selectedItem;

    [ObservableProperty]
    private HeroGalleryItem? _fullscreenItem;

    [ObservableProperty]
    private bool _isFullscreenOpen;

    [ObservableProperty]
    private bool _isProfileExpanded;

    [ObservableProperty]
    private HeroFlightMode _selectedFlightMode = HeroFlightMode.Arc;

    [ObservableProperty]
    private double _durationMs = 380;

    [ObservableProperty]
    private bool _isAnimationEnabled = true;

    public HeroPageViewModel()
        : base("Hero")
    {
        Items =
        [
            new HeroGalleryItem(
                "aurora-ui",
                "Aurora Glass",
                "Design System",
                "Layered translucency & depth",
                "Demonstrates shared-element flight from a compact gallery card to a full-width hero banner with smooth size and corner-radius interpolation.",
                "#0F766E",
                "#14B8A6",
                "AG",
                "Components",
                "42"),
            new HeroGalleryItem(
                "telemetry-core",
                "Telemetry Pulse",
                "Observability",
                "Real-time stream metrics",
                "Watch the card badge and gradient surface fly across the viewport along a curved Material-style arc when opening or closing the detail view.",
                "#1D4ED8",
                "#60A5FA",
                "TP",
                "Throughput",
                "12.4k/s"),
            new HeroGalleryItem(
                "amber-engine",
                "Amber Render",
                "Graphics",
                "Hardware-accelerated pipeline",
                "Captures source and destination visuals and cross-fades them seamlessly during flight so typography and layouts never pop.",
                "#B45309",
                "#F59E0B",
                "AR",
                "Frame Time",
                "4.2 ms"),
            new HeroGalleryItem(
                "crimson-guard",
                "Crimson Shield",
                "Security",
                "Zero-trust policy engine",
                "Interrupting a flight mid-air smoothly redirects the shuttle from its current interpolated bounds back to the target.",
                "#BE123C",
                "#FB7185",
                "CS",
                "Policies",
                "128"),
            new HeroGalleryItem(
                "violet-synth",
                "Violet Synth",
                "Audio",
                "Spatial DSP workstation",
                "Works with any Avalonia content inside <aviora:Hero>, including images, cards, badges, and custom templates.",
                "#6D28D9",
                "#A78BFA",
                "VS",
                "Channels",
                "64"),
            new HeroGalleryItem(
                "slate-atlas",
                "Slate Atlas",
                "Navigation",
                "Cross-view route transitions",
                "Wrap any page, TabControl, or Window root in <aviora:HeroHost> and pair elements by matching Tag values.",
                "#334155",
                "#64748B",
                "SA",
                "Routes",
                "19"),
        ];

        FullscreenItems =
        [
            new HeroGalleryItem(
                "fs-nebula",
                "Nebula Horizon",
                "Fullscreen Showcase",
                "Window-wide HeroOverlay transition",
                "Expands across the entire application window over a fading backdrop. Press Escape, click the backdrop, or click Close to fly back.",
                "#0F766E",
                "#2DD4BF",
                "NH",
                "Resolution",
                "4K"),
            new HeroGalleryItem(
                "fs-solaris",
                "Solaris Peak",
                "Fullscreen Showcase",
                "Smooth backdrop & shuttle exit",
                "During dismissal, the fullscreen Hero starts flying immediately while the backdrop and metadata fade out smoothly underneath.",
                "#9A3412",
                "#FB923C",
                "SP",
                "Luminance",
                "980 nit"),
            new HeroGalleryItem(
                "fs-cobalt",
                "Cobalt Wave",
                "Fullscreen Showcase",
                "Corner radius & scale morph",
                "Combines HeroHost flight with HeroOverlay enter/exit transitions so both opening and closing feel natural.",
                "#1E3A8A",
                "#60A5FA",
                "CW",
                "Refresh",
                "120 Hz"),
        ];

        _activeGalleryView = new HeroGalleryStateViewModel(this);
        _activeProfileView = new HeroCompactProfileStateViewModel(this);
    }

    public IReadOnlyList<HeroGalleryItem> Items { get; }

    public IReadOnlyList<HeroGalleryItem> FullscreenItems { get; }

    public IReadOnlyList<HeroFlightMode> FlightModes { get; } =
    [
        HeroFlightMode.Arc,
        HeroFlightMode.Linear,
    ];

    [ObservableProperty]
    private object _activeGalleryView;

    [ObservableProperty]
    private object _activeProfileView;

    public TimeSpan FlightDuration => TimeSpan.FromMilliseconds(DurationMs);

    public string DurationLabel => $"{DurationMs:0} ms";

    partial void OnDurationMsChanged(double value)
    {
        OnPropertyChanged(nameof(FlightDuration));
        OnPropertyChanged(nameof(DurationLabel));
    }

    partial void OnSelectedItemChanged(HeroGalleryItem? value)
    {
        ActiveGalleryView = value is null
            ? new HeroGalleryStateViewModel(this)
            : new HeroDetailStateViewModel(this, value);
    }

    partial void OnIsProfileExpandedChanged(bool value)
    {
        ActiveProfileView = value
            ? new HeroExpandedProfileStateViewModel(this)
            : new HeroCompactProfileStateViewModel(this);
    }

    [RelayCommand]
    private void SelectItem(HeroGalleryItem item)
    {
        SelectedItem = item;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedItem = null;
    }

    [RelayCommand]
    private void OpenFullscreen(HeroGalleryItem item)
    {
        FullscreenItem = item;
        IsFullscreenOpen = true;
    }

    [RelayCommand]
    private void CloseFullscreen()
    {
        IsFullscreenOpen = false;
    }

    [RelayCommand]
    private void ToggleProfile()
    {
        IsProfileExpanded = !IsProfileExpanded;
    }
}

public sealed record HeroGalleryStateViewModel(HeroPageViewModel Owner);

public sealed record HeroDetailStateViewModel(HeroPageViewModel Owner, HeroGalleryItem Item);

public sealed record HeroCompactProfileStateViewModel(HeroPageViewModel Owner);

public sealed record HeroExpandedProfileStateViewModel(HeroPageViewModel Owner);

public sealed record HeroGalleryItem(
    string Id,
    string Title,
    string Category,
    string Subtitle,
    string Description,
    string AccentHex,
    string SecondaryHex,
    string BadgeText,
    string MetricLabel,
    string MetricValue);
