<p align="center">
  <img src="https://raw.githubusercontent.com/wobushiafa/Aviora/main/assets/aviora.svg" width="168" height="168" alt="Aviora logo" />
</p>

# Aviora

[简体中文](https://github.com/wobushiafa/Aviora/blob/main/README.md) | English

Aviora is a modern, cross-platform open-source control library for [Avalonia](https://avaloniaui.net/), with charts, gauges, loading indicators, drawers, dialogs, and general-purpose surfaces.

> The project is in its early development stage. Public APIs may change before the first stable release.

## Controls

- `AvioraCard`: a themed surface for grouping related content.
- `ColumnChart`: columns with thresholds, axes, selection, animation, and tooltips.
- `LineChart`: lines with smooth interpolation, area fills, points, and interaction.
- `Thermometer`: ranges, ticks, labels, gradient mapping, and transitions.
- `DialGauge`: range-colored ticks, labels, needles, and transitions.
- `ProgressRing`: circular determinate progress and an animated indeterminate state.
- `Loading`: Ring, Dots, Pulse, Bars, Wave, Orbit, and DoubleRing indicators with support for custom content.
- `LoadingOverlay`: a global loading mask with asynchronous scopes, concurrent requests, host routing, and an MVVM service.
- `Drawer`: multiple placements, overlay and push modes, dismissal, and an asynchronous presentation service.
- `Dialog`: modal custom content with asynchronous results, presentation sessions, request queues, and multi-host routing.

## Installation

```powershell
dotnet add package Aviora.Controls
```

Reference the framework-independent chart contracts and algorithms separately when needed:

```powershell
dotnet add package Aviora.Core
```

Reference only the ViewModel-facing presentation contracts when needed:

```powershell
dotnet add package Aviora.Presentation.Abstractions
```

## Setup

Load the control themes in `App.axaml`:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="avares://Aviora.Controls/Themes/Generic.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

Declare the Aviora XML namespace in each AXAML view that uses the controls:

```xml
xmlns:aviora="https://github.com/wobushiafa/Aviora"
```

## Usage

### AvioraCard

```xml
<aviora:AvioraCard Padding="20">
  <StackPanel Spacing="8">
    <TextBlock FontSize="18" FontWeight="SemiBold" Text="System status" />
    <TextBlock Opacity="0.65" Text="All services are operational" />
  </StackPanel>
</aviora:AvioraCard>
```

### ColumnChart

```xml
<aviora:ColumnChart Height="280"
                    ItemsSource="{Binding Sales}"
                    AutoRange="True"
                    XAxisLabelMode="All"
                    IsToolTipEnabled="True"
                    SelectedItem="{Binding SelectedSale}" />
```

```csharp
public IReadOnlyList<IChartDataPoint> Sales { get; } =
[
    new ChartDataPoint { Label = "Jan", Value = 42, ToolTip = "January: 42" },
    new ChartDataPoint { Label = "Feb", Value = 56, ToolTip = "February: 56" },
    new ChartDataPoint { Label = "Mar", Value = 71, ToolTip = "March: 71" },
];
```

### LineChart

```xml
<aviora:LineChart Height="280"
                  Values="{Binding TrendValues}"
                  XAxisLabelsSource="{Binding TrendLabels}"
                  InterpolationMode="Smooth"
                  ShowPoints="True"
                  AreaFillBrush="#332563EB"
                  IsAnimationEnabled="True" />
```

### Thermometer

```xml
<aviora:Thermometer Width="120"
                    Height="300"
                    Minimum="-20"
                    Maximum="120"
                    Value="{Binding Temperature}"
                    TickCount="7"
                    ShowTickLabels="True"
                    TickLabelFormat="0"
                    LiquidBrushMappingMode="FullRange" />
```

### DialGauge

```xml
<aviora:DialGauge Width="280"
                  Height="220"
                  Minimum="0"
                  Maximum="100"
                  Value="{Binding Usage}"
                  TickCount="11"
                  ShowTickLabels="True"
                  TickColorMode="Range" />
```

### Loading

The circular progress bar supports determinate values and an indeterminate animation matching the Loading Ring:

```xml
<aviora:ProgressRing Width="52"
                     Height="52"
                     Value="65"
                     StrokeThickness="5" />

<aviora:ProgressRing IsIndeterminate="True" />
```

Select a built-in style and optionally configure its brush, size, thickness, and cycle duration:

```xml
<aviora:Loading Width="44"
                Height="44"
                IndicatorStyle="Dots"
                IndicatorBrush="#0F766E"
                StrokeThickness="4"
                AnimationDuration="0:0:0.7" />
```

Setting `Content`, optionally with a `ContentTemplate`, replaces the built-in renderer, so any Avalonia control can be used:

```xml
<aviora:Loading Width="180" Height="6">
  <ProgressBar IsIndeterminate="True" />
</aviora:Loading>
```

Set `IsActive="False"` to stop the animation and hide the indicator.

#### Global loading overlay

Place `LoadingOverlay` at the outermost window layer. It blocks input through the mask and covers nested pages, drawers, and dialogs:

```xml
<aviora:LoadingOverlay x:Name="LoadingHost"
                       ShowDelay="0:0:0.1"
                       CloseDelay="0:0:0.2"
                       MinimumShowDuration="0:0:0.35">
  <!-- Main page content -->
</aviora:LoadingOverlay>
```

Create the service at the composition root, connect the host, and inject `ILoadingService` into the ViewModel:

```csharp
var loadingService = new LoadingService();
LoadingHost.Service = loadingService;
DataContext = new MainViewModel(loadingService);
```

The ViewModel only depends on `Aviora.Presentation.Loadings`. `RunAsync` closes its loading session after success, failure, or cancellation:

```csharp
public sealed class MainViewModel(ILoadingService loadingService)
{
    public Task RefreshAsync(CancellationToken cancellationToken) =>
        loadingService.RunAsync(
            token => RefreshDataAsync(token),
            new LoadingRequest("Refreshing data"),
            cancellationToken);
}
```

Use a manual scope when loading spans multiple statements:

```csharp
using ILoadingSession loading = loadingService.Show(
    new LoadingRequest("Synchronizing workspace"));
await SynchronizeAsync();
```

Sessions may overlap. Closing one session removes only its request, and the overlay remains until the final session closes. Use `HostId` for multi-window routing and `LoadingContentTemplate` for message ViewModels. `ShowDelay` filters short operations, `MinimumShowDuration` guarantees a minimum visible period once opened, and `CloseDelay` keeps the overlay visible briefly after the final operation. A new request automatically invalidates a pending delayed close.

### Drawer

Place a Drawer host in the view:

```xml
<aviora:Drawer x:Name="DrawerHost"
               Placement="Right"
               DrawerSize="380">
  <Grid>
    <!-- Main page content -->
  </Grid>
</aviora:Drawer>
```

When `DrawerSize` is not set, Drawer measures the content's desired size in the presentation direction: width for left/right and height for top/bottom. Set a numeric value to keep a fixed size.

Create the Avalonia implementation at the application composition root, pass the host interface to the View, and inject the client interface into the ViewModel:

```csharp
var drawerService = new DrawerService();
DrawerHost.Service = drawerService;
DataContext = new MainWindowViewModel(drawerService);
```

The ViewModel depends only on `Aviora.Presentation.Drawers`:

```csharp
using Aviora.Presentation.Drawers;

public sealed class SettingsViewModel(IDrawerService drawerService)
{
    public Task<DrawerResult> OpenAsync() =>
        drawerService.ShowAsync(new DrawerRequest(this)
        {
            Placement = DrawerPlacement.Right,
            Size = 380,
        });
}
```

### Dialog

Place a Dialog host at the outermost window layer and connect its service at the composition root:

```xml
<aviora:Dialog x:Name="DialogHost">
  <!-- Main page content -->
</aviora:Dialog>
```

```csharp
var dialogService = new DialogService(new DialogOptions
{
    IsAnimationEnabled = true,
    IsEscapeKeyEnabled = false,
    IsLightDismissEnabled = false,
    IsOverlayVisible = true,
});
DialogHost.Service = dialogService;
DataContext = new MainWindowViewModel(dialogService);
```

These are also the built-in defaults. An individual `DialogRequest` can still override them when needed.

A ViewModel can present content directly. Use a session factory when the content must close itself and return a result:

```csharp
await dialogService.ShowAsync(new MessageViewModel());

DialogResult result = await dialogService.ShowAsync(
    session => new EditProfileViewModel(session));
```

Use `Navigate` to show only the current level, or `Stack` to render a child Dialog visually above its parent. Both restore the parent after the child closes; requests use the existing queue behavior by default:

```csharp
DialogResult childResult = await dialogService.ShowAsync(new DialogRequest(childViewModel)
{
    PresentationMode = DialogPresentationMode.Stack, // or Navigate
});
```

## Build from source

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project samples/Aviora.Demo
```

See the [documentation index](https://github.com/wobushiafa/Aviora/tree/main/docs) and the demo application for more examples.

## Contributing

Read [CONTRIBUTING.md](https://github.com/wobushiafa/Aviora/blob/main/CONTRIBUTING.md) before opening an issue or pull request.

## License

Aviora is licensed under the [MIT License](https://github.com/wobushiafa/Aviora/blob/main/LICENSE).
