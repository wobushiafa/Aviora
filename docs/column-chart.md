# ColumnChart

`ColumnChart` renders an interactive, single-series column chart. It supports
automatic or fixed ranges, positive and negative values, category labels,
threshold colors, selection, commands, tooltips, and animated updates.

## Rich data points

Use `ItemsSource` with the built-in `ChartDataPoint` type:

```csharp
public IReadOnlyList<IChartDataPoint> Sales { get; } =
[
    new ChartDataPoint { Key = "jan", Label = "Jan", Value = 42 },
    new ChartDataPoint { Key = "feb", Label = "Feb", Value = 56 },
];
```

```xml
<controls:ColumnChart ItemsSource="{Binding Sales}"
                      AutoRange="True"
                      Thresholds="{Binding SalesThresholds}"
                      ShowThresholds="True" />
```

A domain model can implement `IChartDataPoint` directly when mapping data into
`ChartDataPoint` objects would be unnecessary.

## Simple values

Use `Values` for a numeric sequence. Labels can be provided independently:

```xml
<controls:ColumnChart Values="{Binding WeeklyValues}"
                      XAxisLabelsSource="{Binding WeeklyLabels}"
                      AutoRange="True" />
```

`ItemsSource` takes precedence when both data properties are set.

## Thresholds

Thresholds are data-driven rather than fixed to Normal, Warning, and Danger.
Each `ChartThreshold` defines its boundary, brush, and optional semantic label:

```csharp
public IReadOnlyList<ChartThreshold> SalesThresholds { get; } =
[
    new ChartThreshold { Label = "Normal", Value = 40, Brush = Brushes.Green },
    new ChartThreshold { Label = "Warning", Value = 65, Brush = Brushes.Orange },
    new ChartThreshold { Label = "Danger", Value = 80, Brush = Brushes.Red },
];
```

`HigherIsMoreSevere` is the default. For metrics where lower values are worse,
set `ThresholdDirection="LowerIsMoreSevere"`. Thresholds are evaluated by value,
so collection order does not affect the result. `DefaultBrush` is used when no
threshold matches, and a data point's own `Brush` takes precedence over both.

Negative and zero thresholds are supported.

When `ChartThreshold.Label` is set, it is rendered at the threshold's actual Y
axis position. `ShowThresholdLabels` defaults to `True`; nearby numeric tick
labels are skipped to prevent overlap. Use `ThresholdLabelFontSize` to size all
threshold labels or `ChartThreshold.LabelBrush` to override one label's color.

## Axes

Axis labels are enabled by default. `YAxisWidth` defaults to `44` and
`XAxisHeight` defaults to `26`, so neither property is required for normal use.
Set `ShowYAxis` or `ShowXAxis` to `False` to remove an axis. Text and grid styling
can be configured independently:

```xml
<controls:ColumnChart XAxisTextBrush="#64748B"
                      YAxisTextBrush="#475569"
                      XAxisFontSize="11"
                      YAxisFontSize="11"
                      GridLineBrush="#CBD5E1" />
```

## Animation and updates

Animation is enabled by default. `AnimationDuration` defaults to `320ms`, and
`AnimationItemLimit` disables animation when the item count exceeds `200`.

`UpdateThrottleInterval` controls how often rapidly changing source collections
are applied. It defaults to `400ms`; set it to `TimeSpan.Zero` to disable
throttling:

```xml
<controls:ColumnChart IsAnimationEnabled="True"
                      AnimationDuration="0:0:0.45"
                      UpdateThrottleInterval="0:0:0.25" />
```

## Column background and fill

`ColumnBackgroundBrush` fills a full-height track behind every column. The track
uses the same width as the value column and spans the complete plot height.
`DefaultBrush` fills the actual value portion rendered over that track.

```xml
<controls:ColumnChart ColumnBackgroundBrush="#E2E8F0"
                      DefaultBrush="#3B82F6" />
```

Set `ChartDataPoint.ColumnBackgroundBrush` to override the track color for one
item. A null control and item brush leaves the column track transparent. Tracks
are drawn before grid lines, so grid lines remain visible.

## Interaction

Set `SelectedIndex` or `SelectedItem` with two-way binding to observe selection.
`ItemClickCommand` receives the selected `IChartDataPoint` as its parameter.
Tooltips use the data point's `ToolTip` first and otherwise display its label and
formatted value. See [Chart tooltips](chart-tooltips.md) for shared styling and
`DataTemplate` customization.

When an item implements `INotifyPropertyChanged`, changes to its value, label,
brush, or tooltip are observed automatically. The same configured
`UpdateThrottleInterval` is used for those updates. Selection can be changed by
pointer or keyboard, and `SelectedIndex` and `SelectedItem` remain synchronized
when either one is set externally.

Selection is visually transparent by default: the selected item and index still
update, but no fill, overlay, or stroke is rendered. Each part of an optional
selection effect can be configured independently:

```xml
<controls:ColumnChart SelectedBarBrush="#0F766E"
                      SelectionOverlayBrush="#40FFFFFF"
                      SelectionStrokeBrush="#0F172A"
                      SelectionStrokeThickness="2" />
```

Leave `SelectedBarBrush` and `SelectionOverlayBrush` unset and keep
`SelectionStrokeThickness` at `0` to retain the default invisible selection.
