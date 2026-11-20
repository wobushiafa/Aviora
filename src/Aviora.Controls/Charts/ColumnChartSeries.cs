using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

/// <summary>Describes one named data series in a <see cref="ColumnChart"/>.</summary>
public sealed class ColumnChartSeries : AvaloniaObject
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ColumnChartSeries, string?>(nameof(Title));

    /// <summary>Defines the <see cref="ItemsSource"/> property.</summary>
    public static readonly StyledProperty<IEnumerable<IChartDataPoint>?> ItemsSourceProperty =
        AvaloniaProperty.Register<ColumnChartSeries, IEnumerable<IChartDataPoint>?>(nameof(ItemsSource));

    /// <summary>Defines the <see cref="Values"/> property.</summary>
    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<ColumnChartSeries, IEnumerable<double>?>(nameof(Values));

    /// <summary>Defines the <see cref="Brush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BrushProperty =
        AvaloniaProperty.Register<ColumnChartSeries, IBrush?>(nameof(Brush));

    /// <summary>Defines the <see cref="SelectedBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> SelectedBrushProperty =
        AvaloniaProperty.Register<ColumnChartSeries, IBrush?>(nameof(SelectedBrush));

    /// <summary>Defines the <see cref="IsVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsVisibleProperty =
        AvaloniaProperty.Register<ColumnChartSeries, bool>(nameof(IsVisible), true);

    /// <summary>Gets or sets the name used to identify this series.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the rich data points in this series.</summary>
    public IEnumerable<IChartDataPoint>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets simple numeric values used when <see cref="ItemsSource"/> is not set.</summary>
    public IEnumerable<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Gets or sets the default column brush for this series.</summary>
    public IBrush? Brush
    {
        get => GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    /// <summary>Gets or sets the brush used when a bar in this series is selected.</summary>
    public IBrush? SelectedBrush
    {
        get => GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    /// <summary>Gets or sets whether this series is visible.</summary>
    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }
}
