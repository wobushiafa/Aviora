namespace Aviora.Core.Charts;

/// <summary>
/// Defines a framework-independent value boundary with an optional semantic label.
/// </summary>
public class ThresholdRule
{
    /// <summary>Gets the numeric boundary.</summary>
    public virtual double Value { get; init; }

    /// <summary>Gets an optional semantic label for the boundary.</summary>
    public virtual string? Label { get; init; }
}

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
