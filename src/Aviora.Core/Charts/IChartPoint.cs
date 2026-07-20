namespace Aviora.Core.Charts;

/// <summary>
/// Describes the framework-independent data required to plot one chart point.
/// </summary>
public interface IChartPoint
{
    /// <summary>Gets a stable identifier for the point.</summary>
    object? Key { get; }

    /// <summary>Gets the category label associated with the point.</summary>
    string? Label { get; }

    /// <summary>Gets the numeric value represented by the point.</summary>
    double Value { get; }

    /// <summary>Gets optional descriptive text for the point.</summary>
    string? ToolTip { get; }
}
