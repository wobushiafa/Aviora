# Changelog

All notable changes to this project will be documented in this file. The format
is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.3] - 2026-11-19

### Fixed

- Fixed a one-frame flash before `Hero` flights by pre-hiding arriving matched heroes and launching flights synchronously upon first layout pass before rendering.
- Fixed hidden `Loading` inside closed `LoadingOverlay` (and hidden `ProgressRing`) running background `RequestAnimationFrame` loops while invisible.

### Changed

- Replaced single-cell overlay `Grid` containers in control templates (`LoadingOverlay`, `Dialog`, `ToastHost`, `HeroHost`, `HeroTransitionControl`, `HeroOverlay`) with lightweight `Panel`.
- Reduced per-frame rendering allocations in `Loading`, `ProgressRing`, and `DialGauge` by using `StreamGeometry` arcs and reusing cached `Pen` instances.

## [0.6.2] - 2026-11-19

### Added

- Added `HeroShuttleMode` (`Snapshot` and `Direct`) on `HeroHost` and `Hero` to support zero-snapshot, pure `CompositionVisual` (`Translation` + `Scale`) transitions for live video and fullscreen surfaces.
- Added `HeroHost.MaxSnapshotSize` (default `1280` px) to cap longest-edge snapshot rasterization dimensions on high-DPI and fullscreen views.

### Fixed

- Fixed `HeroHost.StartFlight` and `HeroHost.NotifyHeroDeparting` unconditionally calling `CaptureSnapshot` (`RenderTargetBitmap.Render`) before checking `ShuttleTemplate` or `ShuttleMode`.

## [0.6.1] - 2026-11-18

### Changed

- Moved `HeroHost`, `HeroOverlay`, and `HeroTransitionControl` animations onto Avalonia `CompositionVisual` keyframe animations (`Translation`, `Scale`, and `Opacity`) so hero flights run off the UI thread.
- Avoided per-frame `CornerRadius` clip geometry rebuilds on large/fullscreen hero shuttles and capped snapshot rasterization scale at `1.0x` for large surfaces to improve smoothness on low-spec devices.

## [0.6.0] - 2026-11-16

### Added

- Added `HeroHost` and `Hero` for Flutter-style shared-element transitions with `Arc` and `Linear` flight modes, corner-radius interpolation, mid-flight interruption handling, custom `ShuttleTemplate` support, and `RenderTargetBitmap` cross-fade shuttles.
- Added `HeroTransitionControl` and `HeroOverlay` for smooth content cross-fades and fullscreen modal enter/exit transitions coordinated with `HeroHost`.
- Added an interactive Hero demo page in `Aviora.Demo` showcasing fullscreen enter/exit, gallery-to-detail transitions, and circular-to-card morphing, plus `HeroTests` regression coverage.

### Changed

- Updated `LineChart` demo in `Aviora.Demo` to support incremental random point additions via `ObservableCollection`.

## [0.5.0] - 2026-10-08

### Added

- Added `BackdropBlur` for blurring previously rendered page content beneath a tinted surface, with DIP blur radius, asymmetric corners, and standard border styling.
- Added configurable `RenderScale` (0.25–1, default 1), an interactive Demo page, rendering regression tests, and a desktop GPU performance probe.
- Added BackdropBlur usage documentation and measured performance reports.

### Changed

- Optimized backdrop rendering with native Skia backdrop layers, visible-region sampling with blur halos, reference-counted render resources, and work texture capacity reuse during size changes.
- Standardized overlay animation APIs and dialog service options, including compatibility aliases for previous API names.
- Improved dialog accessibility metadata, initial/restored focus handling, and cancellable closing behavior.

## [0.3.2] - 2026-07-27

### Changed

- Centered Y-axis labels using rendered glyph metrics so alignment remains correct across font sizes.
- Disabled the default selected-point visual for `LineChart`; applications can still opt in with `SelectedPointBrush` or `SelectedPointRadius`.

## [0.3.1] - 2026-07-22

### Added

- Added per-series `AreaFillBrush` support for multi-series `LineChart` charts.
- Added opt-in single-layer navigation and visually stacked Dialog presentations that restore the parent after the topmost Dialog closes.

## [0.3.0] - 2026-07-22

### Added

- Added `ProgressRing` with determinate and indeterminate ring rendering.
- Added Wave, Orbit, and DoubleRing loading indicator styles.
- Added multi-series `LineChart` data, styling, documentation, tests, and demo
  examples.

### Changed

- Added a configurable 250 ms Tooltip hide grace period and reused unchanged
  Tooltip content to avoid redundant layout passes.
- Reworked LineChart hit testing to use cached projected points, zero-allocation
  squared-distance checks, and per-series X-coordinate lookup.
- Cached line segments, static geometries, pens, and X-axis label layouts to
  reduce render time and managed allocations.

## [0.2.0] - 2026-07-21

### Added

- Added a queued, multi-host `Dialog` presentation service with asynchronous
  results, sessions, cancellation, and customizable content.
- Added `Loading` with Ring, Dots, Pulse, and Bars indicators, theme-aware
  appearance properties, and fully custom content support.
- Added a global `LoadingOverlay`, framework-independent `ILoadingService`,
  disposable loading sessions, exception-safe `RunAsync` helpers, concurrent
  operation handling, multi-host routing, and customizable loading content.
- Added configurable loading show delay, minimum visible duration, and delayed
  close behavior with automatic cancellation when a new operation begins.
- Initial solution structure, control library, demo application, and tests.
- Initial `AvioraCard` control and Fluent theme integration.
- Added `ColumnChart`, reusable chart data contracts, documentation, tests, and
  rich/simple binding examples in the demo gallery.
- Added ordered and reverse threshold evaluation, configurable update throttling,
  usable axis defaults, and external axis text styling examples.
- Added threshold labels at their Y-axis positions, full-height per-column
  background tracks, and customizable selected-column fill, overlay, and stroke
  effects.
- Selection remains functional but has no visual effect until selection styling
  is explicitly configured.
