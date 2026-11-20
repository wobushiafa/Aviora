namespace Aviora.Controls;

/// <summary>
/// Specifies how multiple series are arranged within each category of a <see cref="ColumnChart"/>.
/// </summary>
public enum ColumnSeriesLayoutMode
{
    /// <summary>Places series columns side by side within each category slot.</summary>
    Grouped,

    /// <summary>Stacks series columns vertically using cumulative values.</summary>
    Stacked,

    /// <summary>Stacks series columns normalized to 100% of each category total.</summary>
    Stacked100,
}
