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
src/
|-- Aviora.Core/
|   `-- Charts/             Framework-independent data contracts and algorithms
|-- Aviora.Presentation.Abstractions/
|   `-- Drawers/            UI-framework-independent presentation contracts
`-- Aviora.Controls/
    |-- Charts/             Public Avalonia chart API and compatibility models
    |   `-- Internal/      observation, throttling, animation, layout, and rendering
    |-- Drawers/            Drawer control and Avalonia event contracts
    |   `-- Services/       Avalonia service implementation and host coordination
    |-- Thermometers/       Thermometer control and related public types
    |-- Themes/             Shared Avalonia resources and control themes
    `-- AvioraCard.cs      General-purpose Avalonia controls
```

## Aviora.Core boundary

`Aviora.Core` targets `net8.0` and has no Avalonia package reference. It owns
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

`Aviora.Presentation.Abstractions` targets `net8.0` and references neither
Avalonia nor `Aviora.Controls`. It contains contracts that application and
ViewModel projects can consume without depending on a concrete UI framework.

The initial drawer presentation contract contains:

- `IDrawerService` for showing and closing drawer presentations.
- `DrawerRequest` and `DrawerResult` for request/response data.
- `DrawerPlacement`, `DrawerDisplayMode`, and `DrawerCloseReason` for portable
  presentation semantics.
- `DrawerHost` for well-known host identifiers.

Presentation requests may contain behavioral options such as placement,
dismissal, size, and animation timing. Avalonia-specific templates, brushes,
corner radii, shadows, controls, and Dispatcher behavior must not enter this
project. Those remain host or theme configuration in `Aviora.Controls`.

## Aviora.Controls boundary

`Aviora.Controls` references Core and Presentation Abstractions. It owns all
Avalonia-specific API, service implementations, styling, rendering,
interaction, themes, and lifecycle behavior. Existing chart types remain
source-compatible:

Control families live in focused plural folders (`Charts`, `Drawers`, and
`Thermometers`). Folder placement does not force a public namespace change;
existing public namespaces remain stable when files are reorganized.

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
