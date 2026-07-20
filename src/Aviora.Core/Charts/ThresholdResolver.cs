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
        if (!IsFinite(value) || thresholds == null)
        {
            return null;
        }

        ThresholdRule? match = null;
        foreach (ThresholdRule candidate in thresholds)
        {
            if (!IsFinite(candidate.Value))
            {
                continue;
            }

            bool isBetterMatch = direction == ThresholdDirection.HigherIsMoreSevere
                ? value >= candidate.Value && (match is null || candidate.Value > match.Value)
                : value <= candidate.Value && (match is null || candidate.Value < match.Value);
            if (isBetterMatch)
            {
                match = candidate;
            }
        }

        return match;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
