# Project architecture

Aviora separates reusable algorithms, presentation contracts, and
Avalonia-specific implementations. Dependencies point inward toward the two
framework-independent projects:

```text
Aviora.Demo
    |
    v
Aviora.Controls -------------------+
    |                              |
    v                              v
Aviora.Core        Aviora.Presentation.Abstractions
```

## Repository structure

```text
assets/                     Shared brand and NuGet package assets
docs/                       Architecture notes, control docs, and performance reports
samples/
`-- Aviora.Demo/            Avalonia demo application showcasing controls and services
src/
|-- Aviora.Core/
|   |-- Charts/             Framework-independent data contracts and algorithms
|   `-- Compatibility/      Compiler compatibility polyfills for netstandard2.0
|-- Aviora.Presentation.Abstractions/
|   |-- Compatibility/      Compiler compatibility polyfills for netstandard2.0
|   |-- Dialogs/            UI-framework-independent dialog contracts
|   |-- Drawers/            UI-framework-independent drawer contracts
|   |-- Loadings/           UI-framework-independent loading scopes
|   `-- Toasts/             UI-framework-independent toast notification contracts
`-- Aviora.Controls/
    |-- Assists/            Attached-property helpers for native controls (SmoothScrollAssist)
    |-- Charts/             Public Avalonia chart API and compatibility models
    |   `-- Internal/       Observation, throttling, animation, layout, and rendering
    |-- Dialogs/            Dialog control and Avalonia event contracts
    |   `-- Services/       Queued dialog service and host coordination
    |-- Drawers/            Drawer control and Avalonia event contracts
    |   `-- Services/       Drawer service and host coordination
    |-- Extensions/         XAML markup extensions (Aviora.Controls.Extensions)
    |-- Gauges/             Gauge controls (DialGauge, AngleGauge, GaugeBase)
    |-- Heroes/             Shared-element transition host, overlay, and controls
    |-- Internal/           Shared internal helpers and palette resources
    |-- Loadings/           Loading indicators, overlays, and host services
    |   `-- Services/       Avalonia service implementation and host coordination
    |-- Progress/           Progress indicator controls (ProgressRing)
    |-- Surfaces/           General-purpose surface controls (AvioraCard, BackdropBlur)
    |-- Themes/             Shared Avalonia resources and control themes
    |-- Thermometers/       Thermometer control and related public types
    `-- Toasts/             Toast notification controls and host
        `-- Services/       Toast service and host coordination
tests/
|-- Aviora.Core.Tests/      Unit tests aligned with Aviora.Core modules
`-- Aviora.Controls.Tests/  Headless Avalonia tests aligned with Aviora.Controls modules
tools/
`-- BackdropBlur.Perf/      Standalone GPU render probe and report generator
```

## Aviora.Core boundary

`Aviora.Core` targets `netstandard2.0` and has no Avalonia package reference. It owns
public data contracts and deterministic algorithms that can be used by multiple
controls or by non-Avalonia applications.

A type belongs in Core only when it meets at least one of these conditions:

- It is a framework-independent public contract.
- It is deterministic logic that is reusable outside one renderer.
- It represents state or behavior shared by multiple control families.

Core must not contain brushes, controls, styled properties, drawing contexts,
pointer events, formatted text, or other UI-framework concepts. New functional
areas use focused namespaces such as `Aviora.Core.Charts`; broad folders such as
`Common`, `Helpers`, and `Models` are avoided.

The initial chart domain contains:

- `IChartPoint` and `ChartPoint` for value, key, label, and descriptive text.
- `ThresholdRule`, `ThresholdDirection`, and `ThresholdResolver` for semantic
  value boundaries.
- `ChartAxisScale` and `ChartAxisCalculator` for numeric range and tick
  calculation.

## Aviora.Presentation.Abstractions boundary

`Aviora.Presentation.Abstractions` targets `netstandard2.0` and references neither
Avalonia nor `Aviora.Controls`. It contains contracts that application and
ViewModel projects can consume without depending on a concrete UI framework.

The initial drawer presentation contract contains:

