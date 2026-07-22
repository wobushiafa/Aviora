# LineChart

`LineChart` renders one or more trends with shared axes, animation, selection,
keyboard, and tooltip infrastructure. Existing `ItemsSource` and `Values`
bindings remain available for a single series.

## Multiple series

Bind `Series` to a collection of `LineChartSeries` objects. Each series can use
rich `ItemsSource` data or simple numeric `Values`, and can define its own line
and point brushes:

```csharp
public IReadOnlyList<LineChartSeries> Comparison { get; } =
[
    new()
    {
        Title = "Revenue",
        ItemsSource = RevenueTrend,
        LineBrush = Brushes.DodgerBlue,
        PointBrush = Brushes.RoyalBlue,
    },
    new()
    {
        Title = "Cost",
        ItemsSource = CostTrend,
        LineBrush = Brushes.IndianRed,
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
