using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using Aviora.Controls;

namespace Aviora.Controls.Tests;

public class ChartRenderingTests
{
    private const int Width = 320;
    private const int Height = 200;

    [AvaloniaFact]
    public void ColumnChart_renders_non_transparent_pixels()
    {
        var chart = new ColumnChart
        {
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            ShowXAxis = false,
            ShowYAxis = false,
            ShowGridLines = false,
            DefaultBrush = Brushes.Red,
            Values = [20, 55, 80],
        };

        byte[] pixels = Render(chart);

        int visiblePixels = CountVisiblePixels(pixels);
        Assert.True(visiblePixels > 1_000, $"Expected a rendered area, but found {visiblePixels} visible pixels.");
    }

    [AvaloniaFact]
    public void LineChart_default_theme_does_not_show_a_selected_point_state()
    {
        var chart = new LineChart
        {
            IsAnimationEnabled = false,
            Values = [20, 40, 60],
            SelectedIndex = 1,
        };
        var window = new Window { Content = chart };

        try
        {
            window.Show();

            Assert.Null(chart.SelectedPointBrush);
            Assert.True(double.IsNaN(chart.SelectedPointRadius));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Smooth_line_and_area_are_clipped_to_the_plot()
    {
        var chart = new LineChart
        {
            MinValue = 0,
            MaxValue = 100,
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            ShowXAxis = false,
            ShowYAxis = false,
            ShowGridLines = false,
            ShowPoints = false,
            InterpolationMode = LineInterpolationMode.Smooth,
            LineBrush = Brushes.Red,
            LineThickness = 4,
            AreaFillBrush = Brushes.Blue,
            Values = [0, 100, 0, 100],
        };

        byte[] pixels = Render(chart);

        int visiblePixels = CountVisiblePixels(pixels);
        Assert.True(visiblePixels > 1_000, $"Expected a rendered area, but found {visiblePixels} visible pixels.");
        Assert.True(IsTransparentRow(pixels, 0), "The first row should remain outside the plot clip.");
        Assert.True(IsTransparentRow(pixels, 1), "The second row should remain outside the plot clip.");
        Assert.True(IsTransparentRow(pixels, Height - 2), "The penultimate row should remain outside the plot clip.");
        Assert.True(IsTransparentRow(pixels, Height - 1), "The last row should remain outside the plot clip.");
    }

    [AvaloniaFact]
    public void Tooltip_template_receives_the_hovered_data_point()
    {
        var first = new ChartDataPoint { Label = "First", Value = 20 };
        var second = new ChartDataPoint { Label = "Second", Value = 60 };
        IDataTemplate template = new FuncDataTemplate<IChartDataPoint>(
            (item, _) => new TextBlock { Text = item?.Label });
        var chart = new LineChart
        {
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            ShowXAxis = false,
            ShowYAxis = false,
            ToolTipTemplate = template,
            ItemsSource = [first, second],
        };
        var window = new Window
        {
            Width = Width,
            Height = Height,
            Content = chart,
        };

        try
        {
            window.Show();
            window.MouseMove(new Point(80, 100));

            Border presenter = chart.ToolTipPresenter;
            var content = Assert.IsType<ContentControl>(chart.ToolTipPresenter.Child);
            Assert.True(chart.ToolTipPresenter.IsVisible);
            Assert.Same(first, content.Content);
            Assert.Same(template, content.ContentTemplate);
            Assert.Equal("First", Assert.Single(content.GetVisualDescendants().OfType<TextBlock>()).Text);
            Assert.True(presenter.Bounds.Width > 0);
            Assert.True(presenter.Bounds.Height > 0);
            Assert.True(presenter.Bounds.Right <= chart.Bounds.Width);
            Assert.True(presenter.Bounds.Bottom <= chart.Bounds.Height);
            Rect firstBounds = presenter.Bounds;

            window.MouseMove(new Point(100, 120));
            Assert.Same(first, content.Content);
            Assert.Same(presenter, chart.ToolTipPresenter);
            Assert.Equal(firstBounds, presenter.Bounds);

            window.MouseMove(new Point(240, 100));
            Assert.Same(second, content.Content);
            Assert.Same(presenter, chart.ToolTipPresenter);
            Assert.Equal("Second", Assert.Single(content.GetVisualDescendants().OfType<TextBlock>()).Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Tooltip_template_is_constrained_to_chart_bounds()
    {
        IDataTemplate template = new FuncDataTemplate<IChartDataPoint>(
            (_, _) => new Border
            {
                Width = Width * 2,
                Height = Height * 2,
            });
        var chart = new LineChart
        {
            IsAnimationEnabled = false,
            UpdateThrottleInterval = TimeSpan.Zero,
            ShowXAxis = false,
            ShowYAxis = false,
            ToolTipTemplate = template,
            ItemsSource = [new ChartDataPoint { Label = "Oversized", Value = 20 }],
        };
        var window = new Window
        {
            Width = Width,
            Height = Height,
            Content = chart,
        };

        try
        {
            window.Show();
            window.MouseMove(new Point(Width / 2, Height / 2));

            Assert.True(chart.ToolTipPresenter.IsVisible);
            Assert.True(chart.ToolTipPresenter.Bounds.Width <= chart.Bounds.Width);
            Assert.True(chart.ToolTipPresenter.Bounds.Height <= chart.Bounds.Height);
            Assert.True(chart.ToolTipPresenter.Bounds.Right <= chart.Bounds.Width);
            Assert.True(chart.ToolTipPresenter.Bounds.Bottom <= chart.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    private static byte[] Render(Control control)
    {
        control.Measure(new Size(Width, Height));
        control.Arrange(new Rect(0, 0, Width, Height));

        using var target = new RenderTargetBitmap(new PixelSize(Width, Height));
        target.Render(control);
        using var pixels = new WriteableBitmap(
            new PixelSize(Width, Height),
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

    private static bool IsTransparentRow(byte[] pixels, int row)
    {
        int start = row * Width * 4;
        int end = start + (Width * 4);
        for (int index = start + 3; index < end; index += 4)
        {
            if (pixels[index] != 0)
            {
                return false;
            }
        }

        return true;
    }
}
