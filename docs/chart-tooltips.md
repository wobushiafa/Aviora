# Chart tooltips

Every `CartesianChart` owns one tooltip presenter. Moving inside the same data
slot keeps that presenter and its anchor stable; changing slots updates its
content without creating one tooltip per data point.

## Basic styling

The default text tooltip can be styled directly or through an Avalonia style:

```xml
<controls:LineChart ToolTipBackground="#F9FAFB"
                    ToolTipTextBrush="#111827"
                    ToolTipFontSize="12"
                    ToolTipPadding="10,8"
                    ToolTipCornerRadius="6"
                    ToolTipBorderBrush="#CBD5E1"
                    ToolTipBorderThickness="1"
                    ToolTipBoxShadow="0 4 12 0 #33000000"
                    ToolTipHorizontalOffset="10"
                    ToolTipVerticalOffset="10" />
```

`ToolTipHorizontalOffset` moves the presenter to the right of its anchor.
`ToolTipVerticalOffset` controls the gap above the anchor. The presenter is
kept inside the chart bounds.

## Content templates

Set `ToolTipTemplate` when the tooltip needs structured content. The template's
data context is the hovered `IChartDataPoint`:

```xml
<Window.Resources>
    <DataTemplate x:Key="ChartToolTipTemplate"
                  x:DataType="controls:IChartDataPoint">
        <StackPanel Spacing="2">
            <TextBlock FontWeight="SemiBold" Text="{Binding Label}" />
            <TextBlock Opacity="0.72" Text="{Binding Value}" />
        </StackPanel>
    </DataTemplate>
</Window.Resources>

<controls:LineChart ItemsSource="{Binding RevenueTrend}"
                    ToolTipTemplate="{StaticResource ChartToolTipTemplate}" />
```

Without a template, content is resolved from `ToolTipFormatter`, then the data
point's `ToolTip`, and finally the default `Label: Value` text.
