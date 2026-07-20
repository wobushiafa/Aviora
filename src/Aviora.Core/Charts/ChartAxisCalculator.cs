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
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        desiredTickCount = Clamp(desiredTickCount, 2, 20);

        if (!autoRange)
        {
            (minimum, maximum) = NormalizeRange(minimum, maximum);
            return new ChartAxisScale(
                minimum,
                maximum,
                BuildUniformTicks(minimum, maximum, desiredTickCount));
        }

        var finiteValues = values.Where(IsFinite).ToList();
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

        double padding = (dataMaximum - dataMinimum) * Clamp(paddingRatio, 0, 1);
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

        int actualTickCount = Clamp(
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
        if (!IsFinite(minimum))
        {
            minimum = 0;
        }

        if (!IsFinite(maximum) || maximum <= minimum)
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
        if (!IsFinite(value) || value <= 0)
        {
            return 1;
        }

        double exponent = Math.Floor(Math.Log10(value));
        double fraction = value / Math.Pow(10, exponent);
        double niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
        return niceFraction * Math.Pow(10, exponent);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);
}
