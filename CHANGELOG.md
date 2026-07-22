# Changelog

All notable changes to this project will be documented in this file. The format
is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
