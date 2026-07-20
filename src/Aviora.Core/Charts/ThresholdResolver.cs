namespace Aviora.Core.Charts;

/// <summary>
/// Resolves the nearest threshold crossed by a value.
/// </summary>
public static class ThresholdResolver
{
    /// <summary>
    /// Returns the matching threshold, or <see langword="null"/> when no threshold matches.
    /// Collection order does not affect the result.
    /// </summary>
    public static ThresholdRule? Resolve(
        double value,
        IEnumerable<ThresholdRule>? thresholds,
        ThresholdDirection direction)
    {
        if (!double.IsFinite(value) || thresholds == null)
        {
            return null;
        }

        IEnumerable<ThresholdRule> candidates = thresholds.Where(item => double.IsFinite(item.Value));
        return direction == ThresholdDirection.HigherIsMoreSevere
            ? candidates.Where(item => value >= item.Value).MaxBy(item => item.Value)
            : candidates.Where(item => value <= item.Value).MinBy(item => item.Value);
    }
}
