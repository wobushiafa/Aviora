using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aviora.Demo.ViewModels;

public sealed partial class BackdropBlurPageViewModel() : DemoPageViewModel("BackdropBlur")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewCornerRadius))]
    private double _cornerRadius = 24;

    [ObservableProperty]
    private double _renderScale = 1;

    public double[] RenderScales { get; } = [1, 0.75, 0.5];

    public CornerRadius PreviewCornerRadius => new(CornerRadius);
}
