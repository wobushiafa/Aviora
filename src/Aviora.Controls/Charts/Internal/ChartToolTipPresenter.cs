using Avalonia;
using Avalonia.Controls;

namespace Aviora.Controls;

internal sealed class ChartToolTipPresenter
{
    private readonly CartesianChart _owner;
    private readonly ContentControl _content = new() { ClipToBounds = true };
    private readonly ChartToolTipState _state = new();

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
        property == CartesianChart.ToolTipFormatterProperty;

    public bool Update(int index, Point pointerPosition) => _state.Update(index, pointerPosition);

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
    }

    public void Refresh(IReadOnlyList<IChartDataPoint> items)
    {
        int index = _state.HoveredIndex;
        if (!_owner.IsToolTipEnabled || index < 0 || index >= items.Count)
        {
            Hide();
            return;
        }

        IChartDataPoint item = items[index];
        if (_owner.ToolTipTemplate != null)
        {
            _content.Content = item;
            _content.ContentTemplate = _owner.ToolTipTemplate;
        }
        else
        {
            string content = _owner.ToolTipFormatter?.Invoke(item) ??
                             item.ToolTip ??
                             BuildDefaultContent(item);
            if (string.IsNullOrWhiteSpace(content))
            {
                Hide();
                return;
            }

            _content.ContentTemplate = null;
            _content.Content = content;
        }

        Visual.IsVisible = true;
        Visual.InvalidateMeasure();
        _owner.InvalidateMeasure();
    }

    public void Hide()
    {
        if (!Visual.IsVisible)
        {
            return;
        }

        Visual.IsVisible = false;
        _owner.InvalidateArrange();
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
