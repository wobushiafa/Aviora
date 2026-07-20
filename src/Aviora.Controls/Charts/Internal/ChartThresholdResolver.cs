using Avalonia.Media;
using CoreCharts = Aviora.Core.Charts;

namespace Aviora.Controls;

internal static class ChartThresholdResolver
{
    public static IBrush Resolve(
        double value,
        IBrush defaultBrush,
        IEnumerable<ChartThreshold>? thresholds,
        ThresholdDirection direction)
    {
        if (!double.IsFinite(value) || thresholds == null)
        {
            return defaultBrush;
        }

        CoreCharts.ThresholdDirection coreDirection = direction == ThresholdDirection.HigherIsMoreSevere
            ? CoreCharts.ThresholdDirection.HigherIsMoreSevere
            : CoreCharts.ThresholdDirection.LowerIsMoreSevere;
        ChartThreshold? match = CoreCharts.ThresholdResolver.Resolve(
            value,
            thresholds,
            coreDirection) as ChartThreshold;

        return match?.Brush ?? defaultBrush;
    }
}
