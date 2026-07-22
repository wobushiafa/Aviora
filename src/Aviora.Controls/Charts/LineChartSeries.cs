using Avalonia.Media;

namespace Aviora.Controls;

/// <summary>Describes one named data series in a <see cref="LineChart"/>.</summary>
public sealed class LineChartSeries
{
    /// <summary>Gets the name used to identify this series.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the rich data points in this series.</summary>
    public IEnumerable<IChartDataPoint>? ItemsSource { get; init; }

    /// <summary>Gets simple numeric values used when <see cref="ItemsSource"/> is not set.</summary>
    public IEnumerable<double>? Values { get; init; }

    /// <summary>Gets the line brush for this series.</summary>
    public IBrush? LineBrush { get; init; }

    /// <summary>Gets the area fill brush for this series.</summary>
    public IBrush? AreaFillBrush { get; init; }

    /// <summary>Gets the point brush for this series.</summary>
    public IBrush? PointBrush { get; init; }
}
