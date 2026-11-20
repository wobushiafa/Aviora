using Avalonia;
using Avalonia.Media;
using Aviora.Core.Charts;

namespace Aviora.Controls;

internal abstract class CartesianChartRenderer<TChart>
    where TChart : CartesianChart
{
    private readonly ChartTextLayout _textLayout = new();
    private readonly Dictionary<int, Pen> _thresholdPens = [];
    private ChartRenderState? _state;
    private XAxisLabelCache? _xAxisLabelCache;
    private Pen? _cachedGridPen;
    private Pen? _cachedVerticalGridPen;
    private Pen? _cachedAxisPen;
    private Pen? _cachedZeroPen;
    private Pen? _cachedCrosshairPen;

    public int StateBuildCount { get; private set; }

    public virtual void Invalidate() => Invalidate(ChartInvalidationFlags.All);

    public virtual void Invalidate(ChartInvalidationFlags flags)
    {
        if ((flags & ChartInvalidationFlags.TextCache) != 0)
        {
            _textLayout.Clear();
            _xAxisLabelCache = null;
        }

        if ((flags & ChartInvalidationFlags.LayoutAndScale) != 0)
        {
            _state = null;
            _xAxisLabelCache = null;
        }

        if ((flags & ChartInvalidationFlags.VisualOnly) != 0 ||
            (flags & ChartInvalidationFlags.LayoutAndScale) != 0)
        {
            _cachedGridPen = null;
            _cachedVerticalGridPen = null;
            _cachedAxisPen = null;
            _cachedZeroPen = null;
            _cachedCrosshairPen = null;
            _thresholdPens.Clear();
        }
    }

    public Rect GetPlotBounds(TChart chart, IReadOnlyList<IChartDataPoint> items) =>
        EnsureState(chart, items).Layout.Plot;

    public int GetCategoryCount(TChart chart, IReadOnlyList<IChartDataPoint> items) =>
        EnsureState(chart, items).CategoryCount;

    public IReadOnlyList<Rect> GetLegendItemBounds(TChart chart, IReadOnlyList<IChartDataPoint> items) =>
        EnsureState(chart, items).LegendItems.Select(item => item.Bounds).ToArray();

    public bool TryToggleLegendAt(TChart chart, IReadOnlyList<IChartDataPoint> items, Point point)
    {
        ChartRenderState state = EnsureState(chart, items);
        foreach (LegendHitItem legendItem in state.LegendItems)
        {
            if (legendItem.Bounds.Contains(point) && legendItem.SeriesObject != null)
            {
                if (legendItem.SeriesObject is LineChartSeries lineSeries)
                {
                    lineSeries.IsVisible = !lineSeries.IsVisible;
                    return true;
                }

                if (legendItem.SeriesObject is ColumnChartSeries colSeries)
                {
                    colSeries.IsVisible = !colSeries.IsVisible;
                    return true;
                }
            }
        }

        return false;
    }

    public void Render(
        DrawingContext context,
        TChart chart,
        IReadOnlyList<IChartDataPoint> items,
        IReadOnlyList<double> animatedValues)
    {
        if (chart.Bounds.Width > 0 && chart.Bounds.Height > 0)
        {
            context.FillRectangle(Brushes.Transparent, new Rect(chart.Bounds.Size));
        }

        ChartRenderState state = EnsureState(chart, items);
        DrawLegend(context, chart, state);

        if (state.Layout.Plot.Width <= 0 || state.Layout.Plot.Height <= 0)
        {
            return;
        }

        var plotContext = new ChartPlotContext(
            state.Layout.Plot,
            state.Scale,
            state.CategoryCount,
            state.Layout.ViewportStartIndex,
            state.Layout.ViewportCount,
            state.Layout.SlotWidth);

        DrawPlotSurface(context, chart, state);
        if (chart.PlotBackgroundRenderer != null)
        {
            using (context.PushClip(state.Layout.Plot))
            {
                chart.PlotBackgroundRenderer(context, plotContext);
            }
        }

        DrawChartBackground(context, chart, state);
        DrawThresholdBands(context, chart, state);
        DrawGridLines(context, chart, state);
        DrawYAxisLabels(context, chart, state);

        if (state.Items.Count == 0)
        {
            DrawEmptyState(context, chart, state);
            return;
        }

        DrawSeries(context, chart, state, animatedValues);
        DrawThresholdLines(context, chart, state);
        DrawAxisLines(context, chart, state);
        DrawXAxisLabels(context, chart, state);
        DrawCrosshair(context, chart, state, animatedValues);

        if (chart.PlotOverlayRenderer != null)
        {
            using (context.PushClip(state.Layout.Plot))
            {
                chart.PlotOverlayRenderer(context, plotContext);
            }
        }
    }

    public virtual int HitTest(TChart chart, IReadOnlyList<IChartDataPoint> items, Point point)
    {
        ChartRenderState state = EnsureState(chart, items);
        return ChartLayoutCalculator.HitTest(state.Layout, point, state.CategoryCount);
    }

    public virtual int HitTestCategory(TChart chart, IReadOnlyList<IChartDataPoint> items, Point point)
    {
        ChartRenderState state = EnsureState(chart, items);
        return ChartLayoutCalculator.HitTest(state.Layout, point, state.CategoryCount);
    }

    protected abstract void DrawSeries(
        DrawingContext context,
        TChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues);

    protected virtual void DrawChartBackground(
        DrawingContext context,
        TChart chart,
        ChartRenderState state)
    {
    }

    protected virtual Point? ResolveCrosshairPoint(
        TChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues,
        int hoveredItemIndex)
    {
        if (hoveredItemIndex < 0 || hoveredItemIndex >= state.Items.Count)
        {
            return null;
        }

        int categoryIndex = state.Items[hoveredItemIndex] is ChartDataPipeline.SeriesDataPoint seriesPoint
            ? seriesPoint.PointIndex
            : hoveredItemIndex;
        if (categoryIndex < state.Layout.ViewportStartIndex ||
            categoryIndex >= state.Layout.ViewportStartIndex + state.Layout.ViewportCount)
        {
            return null;
        }

        double val = hoveredItemIndex < animatedValues.Count && double.IsFinite(animatedValues[hoveredItemIndex])
            ? animatedValues[hoveredItemIndex]
            : state.Items[hoveredItemIndex].Value;
        if (!double.IsFinite(val))
        {
            return null;
        }

        double x = GetItemCenterX(state.Layout, categoryIndex);
        double y = MapY(Math.Clamp(val, state.Scale.Minimum, state.Scale.Maximum), state);
        return new Point(x, y);
    }

    protected virtual double GetItemWidthRatio(TChart chart) => 1;

    protected virtual int ResolveCategoryCount(TChart chart, IReadOnlyList<IChartDataPoint> items) => items.Count;

    protected virtual IEnumerable<double> GetScaleValues(TChart chart, IReadOnlyList<IChartDataPoint> items) =>
        items.Select(item => item.Value);

    protected virtual IReadOnlyList<string> GetDefaultXAxisLabels(IReadOnlyList<IChartDataPoint> items) =>
        items.Select(item => item.Label ?? string.Empty).ToList();

    protected static double GetItemCenterX(ChartLayout layout, int categoryIndex) =>
        layout.Plot.Left + ((categoryIndex - layout.ViewportStartIndex) * layout.SlotWidth) + (layout.SlotWidth / 2);

    protected static double MapY(double value, ChartRenderState state) =>
        ChartLayoutCalculator.MapY(value, state.Layout.Plot, state.Scale);

    protected FormattedText FormatText(TChart chart, string value, double fontSize, IBrush brush) =>
        _textLayout.Format(value, fontSize, brush, chart.FontFamily, chart.FontStyle, chart.FontWeight);

    protected ChartRenderState EnsureState(TChart chart, IReadOnlyList<IChartDataPoint> items)
    {
        if (_state != null)
        {
            return _state;
        }

        IReadOnlyList<ChartThreshold> thresholds = chart.Thresholds?.ToList() ?? [];
        IReadOnlyList<ChartDataPipeline.ChartSeriesMeta> seriesMeta = chart.GetSeriesMetadata();
        int categoryCount = ResolveCategoryCount(chart, items);

        (double legendTopHeight, double legendBottomHeight, LegendHitItem[] legendItems) =
            BuildLegendLayout(chart, seriesMeta);

        ChartLayout layout = CalculateLayout(
            chart,
            categoryCount,
            thresholds,
            legendTopHeight,
            legendBottomHeight);

        ChartAxisScale scale = ChartAxisCalculator.Calculate(
            GetScaleValues(chart, items),
            chart.AutoRange,
            chart.MinValue,
            chart.MaxValue,
            chart.GridLineCount,
            chart.AutoRangePaddingRatio,
            chart.AutoRangeIncludeZero);

        List<string> yLabels = ChartDataPipeline.ResolveLabels(chart.YAxisLabelsSource, chart.YAxisLabels);
        if (yLabels.Count == 0)
        {
            Func<double, string> formatter = chart.YAxisLabelFormatter ?? ChartValueFormatter.Format;
            yLabels = scale.Ticks.Select(formatter).ToList();
        }

        List<string> xLabels = ChartDataPipeline.ResolveLabels(chart.XAxisLabelsSource, chart.XAxisLabels);
        if (xLabels.Count == 0)
        {
            xLabels = GetDefaultXAxisLabels(items).ToList();
        }

        _state = new ChartRenderState
        {
            Items = items,
            CategoryCount = categoryCount,
            Thresholds = thresholds,
            Layout = layout,
            Scale = scale,
            YAxisLabels = yLabels,
            XAxisLabels = xLabels,
            SeriesMeta = seriesMeta,
            LegendItems = legendItems,
        };
        StateBuildCount++;
        return _state;
    }

    private (double TopHeight, double BottomHeight, LegendHitItem[] Items) BuildLegendLayout(
        TChart chart,
        IReadOnlyList<ChartDataPipeline.ChartSeriesMeta> seriesMeta)
    {
        if (!chart.ShowLegend || seriesMeta.Count == 0 || chart.Bounds.Width <= 0)
        {
            return (0, 0, []);
        }

        const double swatchSize = 10;
        const double swatchGap = 6;
        const double itemGap = 16;
        const double rowGap = 4;

        double maxWidth = Math.Max(40, chart.Bounds.Width - 16);
        var entries = new List<(ChartDataPipeline.ChartSeriesMeta Meta, FormattedText Text, double Width, double Height)>(seriesMeta.Count);
        double rowHeight = 16;

        foreach (ChartDataPipeline.ChartSeriesMeta meta in seriesMeta)
        {
            FormattedText text = FormatText(chart, meta.Title, chart.LegendFontSize, chart.LegendTextBrush);
            double itemWidth = swatchSize + swatchGap + text.Width;
            rowHeight = Math.Max(rowHeight, text.Height + 4);
            entries.Add((meta, text, itemWidth, Math.Max(swatchSize, text.Height)));
        }

        var rows = new List<List<(ChartDataPipeline.ChartSeriesMeta Meta, FormattedText Text, double Width, double Height)>>();
        var currentRow = new List<(ChartDataPipeline.ChartSeriesMeta Meta, FormattedText Text, double Width, double Height)>();
        double currentWidth = 0;

        foreach (var entry in entries)
        {
            double added = currentRow.Count == 0 ? entry.Width : itemGap + entry.Width;
            if (currentRow.Count > 0 && currentWidth + added > maxWidth)
            {
                rows.Add(currentRow);
                currentRow = [entry];
                currentWidth = entry.Width;
            }
            else
            {
                currentRow.Add(entry);
                currentWidth += added;
            }
        }

        if (currentRow.Count > 0)
        {
            rows.Add(currentRow);
        }

        double totalHeight = (rows.Count * rowHeight) + (Math.Max(0, rows.Count - 1) * rowGap);
        double startY = chart.LegendPlacement == ChartLegendPlacement.Bottom
            ? Math.Max(0, chart.Bounds.Height - totalHeight - 2)
            : 2;

        var hitItems = new List<LegendHitItem>(entries.Count);
        double y = startY;
        foreach (var row in rows)
        {
            double rowWidth = row.Sum(r => r.Width) + (Math.Max(0, row.Count - 1) * itemGap);
            double x = Math.Max(8, (chart.Bounds.Width - rowWidth) / 2);

            foreach (var item in row)
            {
                double swatchY = y + ((rowHeight - swatchSize) / 2);
                double textY = y + ((rowHeight - item.Text.Height) / 2);
                var swatchRect = new Rect(x, swatchY, swatchSize, swatchSize);
                var textPos = new Point(x + swatchSize + swatchGap, textY);
                var bounds = new Rect(x - 2, y, item.Width + 4, rowHeight);

                hitItems.Add(new LegendHitItem(
                    item.Meta.SeriesIndex,
                    item.Meta.Title,
                    item.Meta.Brush,
                    item.Meta.IsVisible,
                    item.Meta.SeriesObject,
                    bounds,
                    item.Text,
                    textPos,
                    swatchRect));

                x += item.Width + itemGap;
            }

            y += rowHeight + rowGap;
        }

        double reserved = totalHeight + 6;
        return chart.LegendPlacement == ChartLegendPlacement.Bottom
            ? (0, reserved, hitItems.ToArray())
            : (reserved, 0, hitItems.ToArray());
    }

    private ChartLayout CalculateLayout(
        TChart chart,
        int itemCount,
        IReadOnlyList<ChartThreshold> thresholds,
        double legendTopHeight,
        double legendBottomHeight)
    {
        double requiredYAxisWidth = 0;
        if (chart.ShowYAxis && chart.ShowThresholds && chart.ShowThresholdLabels)
        {
            requiredYAxisWidth = thresholds
                .Where(item => !string.IsNullOrWhiteSpace(item.Label))
                .Select(item => FormatText(
                    chart,
                    item.Label!,
                    chart.ThresholdLabelFontSize,
                    item.LabelBrush ?? item.Brush).Width + 8)
                .DefaultIfEmpty(0)
                .Max();
        }

        double verticalInset = 4;
        if (chart.ShowYAxis && Math.Max(chart.YAxisWidth, requiredYAxisWidth) > 0)
        {
            double labelHeight = FormatText(chart, "0", chart.YAxisFontSize, chart.YAxisTextBrush).Height;
            if (chart.ShowThresholds && chart.ShowThresholdLabels)
            {
                double thresholdLabelHeight = thresholds
                    .Where(item => !string.IsNullOrWhiteSpace(item.Label))
                    .Select(item => FormatText(
                        chart,
                        item.Label!,
                        chart.ThresholdLabelFontSize,
                        item.LabelBrush ?? item.Brush).Height)
                    .DefaultIfEmpty(0)
                    .Max();
                labelHeight = Math.Max(labelHeight, thresholdLabelHeight);
            }

            verticalInset = Math.Max(verticalInset, labelHeight / 2);
        }

        var layoutOptions = new ChartLayoutOptions(
            chart.Bounds.Size,
            itemCount,
            chart.ShowXAxis,
            chart.XAxisHeight,
            chart.ShowYAxis,
            chart.YAxisWidth,
            GetItemWidthRatio(chart),
            verticalInset,
            requiredYAxisWidth);

        return ChartLayoutCalculator.Calculate(
            layoutOptions,
            chart.PlotPadding,
            legendTopHeight,
            legendBottomHeight,
            chart.ZoomStartRatio,
            chart.ZoomEndRatio,
            chart.MinVisiblePoints);
    }

    private static void DrawLegend(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowLegend || state.LegendItems.Count == 0)
        {
            return;
        }

        foreach (LegendHitItem item in state.LegendItems)
        {
            using IDisposable? opacity = item.IsVisible ? null : context.PushOpacity(0.38);
            context.DrawRectangle(item.Brush, null, new RoundedRect(item.SwatchRect, 2.5));
            context.DrawText(item.Text, item.TextPosition);
        }
    }

    private static void DrawPlotSurface(DrawingContext context, TChart chart, ChartRenderState state)
    {
        bool hasBackground = chart.PlotBackground != null;
        bool hasBorder = chart.PlotBorderBrush != null && chart.PlotBorderThickness != default;
        if (!hasBackground && !hasBorder)
        {
            return;
        }

        Pen? borderPen = null;
        if (hasBorder)
        {
            double thickness = Math.Max(
                Math.Max(chart.PlotBorderThickness.Left, chart.PlotBorderThickness.Top),
                Math.Max(chart.PlotBorderThickness.Right, chart.PlotBorderThickness.Bottom));
            if (double.IsFinite(thickness) && thickness > 0)
            {
                borderPen = new Pen(chart.PlotBorderBrush, thickness);
            }
        }

        if (chart.PlotCornerRadius != default)
        {
            context.DrawRectangle(
                chart.PlotBackground,
                borderPen,
                new RoundedRect(state.Layout.Plot, chart.PlotCornerRadius));
        }
        else
        {
            context.DrawRectangle(chart.PlotBackground, borderPen, state.Layout.Plot);
        }
    }

    private static void DrawThresholdBands(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowThresholds || state.Thresholds.Count == 0)
        {
            return;
        }

        using (context.PushClip(state.Layout.Plot))
        {
            foreach (ChartThreshold threshold in state.Thresholds)
            {
                if (threshold.BandBrush == null || !double.IsFinite(threshold.Value))
                {
                    continue;
                }

                double endValue = double.IsFinite(threshold.BandEndValue)
                    ? threshold.BandEndValue
                    : chart.ThresholdDirection == ThresholdDirection.HigherIsMoreSevere
                        ? state.Scale.Maximum
                        : state.Scale.Minimum;

                double minVal = Math.Min(threshold.Value, endValue);
                double maxVal = Math.Max(threshold.Value, endValue);
                double clampedMin = Math.Clamp(minVal, state.Scale.Minimum, state.Scale.Maximum);
                double clampedMax = Math.Clamp(maxVal, state.Scale.Minimum, state.Scale.Maximum);
                if (clampedMax <= clampedMin)
                {
                    continue;
                }

                double top = MapY(clampedMax, state);
                double bottom = MapY(clampedMin, state);
                context.DrawRectangle(
                    threshold.BandBrush,
                    null,
                    new Rect(state.Layout.Plot.Left, top, state.Layout.Plot.Width, Math.Max(0, bottom - top)));
            }
        }
    }

    private void DrawGridLines(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (chart.ShowGridLines && state.GridLineCount >= 2)
        {
            Pen gridPen = GetOrCreateGridPen(chart);
            for (int index = 0; index < state.GridLineCount; index++)
            {
                double y = state.Layout.Plot.Top +
                           (index * state.Layout.Plot.Height / (state.GridLineCount - 1));
                context.DrawLine(
                    gridPen,
                    new Point(state.Layout.Plot.Left, y),
                    new Point(state.Layout.Plot.Right, y));
            }
        }

        if (chart.ShowVerticalGridLines && state.Layout.ViewportCount > 0)
        {
            Pen vPen = GetOrCreateVerticalGridPen(chart);
            int visible = state.Layout.ViewportCount;
            int step = Math.Max(1, visible / 24);
            for (int offset = 0; offset < visible; offset += step)
            {
                double x = state.Layout.Plot.Left + (offset * state.Layout.SlotWidth) + (state.Layout.SlotWidth / 2);
                context.DrawLine(
                    vPen,
                    new Point(x, state.Layout.Plot.Top),
                    new Point(x, state.Layout.Plot.Bottom));
            }
        }

        if (chart.ShowZeroBaseline &&
            state.Scale.Minimum <= 0 &&
            state.Scale.Maximum >= 0)
        {
            double zeroY = MapY(0, state);
            Pen zeroPen = GetOrCreateZeroPen(chart);
            context.DrawLine(
                zeroPen,
                new Point(state.Layout.Plot.Left, zeroY),
                new Point(state.Layout.Plot.Right, zeroY));
        }
    }

    private void DrawAxisLines(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowAxisLines)
        {
            return;
        }

        Pen axisPen = GetOrCreateAxisPen(chart);
        if (chart.ShowYAxis)
        {
            context.DrawLine(
                axisPen,
                new Point(state.Layout.Plot.Left, state.Layout.Plot.Top),
                new Point(state.Layout.Plot.Left, state.Layout.Plot.Bottom));
        }

        if (chart.ShowXAxis)
        {
            context.DrawLine(
                axisPen,
                new Point(state.Layout.Plot.Left, state.Layout.Plot.Bottom),
                new Point(state.Layout.Plot.Right, state.Layout.Plot.Bottom));
        }
    }

    private void DrawThresholdLines(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowThresholds)
        {
            return;
        }

        for (int index = 0; index < state.Thresholds.Count; index++)
        {
            ChartThreshold threshold = state.Thresholds[index];
            if (!double.IsFinite(threshold.Value) ||
                threshold.Value < state.Scale.Minimum ||
                threshold.Value > state.Scale.Maximum)
            {
                continue;
            }

            if (!_thresholdPens.TryGetValue(index, out Pen? pen))
            {
                double thickness = double.IsFinite(threshold.StrokeThickness) && threshold.StrokeThickness > 0
                    ? threshold.StrokeThickness
                    : 1.2;
                DashStyle? dashStyle = threshold.DashArray is { Count: > 0 }
                    ? new DashStyle(threshold.DashArray, 0)
                    : null;
                pen = new Pen(threshold.Brush, thickness, dashStyle);
                _thresholdPens[index] = pen;
            }

            double y = MapY(threshold.Value, state);
            context.DrawLine(
                pen,
                new Point(state.Layout.Plot.Left, y),
                new Point(state.Layout.Plot.Right, y));
            DrawThresholdLabel(context, chart, state, threshold, y);
        }
    }

    private void DrawThresholdLabel(
        DrawingContext context,
        TChart chart,
        ChartRenderState state,
        ChartThreshold threshold,
        double targetY)
    {
        if (!chart.ShowYAxis || !chart.ShowThresholdLabels || state.Layout.Plot.Left <= 0 ||
            string.IsNullOrWhiteSpace(threshold.Label))
        {
            return;
        }

        IBrush brush = threshold.LabelBrush ?? threshold.Brush;
        FormattedText text = FormatText(chart, threshold.Label, chart.ThresholdLabelFontSize, brush);
        double y = Math.Clamp(
            targetY - (text.Height / 2),
            0,
            Math.Max(0, chart.Bounds.Height - text.Height));
        double x = Math.Max(0, state.Layout.Plot.Left - text.Width - 4);
        context.DrawText(text, new Point(x, y));
    }

    private void DrawYAxisLabels(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowYAxis || state.Layout.Plot.Left <= 0)
        {
            return;
        }

        for (int index = 0; index < state.YAxisLabels.Count; index++)
        {
            string label = state.YAxisLabels[index];
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            FormattedText text = FormatText(chart, label, chart.YAxisFontSize, chart.YAxisTextBrush);
            double targetY = state.YAxisLabels.Count == 1
                ? state.Layout.Plot.Center.Y
                : state.Layout.Plot.Top +
                  (index * state.Layout.Plot.Height / (state.YAxisLabels.Count - 1));
            if (OverlapsThresholdLabel(chart, state, targetY, text.Height))
            {
                continue;
            }

            // Center the visible glyph bounds on the grid line. Height describes the
            // line box, while Extent and OverhangAfter describe the rendered ink.
            double glyphCenterFromOrigin =
                text.Height + text.OverhangAfter - (text.Extent / 2);
            double y = Math.Clamp(
                targetY - glyphCenterFromOrigin,
                0,
                Math.Max(0, chart.Bounds.Height - text.Height));
            context.DrawText(
                text,
                new Point(Math.Max(0, state.Layout.Plot.Left - text.Width - 4), y));
        }
    }

    private bool OverlapsThresholdLabel(
        TChart chart,
        ChartRenderState state,
        double targetY,
        double textHeight)
    {
        if (!chart.ShowThresholds || !chart.ShowThresholdLabels)
        {
            return false;
        }

        foreach (ChartThreshold threshold in state.Thresholds)
        {
            if (string.IsNullOrWhiteSpace(threshold.Label) ||
                !double.IsFinite(threshold.Value) ||
                threshold.Value < state.Scale.Minimum ||
                threshold.Value > state.Scale.Maximum)
            {
                continue;
            }

            FormattedText thresholdText = FormatText(
                chart,
                threshold.Label,
                chart.ThresholdLabelFontSize,
                threshold.LabelBrush ?? threshold.Brush);
            double thresholdY = MapY(threshold.Value, state);
            if (Math.Abs(thresholdY - targetY) < ((thresholdText.Height + textHeight) / 2) + 2)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawXAxisLabels(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowXAxis || chart.XAxisHeight <= 0 || state.Items.Count == 0 ||
            state.XAxisLabels.Count == 0 || state.Layout.ViewportCount == 0)
        {
            return;
        }

        XAxisLabelCache cache = GetXAxisLabelCache(chart, state);
        foreach (int index in cache.VisibleIndices)
        {
            XAxisLabelPlacement placement = cache.Placements[index];
            context.DrawText(placement.Text, new Point(placement.Left, state.Layout.XAxisTop));
        }
    }

    private XAxisLabelCache GetXAxisLabelCache(TChart chart, ChartRenderState state)
    {
        if (_xAxisLabelCache?.State == state)
        {
            return _xAxisLabelCache;
        }

        int viewStart = state.Layout.ViewportStartIndex;
        int viewEnd = viewStart + state.Layout.ViewportCount - 1;

        var placements = new List<XAxisLabelPlacement>(Math.Min(state.XAxisLabels.Count, state.Layout.ViewportCount));
        for (int index = 0; index < state.XAxisLabels.Count; index++)
        {
            double itemIndex = state.XAxisLabels.Count == state.CategoryCount
                ? index
                : state.XAxisLabels.Count == 1
                    ? (state.CategoryCount - 1) / 2.0
                    : index * (state.CategoryCount - 1.0) / (state.XAxisLabels.Count - 1);

            if (itemIndex < viewStart - 0.5 || itemIndex > viewEnd + 0.5)
            {
                continue;
            }

            string label = state.XAxisLabels[index];
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            FormattedText text = FormatText(chart, label, chart.XAxisFontSize, chart.XAxisTextBrush);
            double center = state.Layout.Plot.Left +
                            ((itemIndex - viewStart) * state.Layout.SlotWidth) +
                            (state.Layout.SlotWidth / 2);
            double left = Math.Clamp(
                center - (text.Width / 2),
                state.Layout.Plot.Left,
                Math.Max(state.Layout.Plot.Left, state.Layout.Plot.Right - text.Width));
            placements.Add(new XAxisLabelPlacement(text, left));
        }

        IReadOnlyList<int> visible = ChartAxisCalculator.SelectLabelIndices(
            placements.Select(item => item.Left).ToList(),
            placements.Select(item => item.Right).ToList(),
            chart.XAxisLabelMode,
            chart.XAxisLabelInterval);
        _xAxisLabelCache = new XAxisLabelCache(state, placements.ToArray(), visible.ToArray());
        return _xAxisLabelCache;
    }

    private void DrawCrosshair(
        DrawingContext context,
        TChart chart,
        ChartRenderState state,
        IReadOnlyList<double> animatedValues)
    {
        if (chart.CrosshairMode == ChartCrosshairMode.None || chart.HoverPosition is not { } hoverPos)
        {
            return;
        }

        Point crosshairPos = hoverPos;
        if (chart.SnapCrosshairToDataPoint &&
            ResolveCrosshairPoint(chart, state, animatedValues, chart.HoveredItemIndex) is { } snapped)
        {
            crosshairPos = snapped;
        }

        if (!state.Layout.Plot.Contains(crosshairPos))
        {
            return;
        }

        Pen pen = GetOrCreateCrosshairPen(chart);
        if (chart.CrosshairMode is ChartCrosshairMode.Vertical or ChartCrosshairMode.Both)
        {
            context.DrawLine(
                pen,
                new Point(crosshairPos.X, state.Layout.Plot.Top),
                new Point(crosshairPos.X, state.Layout.Plot.Bottom));
        }

        if (chart.CrosshairMode is ChartCrosshairMode.Horizontal or ChartCrosshairMode.Both)
        {
            context.DrawLine(
                pen,
                new Point(state.Layout.Plot.Left, crosshairPos.Y),
                new Point(state.Layout.Plot.Right, crosshairPos.Y));
        }
    }

    private void DrawEmptyState(DrawingContext context, TChart chart, ChartRenderState state)
    {
        if (!chart.ShowEmptyText || string.IsNullOrWhiteSpace(chart.EmptyText))
        {
            return;
        }

        FormattedText text = FormatText(chart, chart.EmptyText, chart.EmptyTextFontSize, chart.EmptyTextBrush);
        context.DrawText(
            text,
            new Point(
                state.Layout.Plot.Center.X - (text.Width / 2),
                state.Layout.Plot.Center.Y - (text.Height / 2)));
    }

    private Pen GetOrCreateGridPen(TChart chart) =>
        _cachedGridPen ??= BuildPen(chart.GridLineBrush, chart.GridLineThickness, chart.GridLineDashArray);

    private Pen GetOrCreateVerticalGridPen(TChart chart) =>
        _cachedVerticalGridPen ??= BuildPen(
            chart.VerticalGridLineBrush ?? chart.GridLineBrush,
            chart.GridLineThickness,
            chart.GridLineDashArray);

    private Pen GetOrCreateAxisPen(TChart chart) =>
        _cachedAxisPen ??= BuildPen(chart.AxisLineBrush, chart.AxisLineThickness, null);

    private Pen GetOrCreateZeroPen(TChart chart) =>
        _cachedZeroPen ??= BuildPen(
            chart.ZeroBaselineBrush ?? chart.AxisLineBrush,
            chart.ZeroBaselineThickness,
            null);

    private Pen GetOrCreateCrosshairPen(TChart chart) =>
        _cachedCrosshairPen ??= BuildPen(chart.CrosshairBrush, chart.CrosshairThickness, chart.CrosshairDashArray);

    private static Pen BuildPen(IBrush brush, double thickness, IReadOnlyList<double>? dashArray)
    {
        double normalizedThickness = double.IsFinite(thickness) && thickness > 0 ? thickness : 1.0;
        DashStyle? dash = dashArray is { Count: > 0 } ? new DashStyle(dashArray, 0) : null;
        return new Pen(brush, normalizedThickness, dash);
    }

    private readonly record struct XAxisLabelPlacement(FormattedText Text, double Left)
    {
        public double Right => Left + Text.Width;
    }

    private sealed record XAxisLabelCache(
        ChartRenderState State,
        XAxisLabelPlacement[] Placements,
        int[] VisibleIndices);
}
