using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Aviora.Core.Charts;

namespace Aviora.Controls.Tests;

public class ChartCustomizationAndPerfTests
{
    [AvaloniaFact]
    public void Selection_and_hover_do_not_rebuild_render_state()
    {
        var chart = new LineChart
        {
            Width = 320,
            Height = 200,
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            CrosshairMode = ChartCrosshairMode.Both,
            HoverPointRadius = 6,
            HoverHaloBrush = Brushes.LightBlue,
            Values = [15, 45, 30, 80, 55],
        };

        var window = new Window
        {
            Width = 320,
            Height = 200,
            Content = chart,
        };

        try
        {
            window.Show();
            RenderToBitmap(chart, 320, 200);
            int baselineBuilds = chart.RenderStateBuildCount;
            Assert.True(baselineBuilds >= 1);

            chart.SelectedIndex = 2;
            RenderToBitmap(chart, 320, 200);
            Assert.Equal(baselineBuilds, chart.RenderStateBuildCount);

            window.MouseMove(new Point(140, 100), RawInputModifiers.None);
            window.MouseMove(new Point(220, 80), RawInputModifiers.None);
            RenderToBitmap(chart, 320, 200);
            Assert.Equal(baselineBuilds, chart.RenderStateBuildCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Poco_data_source_and_selectors_adapt_objects_and_observe_changes()
    {
        var items = new ObservableCollection<SampleMetric>
        {
            new("CPU", 32),
            new("Memory", 64),
        };

        var chart = new ColumnChart
        {
            Width = 320,
            Height = 200,
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            DataSource = items,
            ValueSelector = obj => ((SampleMetric)obj).Reading,
            LabelSelector = obj => ((SampleMetric)obj).Name,
        };

        var window = new Window
        {
            Width = 320,
            Height = 200,
            Content = chart,
        };

        try
        {
            window.Show();
            chart.SelectedIndex = 1;
            Assert.Equal("Memory", chart.SelectedItem?.Label);
            Assert.Equal(64, chart.SelectedItem?.Value);

            items[1].Reading = 88;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(88, chart.SelectedItem?.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ColumnChart_supports_grouped_stacked_and_stacked100_with_rounded_bars_and_labels()
    {
        ColumnChartSeries[] series =
        [
            new() { Title = "A", Values = [30, 50], Brush = Brushes.SteelBlue },
            new() { Title = "B", Values = [40, 50], Brush = Brushes.Coral },
        ];

        var chart = new ColumnChart
        {
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            Series = series,
            SeriesLayoutMode = ColumnSeriesLayoutMode.Stacked,
            BarCornerRadius = new CornerRadius(6, 6, 0, 0),
            ShowDataLabels = true,
            ShowLegend = true,
        };

        byte[] stackedPixels = RenderToBitmap(chart, 320, 200);
        Assert.True(CountVisiblePixels(stackedPixels) > 1_000);

        chart.SeriesLayoutMode = ColumnSeriesLayoutMode.Stacked100;
        byte[] stacked100Pixels = RenderToBitmap(chart, 320, 200);
        Assert.True(CountVisiblePixels(stacked100Pixels) > 1_000);

        chart.SeriesLayoutMode = ColumnSeriesLayoutMode.Grouped;
        byte[] groupedPixels = RenderToBitmap(chart, 320, 200);
        Assert.True(CountVisiblePixels(groupedPixels) > 1_000);
    }

    [Fact]
    public void LineChart_monotone_smooth_eliminates_overshoot_compared_to_catmull_rom()
    {
        Point[] pts =
        [
            new(0, 100),
            new(50, 100),
            new(100, 0),
            new(150, 0),
        ];

        (Point First, Point Second)[] monotone = LineChartRenderer.ComputeMonotoneBezierControls(pts);
        Assert.Equal(3, monotone.Length);
        foreach ((Point first, Point second) in monotone)
        {
            Assert.InRange(first.Y, 0.0, 100.0);
            Assert.InRange(second.Y, 0.0, 100.0);
        }

        // Standard Catmull-Rom around the sharp corner overshoots past 100 on segment 0->1.
        (Point catFirst, Point catSecond) = LineChartRenderer.GetBezierControls(pts[0], pts[0], pts[1], pts[2]);
        Assert.True(catFirst.Y >= 100.0 && catSecond.Y > 100.0);
    }

    [AvaloniaFact]
    public void Interactive_legend_click_toggles_series_visibility_and_step_modes_render()
    {
        LineChartSeries[] series =
        [
            new()
            {
                Title = "Actual",
                Values = [20, 60, 40, 80],
                LineBrush = Brushes.DodgerBlue,
                InterpolationMode = LineInterpolationMode.StepAfter,
                PointShape = ChartPointShape.Diamond,
            },
            new()
            {
                Title = "Target",
                Values = [30, 50, 70, 60],
                LineBrush = Brushes.OrangeRed,
                StrokeDashArray = [4.0, 3.0],
                PointShape = ChartPointShape.Triangle,
            },
        ];

        var chart = new LineChart
        {
            Width = 360,
            Height = 220,
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            Series = series,
            ShowLegend = true,
            IsLegendInteractive = true,
            LegendPlacement = ChartLegendPlacement.Top,
        };

        var window = new Window
        {
            Width = 360,
            Height = 220,
            Content = chart,
        };

        try
        {
            window.Show();
            byte[] pixels = RenderToBitmap(chart, 360, 220);
            Assert.True(CountVisiblePixels(pixels) > 500);

            IReadOnlyList<Rect> legendBounds = chart.GetLegendItemBounds();
            Assert.Equal(2, legendBounds.Count);
            Point clickPoint = legendBounds[0].Center;
            window.MouseMove(clickPoint, RawInputModifiers.None);
            window.MouseDown(clickPoint, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(clickPoint, MouseButton.Left, RawInputModifiers.None);

            Assert.False(series[0].IsVisible);
            Assert.True(series[1].IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SharedCategory_tooltip_and_zoom_pan_and_reset_work()
    {
        LineChartSeries[] series =
        [
            new() { Title = "Alpha", Values = [10, 20, 30, 40, 50, 60, 70, 80], LineBrush = Brushes.Blue },
            new() { Title = "Beta", Values = [15, 25, 35, 45, 55, 65, 75, 85], LineBrush = Brushes.Green },
        ];

        var chart = new LineChart
        {
            Width = 360,
            Height = 220,
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            Series = series,
            ShowXAxis = false,
            ShowYAxis = false,
            ToolTipMode = ChartToolTipMode.SharedCategory,
            CrosshairMode = ChartCrosshairMode.Vertical,
            IsZoomEnabled = true,
            IsPanEnabled = true,
            MinVisiblePoints = 3,
        };

        var window = new Window
        {
            Width = 360,
            Height = 220,
            Content = chart,
        };

        try
        {
            window.Show();
            window.MouseMove(new Point(180, 110), RawInputModifiers.None);

            var content = Assert.IsType<ContentControl>(chart.ToolTipPresenter.Child);
            Assert.True(chart.ToolTipPresenter.IsVisible);
            string? text = content.Content as string;
            Assert.NotNull(text);
            Assert.Contains("Alpha:", text);
            Assert.Contains("Beta:", text);

            window.MouseWheel(new Point(180, 110), new Vector(0, 1), RawInputModifiers.None);
            Assert.True((chart.ZoomEndRatio - chart.ZoomStartRatio) < 1.0);

            chart.ResetZoom();
            Assert.Equal(0.0, chart.ZoomStartRatio);
            Assert.Equal(1.0, chart.ZoomEndRatio);
        }
        finally
        {
            window.Close();
        }
    }

    private static byte[] RenderToBitmap(Control control, int width, int height)
    {
        control.Measure(new Size(width, height));
        control.Arrange(new Rect(0, 0, width, height));

        using var target = new RenderTargetBitmap(new PixelSize(width, height));
        target.Render(control);
        using var pixels = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using ILockedFramebuffer framebuffer = pixels.Lock();
        target.CopyPixels(framebuffer);

        var result = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, result, 0, result.Length);
        return result;
    }

    private static int CountVisiblePixels(byte[] pixels)
    {
        int count = 0;
        for (int index = 3; index < pixels.Length; index += 4)
        {
            if (pixels[index] != 0)
            {
                count++;
            }
        }

        return count;
    }

    private sealed class SampleMetric(string name, double reading) : INotifyPropertyChanged
    {
        private double _reading = reading;

        public string Name { get; } = name;

        public double Reading
        {
            get => _reading;
            set
            {
                if (_reading.Equals(value))
                {
                    return;
                }

                _reading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Reading)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
