using Avalonia.Media;
using Aviora.Controls;

namespace Aviora.Demo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public IReadOnlyList<IChartDataPoint> MonthlySales { get; } =
    [
        new ChartDataPoint { Key = "Jan", Label = "Jan", Value = 42, ToolTip = "January: 42" },
        new ChartDataPoint { Key = "Feb", Label = "Feb", Value = 56, ToolTip = "February: 56" },
        new ChartDataPoint { Key = "Mar", Label = "Mar", Value = 49, ToolTip = "March: 49" },
        new ChartDataPoint { Key = "Apr", Label = "Apr", Value = 71, ToolTip = "April: 71" },
        new ChartDataPoint { Key = "May", Label = "May", Value = 64, ToolTip = "May: 64" },
        new ChartDataPoint { Key = "Jun", Label = "Jun", Value = 83, ToolTip = "June: 83" },
    ];

    public IReadOnlyList<ChartThreshold> MonthlySalesThresholds { get; } =
    [
        new ChartThreshold { Label = "Normal", Value = 40, Brush = Brushes.Green },
        new ChartThreshold { Label = "Warning", Value = 65, Brush = Brushes.Orange },
        new ChartThreshold { Label = "Danger", Value = 80, Brush = Brushes.Red },
    ];

    public IReadOnlyList<double> WeeklyValues { get; } = [24, 38, 31, 46, 40, 54, 48];

    public IReadOnlyList<string> WeeklyLabels { get; } = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    public IReadOnlyList<ChartThreshold> WeeklyScoreThresholds { get; } =
    [
        new ChartThreshold { Label = "Danger", Value = 30, Brush = Brushes.Red },
        new ChartThreshold { Label = "Warning", Value = 42, Brush = Brushes.Orange },
        new ChartThreshold { Label = "Normal", Value = 50, Brush = Brushes.Green },
    ];
}
