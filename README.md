# Aviora

Aviora is an open-source control library for building modern, cross-platform
applications with [Avalonia](https://avaloniaui.net/).

> The project is in its initial development stage. APIs may change before the
> first stable release.

## Repository structure

```text
aviora/
|-- src/Aviora.Core/              Framework-independent contracts and algorithms
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
`Aviora.Core` transitively. Then load the control themes in `App.axaml`:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="avares://Aviora.Controls/Themes/Generic.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### ColumnChart

Bind a simple numeric sequence with `Values`, or use `ItemsSource` when each
column needs its own key, label, brush, or Tooltip. Business models can
implement `IChartDataPoint` directly; `ChartDataPoint` is provided for common
cases.

```xml
<controls:ColumnChart ItemsSource="{Binding Sales}"
                      AutoRange="True"
                      Thresholds="{Binding SalesThresholds}"
                      ShowThresholds="True" />
```

See the [control documentation](docs/column-chart.md) and the demo application
for complete examples.

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before
opening an issue or pull request.

## License

Aviora is licensed under the [MIT License](LICENSE).
