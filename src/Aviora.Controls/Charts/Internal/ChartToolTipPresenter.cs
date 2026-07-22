using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Aviora.Controls;

internal sealed class ChartToolTipPresenter
{
    private readonly CartesianChart _owner;
    private readonly ContentControl _content = new() { ClipToBounds = true };
    private readonly ChartToolTipState _state = new();
    private DispatcherTimer? _hideTimer;
    private bool _reevaluatePointerOnHide;
    private int _displayedIndex = -1;
    private IChartDataPoint? _displayedItem;
    private object? _displayedContent;
    private object? _displayedTemplate;
    private bool _styleNeedsMeasure;

    public ChartToolTipPresenter(CartesianChart owner)
    {
        _owner = owner;
        Visual = new Border
        {
            Child = _content,
            IsHitTestVisible = false,
            IsVisible = false,
        };
        ApplyStyle();
    }

    public Border Visual { get; }

    public static bool IsProperty(AvaloniaProperty property) =>
        property == CartesianChart.IsToolTipEnabledProperty ||
        property == CartesianChart.ToolTipBackgroundProperty ||
        property == CartesianChart.ToolTipTextBrushProperty ||
        property == CartesianChart.ToolTipFontSizeProperty ||
        property == CartesianChart.ToolTipTemplateProperty ||
        property == CartesianChart.ToolTipPaddingProperty ||
        property == CartesianChart.ToolTipCornerRadiusProperty ||
        property == CartesianChart.ToolTipBorderBrushProperty ||
        property == CartesianChart.ToolTipBorderThicknessProperty ||
        property == CartesianChart.ToolTipBoxShadowProperty ||
        property == CartesianChart.ToolTipHorizontalOffsetProperty ||
        property == CartesianChart.ToolTipVerticalOffsetProperty ||
        property == CartesianChart.ToolTipHideDelayProperty ||
        property == CartesianChart.ToolTipFormatterProperty;

    public bool Update(int index, Point pointerPosition)
    {
        if (index >= 0)
        {
            CancelHide();
        }

        return _state.Update(index, pointerPosition);
    }

    public void Reevaluate(int index, Point pointerPosition, IReadOnlyList<IChartDataPoint> items)
    {
        if (index >= 0)
        {
            CancelHide();
            _state.Update(index, pointerPosition);
            Refresh(items);
            return;
        }

        _state.Update(-1, pointerPosition);
        HideNow();
    }

    public bool Clear() => _state.Clear();

    public void Normalize(int itemCount) => _state.Normalize(itemCount);

    public void ApplyStyle()
    {
        Visual.Background = _owner.ToolTipBackground;
        Visual.BorderBrush = _owner.ToolTipBorderBrush;
        Visual.BorderThickness = _owner.ToolTipBorderThickness;
        Visual.CornerRadius = _owner.ToolTipCornerRadius;
        Visual.Padding = _owner.ToolTipPadding;
        Visual.BoxShadow = _owner.ToolTipBoxShadow;
        _content.Foreground = _owner.ToolTipTextBrush;
        _content.FontSize = NormalizePositive(_owner.ToolTipFontSize, 11);
        _styleNeedsMeasure = true;
    }

    public void Refresh(IReadOnlyList<IChartDataPoint> items)
    {
        int index = _state.HoveredIndex;
        if (!_owner.IsToolTipEnabled || index < 0 || index >= items.Count)
        {
            if (!_owner.IsToolTipEnabled)
            {
                Hide();
            }
            else
            {
                RequestHide(reevaluatePointer: true);
            }
            return;
        }

        IChartDataPoint item = items[index];
        object content;
        object? template;
        if (_owner.ToolTipTemplate != null)
        {
            content = item;
            template = _owner.ToolTipTemplate;
        }
        else
        {
            string text = _owner.ToolTipFormatter?.Invoke(item) ??
                          item.ToolTip ??
                          BuildDefaultContent(item);
            if (string.IsNullOrWhiteSpace(text))
            {
                Hide();
                return;
            }

            content = text;
            template = null;
        }

        bool contentChanged = index != _displayedIndex ||
                              !ReferenceEquals(item, _displayedItem) ||
                              !Equals(content, _displayedContent) ||
                              !ReferenceEquals(template, _displayedTemplate);
        bool needsMeasure = contentChanged || !Visual.IsVisible || _styleNeedsMeasure;
        if (contentChanged)
        {
            _content.ContentTemplate = _owner.ToolTipTemplate;
            _content.Content = content;
            _displayedIndex = index;
            _displayedItem = item;
            _displayedContent = content;
            _displayedTemplate = template;
        }

        Visual.IsVisible = true;
        if (needsMeasure)
        {
            MeasureAndArrange();
        }
    }

    public void Hide()
    {
        CancelHide();
        HideNow();
    }

    public void RequestHide(bool reevaluatePointer)
    {
        TimeSpan delay = _owner.ToolTipHideDelay;
        if (delay <= TimeSpan.Zero)
        {
            Hide();
            return;
        }

        _reevaluatePointerOnHide = reevaluatePointer;

        if (_hideTimer == null)
        {
            _hideTimer = new DispatcherTimer(delay, DispatcherPriority.Normal, HideTimerTick);
        }
        else
        {
            _hideTimer.Interval = delay;
            _hideTimer.Stop();
        }

        _hideTimer.Start();
    }

    private void HideTimerTick(object? sender, EventArgs e)
    {
        bool reevaluatePointer = _reevaluatePointerOnHide;
        CancelHide();
        if (reevaluatePointer)
        {
            _owner.ReevaluateToolTip();
        }
        else
        {
            HideNow();
        }
    }

    private void CancelHide()
    {
        _hideTimer?.Stop();
        _reevaluatePointerOnHide = false;
    }

    private void HideNow()
    {
        if (!Visual.IsVisible)
        {
            return;
        }

        Visual.IsVisible = false;
        _owner.InvalidateArrange();
    }

    private void MeasureAndArrange()
    {
        Visual.InvalidateMeasure();
        Visual.Measure(_owner.Bounds.Size);
        _owner.InvalidateArrange();
        _styleNeedsMeasure = false;
    }

    public void Measure(Size availableSize) => Visual.Measure(availableSize);

    public void Arrange(Size finalSize)
    {
        if (!Visual.IsVisible)
        {
            Visual.Arrange(default);
            return;
        }

        var size = new Size(
            Math.Min(Visual.DesiredSize.Width, Math.Max(0, finalSize.Width)),
            Math.Min(Visual.DesiredSize.Height, Math.Max(0, finalSize.Height)));
        double horizontalOffset = NormalizeFinite(_owner.ToolTipHorizontalOffset);
        double verticalOffset = NormalizeFinite(_owner.ToolTipVerticalOffset);
        double x = Math.Clamp(
            _state.AnchorPosition.X + horizontalOffset,
            0,
            Math.Max(0, finalSize.Width - size.Width));
        double y = Math.Clamp(
            _state.AnchorPosition.Y - size.Height - verticalOffset,
            0,
            Math.Max(0, finalSize.Height - size.Height));
        Visual.Arrange(new Rect(new Point(x, y), size));
    }

    private static string BuildDefaultContent(IChartDataPoint item)
    {
        string value = ChartValueFormatter.Format(item.Value);
        return string.IsNullOrWhiteSpace(item.Label) ? value : $"{item.Label}: {value}";
    }

    private static double NormalizePositive(double value, double fallback) =>
        double.IsFinite(value) && value > 0 ? value : fallback;

    private static double NormalizeFinite(double value) => double.IsFinite(value) ? value : 0;
}
