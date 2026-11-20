namespace Aviora.Core.Charts;

/// <summary>
/// Specifies the downsampling strategy used when rendering large chart datasets.
/// </summary>
public enum ChartDownsampleMode
{
    /// <summary>
    /// Renders all points without downsampling.
    /// </summary>
    None,

    /// <summary>
    /// Largest-Triangle-Three-Buckets algorithm that preserves visual shape and extrema.
    /// </summary>
    Lttb,

    /// <summary>
    /// Per-bucket minimum and maximum envelope sampling that guarantees no spike is missed.
    /// </summary>
    MinMax,
}

/// <summary>
/// Framework-independent point reduction algorithms for large chart series.
/// </summary>
public static class ChartDownsampler
{
    /// <summary>
    /// Returns whether downsampling should run for <paramref name="count"/> items given
    /// <paramref name="targetCount"/> and <paramref name="mode"/>.
    /// </summary>
    public static bool NeedsDownsampling(int count, int targetCount, ChartDownsampleMode mode) =>
        mode != ChartDownsampleMode.None && targetCount > 2 && count > targetCount;

    /// <summary>
    /// Selects a sorted subset of representative indices from <paramref name="values"/>.
    /// Always preserves the first and last indices when <paramref name="values"/> is non-empty.
    /// </summary>
    public static int[] SelectIndices(
        IReadOnlyList<double> values,
        int targetCount,
        ChartDownsampleMode mode)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        int count = values.Count;
        if (count == 0)
        {
            return [];
        }

        if (!NeedsDownsampling(count, targetCount, mode))
        {
            var all = new int[count];
            for (int index = 0; index < count; index++)
            {
                all[index] = index;
            }

            return all;
        }

        return mode == ChartDownsampleMode.MinMax
            ? SelectMinMax(values, targetCount)
            : SelectLttb(values, targetCount);
    }

    private static int[] SelectLttb(IReadOnlyList<double> values, int targetCount)
    {
        int count = values.Count;
        int bucketCount = targetCount - 2;
        double bucketSize = (double)(count - 2) / bucketCount;

        var selected = new List<int>(targetCount) { 0 };
        int previousIndex = 0;

        for (int bucket = 0; bucket < bucketCount; bucket++)
        {
            int rangeStart = 1 + (int)Math.Floor(bucket * bucketSize);
            int rangeEnd = Math.Min(count - 1, 1 + (int)Math.Floor((bucket + 1) * bucketSize));
            if (rangeEnd <= rangeStart)
            {
                rangeEnd = Math.Min(count - 1, rangeStart + 1);
            }

            int nextStart = Math.Min(count - 1, 1 + (int)Math.Floor((bucket + 1) * bucketSize));
            int nextEnd = Math.Min(count, 1 + (int)Math.Floor((bucket + 2) * bucketSize));
            if (bucket == bucketCount - 1)
            {
                nextStart = count - 1;
                nextEnd = count;
            }

            double avgX = 0;
            double avgY = 0;
            int finiteNext = 0;
            for (int index = nextStart; index < nextEnd; index++)
            {
                double val = values[index];
                if (!IsFinite(val))
                {
                    continue;
                }

                avgX += index;
                avgY += val;
                finiteNext++;
            }

            if (finiteNext > 0)
            {
                avgX /= finiteNext;
                avgY /= finiteNext;
            }
            else
            {
                avgX = nextStart;
                avgY = 0;
            }

            double ax = previousIndex;
            double ay = IsFinite(values[previousIndex]) ? values[previousIndex] : avgY;

            double maxArea = -1;
            int maxAreaIndex = -1;
            int firstNonFinite = -1;

            for (int index = rangeStart; index < rangeEnd; index++)
            {
                double val = values[index];
                if (!IsFinite(val))
                {
                    if (firstNonFinite < 0)
                    {
                        firstNonFinite = index;
                    }

                    continue;
                }

                double area = Math.Abs(((ax - avgX) * (val - ay)) - ((ax - index) * (avgY - ay)));
                if (area > maxArea)
                {
                    maxArea = area;
                    maxAreaIndex = index;
                }
            }

            int chosen = maxAreaIndex >= 0
                ? maxAreaIndex
                : firstNonFinite >= 0
                    ? firstNonFinite
                    : rangeStart;

            if (chosen > selected[selected.Count - 1])
            {
                selected.Add(chosen);
                previousIndex = chosen;
            }
        }

        if (selected[selected.Count - 1] != count - 1)
        {
            selected.Add(count - 1);
        }

        return selected.ToArray();
    }

    private static int[] SelectMinMax(IReadOnlyList<double> values, int targetCount)
    {
        int count = values.Count;
        int bucketCount = Math.Max(1, (targetCount - 2) / 2);
        double bucketSize = (double)(count - 2) / bucketCount;

        var selected = new List<int>(targetCount) { 0 };

        for (int bucket = 0; bucket < bucketCount; bucket++)
        {
            int rangeStart = 1 + (int)Math.Floor(bucket * bucketSize);
            int rangeEnd = Math.Min(count - 1, 1 + (int)Math.Floor((bucket + 1) * bucketSize));
            if (rangeEnd <= rangeStart)
            {
                continue;
            }

            int minIndex = -1;
            int maxIndex = -1;
            int nonFiniteIndex = -1;
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;

            for (int index = rangeStart; index < rangeEnd; index++)
            {
                double val = values[index];
                if (!IsFinite(val))
                {
                    if (nonFiniteIndex < 0)
                    {
                        nonFiniteIndex = index;
                    }

                    continue;
                }

                if (val < minValue)
                {
                    minValue = val;
                    minIndex = index;
                }

                if (val > maxValue)
                {
                    maxValue = val;
                    maxIndex = index;
                }
            }

            if (minIndex < 0 && nonFiniteIndex >= 0)
            {
                AppendUnique(selected, nonFiniteIndex);
                continue;
            }

            if (minIndex >= 0 && maxIndex >= 0)
            {
                if (minIndex <= maxIndex)
                {
                    AppendUnique(selected, minIndex);
                    AppendUnique(selected, maxIndex);
                }
                else
                {
                    AppendUnique(selected, maxIndex);
                    AppendUnique(selected, minIndex);
                }
            }

            if (nonFiniteIndex >= 0)
            {
                AppendUnique(selected, nonFiniteIndex);
            }
        }

        AppendUnique(selected, count - 1);
        return selected.ToArray();
    }

    private static void AppendUnique(List<int> selected, int index)
    {
        if (selected.Count == 0 || index > selected[selected.Count - 1])
        {
            selected.Add(index);
        }
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
