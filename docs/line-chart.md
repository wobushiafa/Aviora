# LineChart

`LineChart` renders one or more trends with shared axes, animation, selection,
keyboard, interactive legends, crosshairs, zoom & pan, downsampling, and tooltip
infrastructure. Existing `ItemsSource` and `Values` bindings remain available for
a single series, alongside POCO `DataSource` selectors.

## Multiple series

Bind `Series` to a collection of `LineChartSeries` objects. Each series can use
rich `ItemsSource` data or simple numeric `Values`, and can define its own line,
area, and point brushes:

```csharp
public IReadOnlyList<LineChartSeries> Comparison { get; } =
[
    new()
    {
        Title = "Revenue",
        ItemsSource = RevenueTrend,
        LineBrush = Brushes.DodgerBlue,
        AreaFillBrush = new SolidColorBrush(Color.Parse("#402563EB")),
        PointBrush = Brushes.RoyalBlue,
    },
    new()
    {
        Title = "Cost",
        ItemsSource = CostTrend,
        LineBrush = Brushes.IndianRed,
        AreaFillBrush = new SolidColorBrush(Color.Parse("#40CD5C5C")),
        PointBrush = Brushes.Firebrick,
    },
];
```

```xml
<controls:LineChart Series="{Binding Comparison}"
                    AutoRange="True"
                    InterpolationMode="Smooth"
                    ShowPoints="True" />
```

All series share the category positions and Y-axis range. When `Series` is not
empty it takes precedence over `ItemsSource` and `Values`.

## Single series

```xml
<controls:LineChart ItemsSource="{Binding RevenueTrend}"
                    AutoRange="True"
                    LineBrush="#2563EB"
                    LineThickness="2"
                    InterpolationMode="Smooth"
                    ShowPoints="True"
                    PointRadius="4"
                    AreaFillBrush="#202563EB" />
```

`InterpolationMode="Linear"` draws straight segments. `Smooth` uses a
Catmull-Rom-derived cubic curve with configurable `LineTension`, `MonotoneSmooth`
uses Fritsch-Carlson monotone splines to eliminate overshoot on step transitions,
and `StepBefore` / `StepAfter` / `StepMiddle` draw step charts.

Each `LineChartSeries` inherits `AvaloniaObject` and supports per-series runtime
toggling (`IsVisible`), `LineThickness`, `StrokeDashArray`, `InterpolationMode`,
`ShowPoints`, `PointRadius`, and `PointShape` (`Circle`, `Square`, `Diamond`,
`Triangle`).

`PointBrush` controls the default point color. A data point's `Brush` takes
precedence, and threshold colors are used for points when `ShowThresholds` is
enabled. `SelectedPointBrush` and `SelectedPointRadius` are optional; selection
is visually unchanged by default, matching `ColumnChart`. Hover emphasis can be
configured via `HoverPointRadius` and `HoverHaloBrush`.

Set `AreaFillBrush` to fill the region between the line and the zero baseline.
Shared axes (`AutoRangeIncludeZero`, `ShowAxisLines`, `ShowZeroBaseline`,
`ShowVerticalGridLines`, `PlotPadding`, `PlotBackground`), crosshairs
(`CrosshairMode`), interactive legends (`ShowLegend`), zoom & pan (`IsZoomEnabled`,
`IsPanEnabled`, `ResetZoom()`), and large-dataset downsampling (`DownsampleMode`,
`DownsampleThreshold`) are inherited from `CartesianChart`.

See [Chart tooltips](chart-tooltips.md) for styling properties, `SharedCategory`
aggregation, and custom `DataTemplate` content.
