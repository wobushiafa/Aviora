using Avalonia.Media;
using Aviora.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class ChartsPageViewModel : DemoPageViewModel
{
    private int _updateVersion;

    [ObservableProperty]
    private IReadOnlyList<IChartDataPoint> _animatedTrend;

    [ObservableProperty]
    private bool _isAnimationEnabled = true;

    [ObservableProperty]
    private bool _isToolTipEnabled = true;

    [ObservableProperty]
    private bool _showGridLines = true;

    [ObservableProperty]
    private bool _showThresholdLabels = true;

    [ObservableProperty]
    private int _selectedColumnIndex = -1;

    [ObservableProperty]
    private IChartDataPoint? _selectedColumnItem;

    [ObservableProperty]
    private int _selectedLineIndex = -1;

    [ObservableProperty]
    private string _interactionStatus = "Select a column or point";

    public ChartsPageViewModel()
        : base("Charts")
    {
        _animatedTrend = CreateTrend(0);
    }

    public IReadOnlyList<IChartDataPoint> MonthlySales { get; } =
    [
        new ChartDataPoint { Key = "Jan", Label = "Jan", Value = 42, ToolTip = "January sales: 42" },
        new ChartDataPoint { Key = "Feb", Label = "Feb", Value = 56, ToolTip = "February sales: 56" },
        new ChartDataPoint
        {
            Key = "Mar",
            Label = "Mar",
            Value = 49,
            Brush = Brushes.MediumPurple,
            ColumnBackgroundBrush = new SolidColorBrush(Color.Parse("#F3E8FF")),
            ToolTip = "March uses per-item brushes",
        },
        new ChartDataPoint { Key = "Apr", Label = "Apr", Value = 71, ToolTip = "April sales: 71" },
        new ChartDataPoint { Key = "May", Label = "May", Value = 64, ToolTip = "May sales: 64" },
        new ChartDataPoint { Key = "Jun", Label = "Jun", Value = 83, ToolTip = "June sales: 83" },
    ];

    public IReadOnlyList<ChartThreshold> MonthlySalesThresholds { get; } =
    [
        new ChartThreshold { Label = "Baseline", Value = 40, Brush = Brushes.SeaGreen },
        new ChartThreshold
        {
            Label = "Target",
            Value = 65,
            Brush = Brushes.DarkOrange,
            LabelBrush = Brushes.SaddleBrown,
        },
        new ChartThreshold { Label = "Stretch", Value = 80, Brush = Brushes.Crimson },
    ];

    public IReadOnlyList<double> WeeklyValues { get; } = [24, 38, 31, 46, 40, 54, 48];

    public IReadOnlyList<string> WeeklyLabels { get; } = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    public IReadOnlyList<ChartThreshold> WeeklyScoreThresholds { get; } =
    [
        new ChartThreshold { Label = "Critical", Value = 30, Brush = Brushes.Crimson },
        new ChartThreshold { Label = "Watch", Value = 42, Brush = Brushes.DarkOrange },
        new ChartThreshold { Label = "Healthy", Value = 50, Brush = Brushes.SeaGreen },
    ];

    public IReadOnlyList<double> VarianceValues { get; } = [-24, 12, -8, 31, 18, -15, 27, 36, -5, 22];

    public IReadOnlyList<string> VarianceLabels { get; } =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct"];

    public IReadOnlyList<double> EmptyValues { get; } = [];

    public IReadOnlyList<string> PerformanceAxisLabels { get; } =
        ["0", "Needs work", "On track", "Strong", "100"];

    public Func<double, string> VarianceAxisFormatter { get; } =
        value => value == 0 ? "0" : $"{value:+0;-0}k";

    public Func<IChartDataPoint, string> TrendToolTipFormatter { get; } =
        item => $"{item.Label} performance: {item.Value:0.0}";

    [RelayCommand]
    private void UpdateTrend()
    {
        _updateVersion++;
        AnimatedTrend = CreateTrend(_updateVersion);
    }

    [RelayCommand]
    private void ChartItemClicked(IChartDataPoint? item)
    {
        if (item != null)
        {
            InteractionStatus = $"ItemClickCommand: {item.Label} = {item.Value:0.##}";
        }
    }

    partial void OnSelectedColumnItemChanged(IChartDataPoint? value)
    {
        if (value != null)
        {
            InteractionStatus = $"SelectedItem: {value.Label} = {value.Value:0.##}";
        }
    }

    private static IReadOnlyList<IChartDataPoint> CreateTrend(int version)
    {
        double offset = version % 2 == 0 ? 0 : 9;
        return
        [
            new ChartDataPoint { Key = "Q1", Label = "Q1", Value = 32 + offset },
            new ChartDataPoint { Key = "Q2", Label = "Q2", Value = 48 - (offset / 2) },
            new ChartDataPoint { Key = "Q3", Label = "Q3", Value = 41 + (offset / 3) },
            new ChartDataPoint { Key = "Q4", Label = "Q4", Value = 68 - offset },
            new ChartDataPoint { Key = "Q5", Label = "Q5", Value = 59 + offset },
            new ChartDataPoint { Key = "Q6", Label = "Q6", Value = 82 - (offset / 2) },
        ];
    }
}