- `IDrawerService` for showing and closing drawer presentations.
- `IDrawerSession` for controlling one presentation without knowing its host.
- `DrawerRequest` and `DrawerResult` for request/response data.
- `DrawerPlacement`, `DrawerDisplayMode`, and `DrawerCloseReason` for portable
  presentation semantics.
- `DrawerHost` for well-known host identifiers.

The dialog presentation contract follows the same dependency direction:

- `IDialogService` for ViewModel-facing presentation requests.
- `IDialogSession` for closing one presentation without knowing its host.
- `DialogRequest` and `DialogResult` for request/response data.
- `DialogCloseReason` and `DialogHost` for portable close and routing semantics.

Loading presentation contracts follow the same dependency direction:

- `ILoadingService` creates independently disposable loading scopes.
- `ILoadingSession` closes one scope without exposing the Avalonia host.
- `LoadingRequest` carries host-neutral content, routing, and metadata.
- `LoadingOverlay` and `LoadingService` coordinate concurrent scopes in the Avalonia package.

Presentation requests may contain behavioral options such as placement,
dismissal, size, and animation timing. Avalonia-specific templates, brushes,
corner radii, shadows, controls, and Dispatcher behavior must not enter this
project. Those remain host or theme configuration in `Aviora.Controls`.

## Aviora.Controls boundary

`Aviora.Controls` references Core and Presentation Abstractions. It owns all
Avalonia-specific API, service implementations, styling, rendering,
interaction, themes, and lifecycle behavior. Existing chart types remain
source-compatible:

Drawer and Dialog hosting use separate host-coordination contracts in this
layer. Their default services implement both the framework-independent client
contract and the Avalonia host contract, while ViewModels depend only on
`IDrawerService` or `IDialogService`.

Control families and attached-property helpers live in focused plural folders (`Assists`,
`Charts`, `Dialogs`, `Drawers`, `Gauges`, `Heroes`, `Loadings`, `Progress`,
`Surfaces`, `Thermometers`, `Toasts`, etc.). Folder placement does not force a
public namespace change; public controls and attached helpers share the unified
`Aviora.Controls` namespace so existing XAML and C# callers remain stable (only
XAML markup extensions use `Aviora.Controls.Extensions`).

`CartesianChart` is the shared Avalonia base for single-series Cartesian charts.
It owns the common StyledProperties and coordinates data observation, update
throttling, animation, selection, keyboard interaction, and tooltip lifecycle.
Concrete charts only add series-specific properties and rendering.

- `IChartDataPoint` extends Core's `IChartPoint` with per-column brushes.
- `ChartDataPoint` extends Core's `ChartPoint` and keeps its original public
  properties and namespace.
- `ChartThreshold` extends Core's `ThresholdRule` with threshold and label
  brushes.
- The Controls `ThresholdDirection` remains available for existing XAML and is
  mapped to the Core direction before threshold resolution.

`ColumnChart` keeps the Avalonia-facing API and lifecycle in the control class.
Its runtime responsibilities remain separated into focused internal components:

- `ChartDataPipeline` converts `Values` and `ItemsSource` into one data shape.
- `ChartDataObserver` watches collection and item changes, while
  `ChartUpdateScheduler` applies the configured throttle.
- `ChartAnimationController` owns interpolated values.
- `ChartLayoutCalculator` and `ChartSelectionState` contain UI-oriented layout
  and keyboard behavior.
- `ChartToolTipPresenter` owns the chart's single tooltip visual, its content,
  styling, stable anchor, and bounds-constrained layout.
- Series renderers own drawing, text measurement, hit testing, and selection
  visuals.

The renderer snapshot is invalidated when chart properties or source data
change. Animation frames reuse the snapshot and only update animated values.

## Future growth

New control families continue to use `Aviora.Core` only for genuinely reusable,
framework-independent contracts and algorithms. For example, input controls may
add validation contracts, list controls may add paging state, and navigation
controls may add hierarchy models. Avalonia adapters and visual styles remain in
`Aviora.Controls` even when their underlying state lives in Core.

New ViewModel-facing presentation services such as dialogs, notifications,
flyouts, and navigation put their framework-independent contracts in
`Aviora.Presentation.Abstractions`. Their Avalonia hosts and implementations
remain feature-local in `Aviora.Controls`.
