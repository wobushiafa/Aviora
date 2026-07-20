namespace Aviora.Core.Charts;

/// <summary>
/// Describes a normalized numeric axis range and its tick values.
/// </summary>
public readonly record struct ChartAxisScale(
    double Minimum,
    double Maximum,
    IReadOnlyList<double> Ticks)
{
    /// <summary>Gets the distance between the maximum and minimum values.</summary>
    public double Range => Maximum - Minimum;
}
