using System.Globalization;

namespace Aviora.Controls;

internal static class ChartValueFormatter
{
    public static string Format(double value)
    {
        if (!double.IsFinite(value))
        {
            return "-";
        }

        double rounded = Math.Round(value, 2);
        return Math.Abs(rounded) >= 1_000_000
            ? rounded.ToString("0.##E+0", CultureInfo.InvariantCulture)
            : rounded.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
