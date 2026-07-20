namespace Aviora.Core.Charts;

/// <summary>
/// Calculates framework-independent chart axis ranges and tick values.
/// </summary>
public static class ChartAxisCalculator
{
    /// <summary>Calculates an automatic or fixed axis scale.</summary>
    public static ChartAxisScale Calculate(
        IEnumerable<double> values,
        bool autoRange,
        double minimum,
        double maximum,
        int desiredTickCount,
        double paddingRatio = 0.08)
    {
        ArgumentNullException.ThrowIfNull(values);
        desiredTickCount = Math.Clamp(desiredTickCount, 2, 20);

        if (!autoRange)
        {
            (minimum, maximum) = NormalizeRange(minimum, maximum);
            return new ChartAxisScale(
                minimum,
                maximum,
                BuildUniformTicks(minimum, maximum, desiredTickCount));
        }

        var finiteValues = values.Where(double.IsFinite).ToList();
        if (finiteValues.Count == 0)
        {
            return new ChartAxisScale(0, 1, BuildUniformTicks(0, 1, desiredTickCount));
        }

        double dataMinimum = Math.Min(0, finiteValues.Min());
        double dataMaximum = Math.Max(0, finiteValues.Max());
        if (dataMaximum - dataMinimum < double.Epsilon)
        {
            dataMaximum += Math.Max(Math.Abs(dataMaximum) * 0.1, 1);
        }

        double padding = (dataMaximum - dataMinimum) * Math.Clamp(paddingRatio, 0, 1);
        if (dataMinimum < 0)
        {
            dataMinimum -= padding;
        }

        if (dataMaximum > 0)
        {
            dataMaximum += padding;
        }

        double step = NiceCeiling((dataMaximum - dataMinimum) / (desiredTickCount - 1));
        double niceMinimum = Math.Floor(dataMinimum / step) * step;
        double niceMaximum = Math.Ceiling(dataMaximum / step) * step;
        (niceMinimum, niceMaximum) = NormalizeRange(niceMinimum, niceMaximum);

        int actualTickCount = Math.Clamp(
            (int)Math.Round((niceMaximum - niceMinimum) / step) + 1,
            2,
            20);

        return new ChartAxisScale(
            niceMinimum,
            niceMaximum,
            BuildUniformTicks(niceMinimum, niceMaximum, actualTickCount));
    }

    private static (double Minimum, double Maximum) NormalizeRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum))
        {
            minimum = 0;
        }

        if (!double.IsFinite(maximum) || maximum <= minimum)
        {
            maximum = minimum + 1;
        }

        return (minimum, maximum);
    }

    private static IReadOnlyList<double> BuildUniformTicks(double minimum, double maximum, int tickCount)
    {
        var ticks = new double[tickCount];
        for (int index = 0; index < tickCount; index++)
        {
            ticks[index] = maximum - ((maximum - minimum) * index / (tickCount - 1));
        }

        return ticks;
    }

    private static double NiceCeiling(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return 1;
        }

        double exponent = Math.Floor(Math.Log10(value));
        double fraction = value / Math.Pow(10, exponent);
        double niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
        return niceFraction * Math.Pow(10, exponent);
    }
}
