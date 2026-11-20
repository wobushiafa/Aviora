using System.Collections.ObjectModel;
using Avalonia.Media;
using Aviora.Controls;
using Aviora.Core.Charts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aviora.Demo.ViewModels;

public partial class ChartsPageViewModel : DemoPageViewModel
{
    private int _updateVersion;

    [ObservableProperty]
    private ObservableCollection<IChartDataPoint> _animatedTrend;

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

    [ObservableProperty]
    private ColumnSeriesLayoutMode _selectedColumnLayoutMode = ColumnSeriesLayoutMode.Grouped;

    [ObservableProperty]
    private LineInterpolationMode _selectedLineInterpolation = LineInterpolationMode.MonotoneSmooth;

    [ObservableProperty]
    private double _lineTension = 1.0;

    [ObservableProperty]
    private ChartDownsampleMode _selectedDownsampleMode = ChartDownsampleMode.Lttb;

    [ObservableProperty]
    private IReadOnlyList<double> _largeDataset;

    [ObservableProperty]
    private double _zoomStartRatio;

    [ObservableProperty]
    private double _zoomEndRatio = 1.0;

    public ChartsPageViewModel()
        : base("Charts")
    {
        _animatedTrend = [];
        _largeDataset = GenerateLargeDataset(5000);
    }

    public ColumnSeriesLayoutMode[] ColumnLayoutModes { get; } = Enum.GetValues<ColumnSeriesLayoutMode>();

    public LineInterpolationMode[] InterpolationModes { get; } = Enum.GetValues<LineInterpolationMode>();

    public ChartDownsampleMode[] DownsampleModes { get; } = Enum.GetValues<ChartDownsampleMode>();

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

    public IReadOnlyList<string> ComparisonLabels { get; } = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];

    public IReadOnlyList<LineChartSeries> ComparisonSeries { get; } =
    [
        new LineChartSeries
        {
            Title = "Revenue",
            Values = [38, 46, 52, 61, 68, 78],
            LineBrush = new SolidColorBrush(Color.Parse("#2563EB")),
            AreaFillBrush = new SolidColorBrush(Color.Parse("#302563EB")),
            PointBrush = new SolidColorBrush(Color.Parse("#1D4ED8")),
            PointShape = ChartPointShape.Circle,
        },
        new LineChartSeries
        {
            Title = "Orders",
            Values = [31, 39, 36, 51, 57, 66],
            LineBrush = new SolidColorBrush(Color.Parse("#0F766E")),
            AreaFillBrush = new SolidColorBrush(Color.Parse("#300F766E")),
            PointBrush = new SolidColorBrush(Color.Parse("#0D9488")),
            PointShape = ChartPointShape.Square,
        },
        new LineChartSeries
        {
            Title = "Forecast",
            Values = [42, 44, 49, 56, 64, 72],
            LineBrush = new SolidColorBrush(Color.Parse("#D97706")),
            AreaFillBrush = new SolidColorBrush(Color.Parse("#20D97706")),
            PointBrush = new SolidColorBrush(Color.Parse("#F59E0B")),
            PointShape = ChartPointShape.Diamond,
            StrokeDashArray = [5.0, 4.0],
        },
    ];

    public IReadOnlyList<string> QuarterLabels { get; } = ["Q1", "Q2", "Q3", "Q4", "Q5", "Q6"];

    public IReadOnlyList<ColumnChartSeries> MultiColumnSeries { get; } =
    [
        new ColumnChartSeries
        {
            Title = "Cloud",
            Values = [34, 48, 41, 59, 64, 72],
            Brush = new SolidColorBrush(Color.Parse("#0F766E")),
        },
        new ColumnChartSeries
        {
            Title = "Desktop",
            Values = [28, 32, 37, 42, 39, 45],
            Brush = new SolidColorBrush(Color.Parse("#2563EB")),
        },
        new ColumnChartSeries
        {
            Title = "Services",
            Values = [15, 19, 24, 22, 29, 34],
            Brush = new SolidColorBrush(Color.Parse("#D97706")),
        },
    ];

    public IReadOnlyList<double> StepAndOvershootValues { get; } = [0, 0, 100, 100, 35, 35, 90, 20];

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
        AnimatedTrend = new ObservableCollection<IChartDataPoint>(CreateTrend(_updateVersion));
    }

    [RelayCommand]
    private void AddRandomTrendPoint()
    {
        int pointNumber = AnimatedTrend.Count + 1;
        double value = Random.Shared.Next(25, 91) + Random.Shared.NextDouble();
        AnimatedTrend.Add(new ChartDataPoint
        {
            Key = $"Q{pointNumber}",
            Label = $"Q{pointNumber}",
            Value = value,
            ToolTip = $"Randomly added point: {value:0.0}",
        });
    }

    [RelayCommand]
    private void RegenerateLargeDataset()
    {
        LargeDataset = GenerateLargeDataset(5000);
        ResetLargeChartZoom();
    }

    [RelayCommand]
    private void ResetLargeChartZoom()
    {
        ZoomStartRatio = 0.0;
        ZoomEndRatio = 1.0;
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

    private static double[] GenerateLargeDataset(int count)
    {
        var values = new double[count];
        double level = 50.0;
        for (int index = 0; index < count; index++)
        {
            double wave = Math.Sin(index * 0.025) * 18.0 + Math.Cos(index * 0.11) * 7.5;
            double noise = (Random.Shared.NextDouble() - 0.5) * 6.0;
            double spike = index % 617 == 150 ? 28.0 : index % 733 == 400 ? -24.0 : 0.0;
            values[index] = Math.Round(level + wave + noise + spike, 2);
        }

        return values;
    }
}
