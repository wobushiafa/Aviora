<p align="center">
  <img src="assets/aviora.svg" width="168" height="168" alt="Aviora logo" />
</p>

# Aviora

Aviora is an open-source control library for building modern, cross-platform
applications with [Avalonia](https://avaloniaui.net/).

> The project is in its initial development stage. APIs may change before the
> first stable release.

## Repository structure

```text
aviora/
|-- src/Aviora.Core/              Framework-independent contracts and algorithms
|-- src/Aviora.Presentation.Abstractions/
|                                 Framework-independent presentation contracts
|-- src/Aviora.Controls/          Avalonia controls, rendering, and themes
|-- samples/Aviora.Demo/          Interactive control gallery
|-- tests/Aviora.Core.Tests/      Core unit tests
|-- tests/Aviora.Controls.Tests/  Automated tests
|-- docs/                         Design and usage documentation
`-- Aviora.sln                   Solution entry point
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or newer

The library targets `net8.0`, while the .NET 10 SDK is used to run Avalonia
12.1 source generators during development.

## Build

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Run the control gallery:

```powershell
dotnet run --project samples/Aviora.Demo
```

## Use the library from source

Reference `src/Aviora.Controls/Aviora.Controls.csproj`. It brings in
`Aviora.Core` and `Aviora.Presentation.Abstractions` transitively. ViewModel-only
projects can reference `Aviora.Presentation.Abstractions` directly. Then load
the control themes in `App.axaml`:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="avares://Aviora.Controls/Themes/Generic.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### Charts

Bind a simple numeric sequence with `Values`, or use `ItemsSource` when each
point needs its own key, label, brush, or Tooltip. Business models can implement
`IChartDataPoint` directly; `ChartDataPoint` is provided for common cases.

```xml
<controls:ColumnChart ItemsSource="{Binding Sales}"
                      AutoRange="True"
                      Thresholds="{Binding SalesThresholds}"
                      ShowThresholds="True" />
```

See the [control documentation](docs/column-chart.md),
[LineChart documentation](docs/line-chart.md),
[Tooltip documentation](docs/chart-tooltips.md), and the demo application for
complete examples.

### Presentation services

ViewModels consume presentation contracts without referencing Avalonia or the
Controls namespace:

```csharp
using Aviora.Presentation.Drawers;

public sealed class SettingsViewModel(IDrawerService drawerService)
{
    public Task<DrawerResult> OpenAsync() =>
        drawerService.ShowAsync(new DrawerRequest(this));
}
```

The application composition root creates the Avalonia implementation from
`Aviora.Controls`, and a `Drawer` host binds to the same service instance.

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before
opening an issue or pull request.

## License

Aviora is licensed under the [MIT License](LICENSE).
