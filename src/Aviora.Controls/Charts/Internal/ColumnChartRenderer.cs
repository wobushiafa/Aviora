using System.Collections;
using Avalonia;
using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal sealed class ColumnChartRenderer : CartesianChartRenderer<ColumnChart>
{
    private Pen? _cachedSelectionPen;
    private double _cachedSelectionThickness;
    private IBrush? _cachedSelectionBrush;

    public override void Invalidate(ChartInvalidationFlags flags)
    {
        bool hadState = (flags & ChartInvalidationFlags.LayoutAndScale) != 0;
        base.Invalidate(flags);
        if ((flags & (ChartInvalidationFlags.VisualOnly | ChartInvalidationFlags.LayoutAndScale)) != 0 || hadState)
        {
            _cachedSelectionPen = null;
            _cachedSelectionBrush = null;
            _cachedSelectionThickness = 0;
        }
    }

    protected override double GetItemWidthRatio(ColumnChart chart) => chart.BarWidthRatio;

    protected override int ResolveCategoryCount(ColumnChart chart, IReadOnlyList<IChartDataPoint> items)
    {
        if (chart.Series == null)
        {
            return base.ResolveCategoryCount(chart, items);
        }

        int categoryCount = 0;
        bool hasSeries = false;
        foreach (IChartDataPoint item in items)
        {
            if (item is ChartDataPipeline.SeriesDataPoint series)
            {
                hasSeries = true;
                categoryCount = Math.Max(categoryCount, series.PointIndex + 1);
            }
        }

        if (!hasSeries)
        {
            foreach (ColumnChartSeries series in chart.Series)
            {
                hasSeries = true;
                if (series.ItemsSource is ICollection itemsCol)
                {
                    categoryCount = Math.Max(categoryCount, itemsCol.Count);
                }
                else if (series.Values is ICollection valCol)
                {
                    categoryCount = Math.Max(categoryCount, valCol.Count);
                }
            }
        }

        return hasSeries ? categoryCount : base.ResolveCategoryCount(chart, items);
    }

    protected override IReadOnlyList<string> GetDefaultXAxisLabels(IReadOnlyList<IChartDataPoint> items)
    {
        var labels = new SortedDictionary<int, string>();
        foreach (IChartDataPoint item in items)
        {
            if (item is ChartDataPipeline.SeriesDataPoint series && !labels.ContainsKey(series.PointIndex))
            {
                labels.Add(series.PointIndex, series.Label ?? string.Empty);
            }
        }

        return labels.Count > 0 ? labels.Values.ToList() : base.GetDefaultXAxisLabels(items);
    }

    protected override IEnumerable<double> GetScaleValues(ColumnChart chart, IReadOnlyList<IChartDataPoint> items)
    {
        bool hasSeries = items.Any(item => item is ChartDataPipeline.SeriesDataPoint);
        if (!hasSeries || chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Grouped)
        {
            return base.GetScaleValues(chart, items);
        }

        if (chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Stacked100)
        {
            bool hasNegative = items.Any(item => double.IsFinite(item.Value) && item.Value < 0);
            return hasNegative ? [-100.0, 100.0] : [0.0, 100.0];
        }

        var posSums = new Dictionary<int, double>();
        var negSums = new Dictionary<int, double>();
        foreach (IChartDataPoint item in items)
        {
            if (!double.IsFinite(item.Value))
            {
                continue;
            }

            int category = item is ChartDataPipeline.SeriesDataPoint series ? series.PointIndex : 0;
            if (item.Value >= 0)
            {
                posSums[category] = posSums.GetValueOrDefault(category) + item.Value;
            }
            else
            {
                negSums[category] = negSums.GetValueOrDefault(category) + item.Value;
            }
        }

        var sums = new List<double>(posSums.Count + negSums.Count);
        sums.AddRange(posSums.Values);
        sums.AddRange(negSums.Values);
        return sums;
    }

    public override int HitTest(ColumnChart chart, IReadOnlyList<IChartDataPoint> items, Point point)
    {
        ChartRenderState state = base.EnsureState(chart, items);
        int categoryIndex = ChartLayoutCalculator.HitTest(state.Layout, point, state.CategoryCount);
        if (categoryIndex < 0)
        {
            return -1;
        }

        if (chart.Series == null || !items.Any(i => i is ChartDataPipeline.SeriesDataPoint))
        {
            return categoryIndex < items.Count ? categoryIndex : -1;
        }

        List<(int ItemIndex, int SeriesIndex, IChartDataPoint Item)> categoryItems = [];
        for (int index = 0; index < items.Count; index++)
        {
            if (items[index] is ChartDataPipeline.SeriesDataPoint s && s.PointIndex == categoryIndex)
            {
                categoryItems.Add((index, s.SeriesIndex, items[index]));
            }
        }

        if (categoryItems.Count == 0)
        {
            return -1;
        }

        if (chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Grouped)
        {
            List<int> visibleSeriesOrder = GetVisibleSeriesOrder(items);
            double slotLeft = state.Layout.Plot.Left + ((categoryIndex - state.Layout.ViewportStartIndex) * state.Layout.SlotWidth);
            (double subWidth, double gap, double groupLeftOffset) = ComputeGroupedGeometry(chart, state.Layout, visibleSeriesOrder.Count);
            double bestDistance = double.PositiveInfinity;
            int bestItemIndex = categoryItems[0].ItemIndex;

            foreach (var entry in categoryItems)
            {
                int ordinal = visibleSeriesOrder.IndexOf(entry.SeriesIndex);
                if (ordinal < 0)
                {
                    continue;
                }

                double barCenter = slotLeft + groupLeftOffset + (ordinal * (subWidth + gap)) + (subWidth / 2);
                double dist = Math.Abs(point.X - barCenter);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestItemIndex = entry.ItemIndex;
                }
            }

            return bestItemIndex;
        }

        // Stacked / Stacked100: hit test Y interval in stack.
        double posAccum = 0;
        double negAccum = 0;
        double categoryAbsTotal = 0;
        if (chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Stacked100)
        {
            foreach (var entry in categoryItems)
            {
                if (double.IsFinite(entry.Item.Value))
                {
                    categoryAbsTotal += Math.Abs(entry.Item.Value);
                }
            }
        }

        foreach (var entry in categoryItems)
        {
            double rawVal = double.IsFinite(entry.Item.Value) ? entry.Item.Value : 0;
            double val = chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Stacked100 && categoryAbsTotal > 0
                ? (rawVal / categoryAbsTotal) * 100.0
                : rawVal;
            double start = val >= 0 ? posAccum : negAccum;
            double end = start + val;
            if (val >= 0)
            {
                posAccum = end;
            }
            else
            {
                negAccum = end;
            }

            double y1 = MapY(Math.Clamp(start, state.Scale.Minimum, state.Scale.Maximum), state);
            double y2 = MapY(Math.Clamp(end, state.Scale.Minimum, state.Scale.Maximum), state);
            double top = Math.Min(y1, y2);
            double bottom = Math.Max(y1, y2);
            if (point.Y >= top && point.Y <= bottom)
            {
                return entry.ItemIndex;
            }
        }

        return categoryItems[0].ItemIndex;
    }

    protected override void DrawChartBackground(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state)
    {
        int start = state.Layout.ViewportStartIndex;
        int end = Math.Min(state.CategoryCount, start + state.Layout.ViewportCount);

        if (chart.HoverSlotBrush != null &&
            chart.HoveredCategoryIndex >= start &&
            chart.HoveredCategoryIndex < end)
        {
            double hoverLeft = state.Layout.Plot.Left + ((chart.HoveredCategoryIndex - start) * state.Layout.SlotWidth);
            context.DrawRectangle(
                chart.HoverSlotBrush,
                null,
                new Rect(hoverLeft, state.Layout.Plot.Top, state.Layout.SlotWidth, state.Layout.Plot.Height));
        }

        bool isMultiSeries = state.Items.Any(i => i is ChartDataPipeline.SeriesDataPoint);
        if (isMultiSeries)
        {
            if (chart.ColumnBackgroundBrush != null && end > start)
            {
                double trackWidth = ClampBarWidth(state.Layout.BarWidth, chart.MinBarWidth, chart.MaxBarWidth);
                for (int category = start; category < end; category++)
                {
                    double x = GetSlotBarLeft(state.Layout, category, trackWidth);
                    context.DrawRectangle(
                        chart.ColumnBackgroundBrush,
                        null,
                        new Rect(x, state.Layout.Plot.Top, trackWidth, state.Layout.Plot.Height));
                }
            }

            return;
        }

        int[]? visibleIndices = ResolveColumnIndices(chart, state);
        if (visibleIndices != null)
        {
            if (chart.ColumnBackgroundBrush == null)
            {
                return;
            }

            double downsampledWidth = ComputeDownsampledBarWidth(chart, state.Layout, visibleIndices.Length);
            for (int pos = 0; pos < visibleIndices.Length; pos++)
            {
                int itemIndex = visibleIndices[pos];
                IBrush? brush = state.Items[itemIndex].ColumnBackgroundBrush ?? chart.ColumnBackgroundBrush;
                if (brush == null)
                {
                    continue;
                }

                double x = GetSlotBarLeft(state.Layout, itemIndex, downsampledWidth);
                context.DrawRectangle(brush, null, new Rect(x, state.Layout.Plot.Top, downsampledWidth, state.Layout.Plot.Height));
            }

            return;
        }

        double barWidth = ClampBarWidth(state.Layout.BarWidth, chart.MinBarWidth, chart.MaxBarWidth);
        for (int index = start; index < Math.Min(end, state.Items.Count); index++)
        {
            IBrush? brush = state.Items[index].ColumnBackgroundBrush ?? chart.ColumnBackgroundBrush;
            if (brush == null)
            {
                continue;
            }

            double x = GetSlotBarLeft(state.Layout, index, barWidth);
            context.DrawRectangle(
                brush,
                null,
                new Rect(x, state.Layout.Plot.Top, barWidth, state.Layout.Plot.Height));
        }
    }

    protected override void DrawSeries(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        bool isMultiSeries = state.Items.Any(i => i is ChartDataPipeline.SeriesDataPoint);
        if (isMultiSeries)
        {
            DrawMultiSeries(context, chart, state, animatedValues);
            return;
        }

        double baseline = MapY(Math.Clamp(0, state.Scale.Minimum, state.Scale.Maximum), state);
        int[]? visibleIndices = ResolveColumnIndices(chart, state);
        if (visibleIndices != null)
        {
            double dsBarWidth = ComputeDownsampledBarWidth(chart, state.Layout, visibleIndices.Length);
            using (context.PushClip(state.Layout.Plot))
            {
                foreach (int index in visibleIndices)
                {
                    DrawSingleBar(context, chart, state, animatedValues, index, index, dsBarWidth, baseline,
                        GetSlotBarLeft(state.Layout, index, dsBarWidth));
                }
            }

            return;
        }

        int start = state.Layout.ViewportStartIndex;
        int end = Math.Min(state.Items.Count, start + state.Layout.ViewportCount);
        double barWidth = ClampBarWidth(state.Layout.BarWidth, chart.MinBarWidth, chart.MaxBarWidth);

        using (context.PushClip(state.Layout.Plot))
        {
            for (int index = start; index < end; index++)
            {
                double x = GetSlotBarLeft(state.Layout, index, barWidth);
                DrawSingleBar(context, chart, state, animatedValues, index, index, barWidth, baseline, x);
            }
        }
    }

    private void DrawMultiSeries(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        int start = state.Layout.ViewportStartIndex;
        int end = Math.Min(state.CategoryCount, start + state.Layout.ViewportCount);
        if (end <= start)
        {
            return;
        }

        if (chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Grouped)
        {
            List<int> visibleSeries = GetVisibleSeriesOrder(state.Items);
            if (visibleSeries.Count == 0)
            {
                return;
            }

            (double subWidth, double gap, double groupOffset) =
                ComputeGroupedGeometry(chart, state.Layout, visibleSeries.Count);
            double baseline = MapY(Math.Clamp(0, state.Scale.Minimum, state.Scale.Maximum), state);

            var ordinalBySeries = new Dictionary<int, int>(visibleSeries.Count);
            for (int i = 0; i < visibleSeries.Count; i++)
            {
                ordinalBySeries[visibleSeries[i]] = i;
            }

            using (context.PushClip(state.Layout.Plot))
            {
                for (int index = 0; index < state.Items.Count; index++)
                {
                    if (state.Items[index] is not ChartDataPipeline.SeriesDataPoint s)
                    {
                        continue;
                    }

                    if (s.PointIndex < start || s.PointIndex >= end ||
                        !ordinalBySeries.TryGetValue(s.SeriesIndex, out int ordinal))
                    {
                        continue;
                    }

                    double slotLeft = state.Layout.Plot.Left + ((s.PointIndex - start) * state.Layout.SlotWidth);
                    double x = slotLeft + groupOffset + (ordinal * (subWidth + gap));
                    DrawSingleBar(context, chart, state, animatedValues, index, s.PointIndex, subWidth, baseline, x);
                }
            }

            return;
        }

        // Stacked or Stacked100
        double stackWidth = ClampBarWidth(state.Layout.BarWidth, chart.MinBarWidth, chart.MaxBarWidth);
        bool is100 = chart.SeriesLayoutMode == ColumnSeriesLayoutMode.Stacked100;
        var posAccum = new double[state.CategoryCount];
        var negAccum = new double[state.CategoryCount];
        double[]? absTotals = null;

        if (is100)
        {
            absTotals = new double[state.CategoryCount];
            foreach (IChartDataPoint item in state.Items)
            {
                if (item is ChartDataPipeline.SeriesDataPoint s &&
                    s.PointIndex >= 0 && s.PointIndex < absTotals.Length &&
                    double.IsFinite(item.Value))
                {
                    absTotals[s.PointIndex] += Math.Abs(item.Value);
                }
            }
        }

        using (context.PushClip(state.Layout.Plot))
        {
            for (int index = 0; index < state.Items.Count; index++)
            {
                if (state.Items[index] is not ChartDataPipeline.SeriesDataPoint s ||
                    !double.IsFinite(s.Value) ||
                    index >= animatedValues.Count)
                {
                    continue;
                }

                int cat = s.PointIndex;
                if (cat < 0 || cat >= state.CategoryCount)
                {
                    continue;
                }

                double rawAnim = animatedValues[index];
                double val = is100 && absTotals![cat] > 0
                    ? (rawAnim / absTotals[cat]) * 100.0
                    : rawAnim;

                double baseVal = val >= 0 ? posAccum[cat] : negAccum[cat];
                double topVal = baseVal + val;
                if (val >= 0)
                {
                    posAccum[cat] = topVal;
                }
                else
                {
                    negAccum[cat] = topVal;
                }

                if (cat < start || cat >= end)
                {
                    continue;
                }

                double yBase = MapY(Math.Clamp(baseVal, state.Scale.Minimum, state.Scale.Maximum), state);
                double yTop = MapY(Math.Clamp(topVal, state.Scale.Minimum, state.Scale.Maximum), state);
                double x = GetSlotBarLeft(state.Layout, cat, stackWidth);
                var rect = new Rect(x, Math.Min(yTop, yBase), stackWidth, Math.Abs(yTop - yBase));
                DrawBarShapeAndSelection(context, chart, state, s, index, rect, val < 0, val);
            }
        }
    }

    private void DrawSingleBar(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues,
        int itemIndex,
        int categoryIndex,
        double barWidth,
        double baseline,
        double x)
    {
        IChartDataPoint item = state.Items[itemIndex];
        if (!double.IsFinite(item.Value) || itemIndex >= animatedValues.Count)
        {
            return;
        }

        double animatedVal = animatedValues[itemIndex];
        double valueY = MapY(
            Math.Clamp(animatedVal, state.Scale.Minimum, state.Scale.Maximum),
            state);
        var rect = new Rect(
            x,
            Math.Min(valueY, baseline),
            barWidth,
            Math.Abs(valueY - baseline));
        DrawBarShapeAndSelection(context, chart, state, item, itemIndex, rect, animatedVal < 0, animatedVal);
        _ = categoryIndex;
    }

    private void DrawBarShapeAndSelection(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state,
        IChartDataPoint item,
        int itemIndex,
        Rect rect,
        bool isNegative,
        double displayValue)
    {
        bool isSelected = chart.SelectedIndex == itemIndex || ReferenceEquals(chart.SelectedItem, item);
        IBrush? seriesSelectedBrush = (item as ChartDataPipeline.SeriesDataPoint)?.SelectedBrush;
        IBrush barBrush = isSelected && (seriesSelectedBrush != null || chart.SelectedBarBrush != null)
            ? seriesSelectedBrush ?? chart.SelectedBarBrush!
            : ResolveBarBrush(chart, state, item);

        DrawRect(context, barBrush, null, rect, chart.BarCornerRadius, isNegative);

        if (isSelected && chart.SelectionOverlayBrush != null)
        {
            DrawRect(context, chart.SelectionOverlayBrush, null, rect, chart.BarCornerRadius, isNegative);
        }

        if (isSelected && chart.SelectionStrokeThickness > 0)
        {
            double strokeThickness = Math.Max(0, chart.SelectionStrokeThickness);
            double inset = Math.Min(strokeThickness / 2, Math.Min(rect.Width, rect.Height) / 2);
            Pen pen = GetOrCreateSelectionPen(chart.SelectionStrokeBrush, strokeThickness);
            DrawRect(context, null, pen, rect.Deflate(inset), chart.BarCornerRadius, isNegative);
        }

        if (chart.ShowDataLabels)
        {
            DrawDataLabel(context, chart, state, rect, isNegative, displayValue);
        }
    }

    private void DrawDataLabel(
        DrawingContext context,
        ColumnChart chart,
        ChartRenderState state,
        Rect rect,
        bool isNegative,
        double value)
    {
        string textStr = chart.DataLabelFormatter?.Invoke(value) ?? ChartValueFormatter.Format(value);
        if (string.IsNullOrWhiteSpace(textStr))
        {
            return;
        }

        IBrush brush = chart.DataLabelBrush ?? chart.XAxisTextBrush;
        FormattedText text = FormatText(chart, textStr, chart.DataLabelFontSize, brush);
        double x = rect.Center.X - (text.Width / 2);
        double y = chart.DataLabelPosition switch
        {
            ColumnLabelPosition.Center => rect.Center.Y - (text.Height / 2),
            ColumnLabelPosition.InsideTop => isNegative
                ? Math.Max(rect.Top + 2, rect.Bottom - text.Height - 4)
                : Math.Min(rect.Bottom - text.Height - 2, rect.Top + 4),
            _ => isNegative
                ? Math.Min(state.Layout.Plot.Bottom - text.Height, rect.Bottom + 3)
                : Math.Max(state.Layout.Plot.Top, rect.Top - text.Height - 3),
        };
        context.DrawText(text, new Point(x, y));
    }

    private static void DrawRect(
        DrawingContext context,
        IBrush? fill,
        Pen? pen,
        Rect rect,
        CornerRadius cornerRadius,
        bool isNegative)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (cornerRadius == default)
        {
            context.DrawRectangle(fill, pen, rect);
            return;
        }

        double maxRadius = Math.Min(rect.Width / 2, rect.Height / 2);
        double tl = Math.Min(Math.Max(0, cornerRadius.TopLeft), maxRadius);
        double tr = Math.Min(Math.Max(0, cornerRadius.TopRight), maxRadius);
        double br = Math.Min(Math.Max(0, cornerRadius.BottomRight), maxRadius);
        double bl = Math.Min(Math.Max(0, cornerRadius.BottomLeft), maxRadius);

        if (isNegative)
        {
            (tl, bl) = (bl, tl);
            (tr, br) = (br, tr);
        }

        context.DrawRectangle(fill, pen, new RoundedRect(rect, tl, tr, br, bl));
    }

    private int[]? ResolveColumnIndices(ColumnChart chart, ChartRenderState state)
    {
        if (chart.DownsampleMode == ChartDownsampleMode.None || chart.DownsampleThreshold <= 0)
        {
            return null;
        }

        int start = state.Layout.ViewportStartIndex;
        int visibleCount = Math.Min(state.Items.Count - start, state.Layout.ViewportCount);
        if (visibleCount <= chart.DownsampleThreshold)
        {
            return null;
        }

        int targetCount = Math.Max(16, Math.Min((int)Math.Ceiling(state.Layout.Plot.Width * 1.5), chart.DownsampleThreshold));
        if (!ChartDownsampler.NeedsDownsampling(visibleCount, targetCount, chart.DownsampleMode))
        {
            return null;
        }

        var slice = new double[visibleCount];
        for (int index = 0; index < visibleCount; index++)
        {
            slice[index] = state.Items[start + index].Value;
        }

        int[] localIndices = ChartDownsampler.SelectIndices(slice, targetCount, chart.DownsampleMode);
        for (int index = 0; index < localIndices.Length; index++)
        {
            localIndices[index] += start;
        }

        return localIndices;
    }

    private static double ComputeDownsampledBarWidth(ColumnChart chart, ChartLayout layout, int visibleBars)
    {
        double raw = visibleBars > 0
            ? (layout.Plot.Width / visibleBars) * Math.Clamp(chart.BarWidthRatio, 0.05, 1.0)
            : layout.BarWidth;
        return ClampBarWidth(raw, chart.MinBarWidth, chart.MaxBarWidth);
    }

    private static List<int> GetVisibleSeriesOrder(IReadOnlyList<IChartDataPoint> items)
    {
        var set = new SortedSet<int>();
        foreach (IChartDataPoint item in items)
        {
            if (item is ChartDataPipeline.SeriesDataPoint s)
            {
                set.Add(s.SeriesIndex);
            }
        }

        return set.ToList();
    }

    private static (double SubWidth, double Gap, double GroupOffset) ComputeGroupedGeometry(
        ColumnChart chart,
        ChartLayout layout,
        int seriesCount)
    {
        int count = Math.Max(1, seriesCount);
        double minGroup = Math.Max(1.0, chart.MinBarWidth) * count;
        double maxGroup = double.IsFinite(chart.MaxBarWidth) ? chart.MaxBarWidth * count : double.PositiveInfinity;
        double groupWidth = ClampBarWidth(layout.BarWidth, minGroup, maxGroup);
        double spacingRatio = count > 1 ? Math.Clamp(chart.BarSpacingRatio, 0.0, 0.5) : 0.0;
        double totalGap = count > 1 ? groupWidth * spacingRatio : 0.0;
        double gap = count > 1 ? totalGap / (count - 1) : 0.0;
        double subWidth = Math.Max(0.5, (groupWidth - totalGap) / count);
        double actualGroupWidth = (subWidth * count) + (gap * (count - 1));
        double groupOffset = (layout.SlotWidth - actualGroupWidth) / 2.0;
        return (subWidth, gap, groupOffset);
    }

    private static double ClampBarWidth(double width, double minWidth, double maxWidth)
    {
        double min = double.IsFinite(minWidth) && minWidth > 0 ? minWidth : 1.0;
        double max = double.IsFinite(maxWidth) && maxWidth >= min ? maxWidth : double.PositiveInfinity;
        return Math.Clamp(width, min, max);
    }

    private static double GetSlotBarLeft(ChartLayout layout, int categoryIndex, double barWidth) =>
        layout.Plot.Left +
        ((categoryIndex - layout.ViewportStartIndex) * layout.SlotWidth) +
        ((layout.SlotWidth - barWidth) / 2);

    private static IBrush ResolveBarBrush(
        ColumnChart chart,
        ChartRenderState state,
        IChartDataPoint item)
    {
        if (item.Brush != null || !chart.ShowThresholds)
        {
            return item.Brush ?? (item as ChartDataPipeline.SeriesDataPoint)?.LineBrush ?? chart.DefaultBrush;
        }

        return ChartThresholdResolver.Resolve(
            item.Value,
            (item as ChartDataPipeline.SeriesDataPoint)?.LineBrush ?? chart.DefaultBrush,
            state.Thresholds,
            chart.ThresholdDirection);
    }

    private Pen GetOrCreateSelectionPen(IBrush brush, double thickness)
    {
        if (_cachedSelectionPen != null &&
            ReferenceEquals(_cachedSelectionBrush, brush) &&
            _cachedSelectionThickness.Equals(thickness))
        {
            return _cachedSelectionPen;
        }

        _cachedSelectionBrush = brush;
        _cachedSelectionThickness = thickness;
        _cachedSelectionPen = new Pen(brush, thickness);
        return _cachedSelectionPen;
    }
}
