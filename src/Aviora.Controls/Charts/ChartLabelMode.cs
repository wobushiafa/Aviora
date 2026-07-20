namespace Aviora.Controls;

/// <summary>
/// Controls how category labels are reduced when the chart is too narrow.
/// </summary>
public enum ChartLabelMode
{
    /// <summary>Chooses labels that fit the available width.</summary>
    Auto,

    /// <summary>Renders every label.</summary>
    All,

    /// <summary>Renders labels at <see cref="CartesianChart.XAxisLabelInterval"/> intervals.</summary>
    Interval,
}
