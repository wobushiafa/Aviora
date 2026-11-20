using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

/// <summary>
/// Defines which side of a threshold represents increasing severity.
/// </summary>
public enum ThresholdDirection
{
    /// <summary>Higher values match progressively higher thresholds.</summary>
    HigherIsMoreSevere,

    /// <summary>Lower values match progressively lower thresholds.</summary>
    LowerIsMoreSevere,
}

/// <summary>
/// Defines a value boundary, line styling, optional region band, and brush used after that boundary is crossed.
/// </summary>
public sealed class ChartThreshold : ThresholdRule
{
    /// <summary>Gets the numeric boundary.</summary>
    public override double Value { get; init; }

    /// <summary>Gets the brush used for values matching this threshold.</summary>
    public IBrush Brush { get; init; } = Brushes.Gray;

    /// <summary>Gets an optional semantic name, such as Normal or Danger.</summary>
    public override string? Label { get; init; }

    /// <summary>Gets an optional label brush. The threshold brush is used when omitted.</summary>
    public IBrush? LabelBrush { get; init; }

    /// <summary>Gets the stroke thickness of the threshold guide line.</summary>
    public double StrokeThickness { get; init; } = 1.2;

    /// <summary>Gets an optional dash pattern for the threshold guide line.</summary>
    public IReadOnlyList<double>? DashArray { get; init; }

    /// <summary>Gets an optional brush used to shade the band starting at <see cref="Value"/>.</summary>
    public IBrush? BandBrush { get; init; }

    /// <summary>
    /// Gets an optional upper or lower bound for the shaded band. When <see cref="double.NaN"/>,
    /// the band extends toward the extreme side indicated by the chart's <see cref="ThresholdDirection"/>.
    /// </summary>
    public double BandEndValue { get; init; } = double.NaN;
}
