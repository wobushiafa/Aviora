using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

/// <summary>Describes one named data series in a <see cref="LineChart"/>.</summary>
public sealed class LineChartSeries : AvaloniaObject
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<LineChartSeries, string?>(nameof(Title));

    /// <summary>Defines the <see cref="ItemsSource"/> property.</summary>
    public static readonly StyledProperty<IEnumerable<IChartDataPoint>?> ItemsSourceProperty =
        AvaloniaProperty.Register<LineChartSeries, IEnumerable<IChartDataPoint>?>(nameof(ItemsSource));

    /// <summary>Defines the <see cref="Values"/> property.</summary>
    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<LineChartSeries, IEnumerable<double>?>(nameof(Values));

    /// <summary>Defines the <see cref="LineBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> LineBrushProperty =
        AvaloniaProperty.Register<LineChartSeries, IBrush?>(nameof(LineBrush));

    /// <summary>Defines the <see cref="AreaFillBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> AreaFillBrushProperty =
        AvaloniaProperty.Register<LineChartSeries, IBrush?>(nameof(AreaFillBrush));

    /// <summary>Defines the <see cref="PointBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> PointBrushProperty =
        AvaloniaProperty.Register<LineChartSeries, IBrush?>(nameof(PointBrush));

    /// <summary>Defines the <see cref="IsVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsVisibleProperty =
        AvaloniaProperty.Register<LineChartSeries, bool>(nameof(IsVisible), true);

    /// <summary>Defines the <see cref="LineThickness"/> property.</summary>
    public static readonly StyledProperty<double?> LineThicknessProperty =
        AvaloniaProperty.Register<LineChartSeries, double?>(nameof(LineThickness));

    /// <summary>Defines the <see cref="StrokeDashArray"/> property.</summary>
    public static readonly StyledProperty<IReadOnlyList<double>?> StrokeDashArrayProperty =
        AvaloniaProperty.Register<LineChartSeries, IReadOnlyList<double>?>(nameof(StrokeDashArray));

    /// <summary>Defines the <see cref="InterpolationMode"/> property.</summary>
    public static readonly StyledProperty<LineInterpolationMode?> InterpolationModeProperty =
        AvaloniaProperty.Register<LineChartSeries, LineInterpolationMode?>(nameof(InterpolationMode));

    /// <summary>Defines the <see cref="ShowPoints"/> property.</summary>
    public static readonly StyledProperty<bool?> ShowPointsProperty =
        AvaloniaProperty.Register<LineChartSeries, bool?>(nameof(ShowPoints));

    /// <summary>Defines the <see cref="PointRadius"/> property.</summary>
    public static readonly StyledProperty<double?> PointRadiusProperty =
        AvaloniaProperty.Register<LineChartSeries, double?>(nameof(PointRadius));

    /// <summary>Defines the <see cref="PointShape"/> property.</summary>
    public static readonly StyledProperty<ChartPointShape?> PointShapeProperty =
        AvaloniaProperty.Register<LineChartSeries, ChartPointShape?>(nameof(PointShape));

    /// <summary>Defines the <see cref="PointStrokeBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> PointStrokeBrushProperty =
        AvaloniaProperty.Register<LineChartSeries, IBrush?>(nameof(PointStrokeBrush));

    /// <summary>Defines the <see cref="PointStrokeThickness"/> property.</summary>
    public static readonly StyledProperty<double?> PointStrokeThicknessProperty =
        AvaloniaProperty.Register<LineChartSeries, double?>(nameof(PointStrokeThickness));

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

    /// <summary>Gets or sets the line brush for this series.</summary>
    public IBrush? LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    /// <summary>Gets or sets the area fill brush for this series.</summary>
    public IBrush? AreaFillBrush
    {
        get => GetValue(AreaFillBrushProperty);
        set => SetValue(AreaFillBrushProperty, value);
    }

    /// <summary>Gets or sets the point brush for this series.</summary>
    public IBrush? PointBrush
    {
        get => GetValue(PointBrushProperty);
        set => SetValue(PointBrushProperty, value);
    }

    /// <summary>Gets or sets whether this series is visible.</summary>
    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    /// <summary>Gets or sets an optional line thickness override for this series.</summary>
    public double? LineThickness
    {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    /// <summary>Gets or sets an optional dash pattern override for this series.</summary>
    public IReadOnlyList<double>? StrokeDashArray
    {
        get => GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    /// <summary>Gets or sets an optional interpolation mode override for this series.</summary>
    public LineInterpolationMode? InterpolationMode
    {
        get => GetValue(InterpolationModeProperty);
        set => SetValue(InterpolationModeProperty, value);
    }

    /// <summary>Gets or sets an optional override controlling whether point markers are shown for this series.</summary>
    public bool? ShowPoints
    {
        get => GetValue(ShowPointsProperty);
        set => SetValue(ShowPointsProperty, value);
    }

    /// <summary>Gets or sets an optional point radius override for this series.</summary>
    public double? PointRadius
    {
        get => GetValue(PointRadiusProperty);
        set => SetValue(PointRadiusProperty, value);
    }

    /// <summary>Gets or sets an optional point marker shape override for this series.</summary>
    public ChartPointShape? PointShape
    {
        get => GetValue(PointShapeProperty);
        set => SetValue(PointShapeProperty, value);
    }

    /// <summary>Gets or sets an optional point stroke brush override for this series.</summary>
    public IBrush? PointStrokeBrush
    {
        get => GetValue(PointStrokeBrushProperty);
        set => SetValue(PointStrokeBrushProperty, value);
    }

    /// <summary>Gets or sets an optional point stroke thickness override for this series.</summary>
    public double? PointStrokeThickness
    {
        get => GetValue(PointStrokeThicknessProperty);
        set => SetValue(PointStrokeThicknessProperty, value);
    }
}
