# LineChart

`LineChart` renders a single-series trend with the same data, axis, threshold,
animation, selection, keyboard, and tooltip infrastructure as `ColumnChart`.

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
Catmull-Rom-derived cubic curve while preserving the original data points for
hit testing and selection.

`PointBrush` controls the default point color. A data point's `Brush` takes
precedence, and threshold colors are used for points when `ShowThresholds` is
enabled. `SelectedPointBrush` and `SelectedPointRadius` are optional; selection
is visually unchanged by default, matching `ColumnChart`.

Set `AreaFillBrush` to fill the region between the line and the zero baseline.
Leave it unset to draw only the line and points. All shared axis and update
properties are inherited from `CartesianChart`.

See [Chart tooltips](chart-tooltips.md) for styling properties and custom
`DataTemplate` content.
