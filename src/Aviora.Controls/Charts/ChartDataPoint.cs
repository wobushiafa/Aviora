using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

/// <summary>
/// Describes the minimum data contract required by a chart.
/// </summary>
public interface IChartDataPoint : IChartPoint
{
    /// <summary>Gets a stable identifier for the data point.</summary>
    new object? Key { get; }

    /// <summary>Gets the category label shown on the horizontal axis.</summary>
    new string? Label { get; }

    /// <summary>Gets the numeric value represented by the column.</summary>
    new double Value { get; }

    /// <summary>Gets an optional per-point brush.</summary>
    IBrush? Brush { get; }

    /// <summary>Gets an optional full-height background brush for this column.</summary>
    IBrush? ColumnBackgroundBrush => null;

    /// <summary>Gets optional text shown by the default Tooltip.</summary>
    new string? ToolTip { get; }
}

/// <summary>
/// Default immutable data point implementation for chart bindings.
/// </summary>
public sealed class ChartDataPoint : ChartPoint, IChartDataPoint
{
    /// <inheritdoc />
    public override object? Key { get; init; }

    /// <inheritdoc />
    public override string? Label { get; init; }

    /// <inheritdoc />
    public override double Value { get; init; }

    /// <inheritdoc />
    public IBrush? Brush { get; init; }

    /// <inheritdoc />
    public IBrush? ColumnBackgroundBrush { get; init; }

    /// <inheritdoc />
    public override string? ToolTip { get; init; }
}
