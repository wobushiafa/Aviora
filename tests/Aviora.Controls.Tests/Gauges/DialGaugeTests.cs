using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aviora.Controls.Tests;

public class DialGaugeTests
{
    [Fact]
    public void DialGauge_has_consistent_range_and_tick_defaults()
    {
        var gauge = new DialGauge();

        Assert.Equal(0, gauge.Minimum);
        Assert.Equal(100, gauge.Maximum);
        Assert.Equal(0, gauge.Value);
        Assert.True(gauge.ShowTicks);
        Assert.Equal(20, gauge.TickCount);
        Assert.True(gauge.ShowTickLabels);
        Assert.Equal(5, gauge.TickLabelInterval);
        Assert.Equal("0.##", gauge.TickLabelFormat);
        Assert.Null(gauge.TickLabelFormatter);
        Assert.Equal(11, gauge.TickLabelFontSize);
        Assert.Equal(DialGaugeTickColorMode.Uniform, gauge.TickColorMode);
        Assert.Null(gauge.Transitions);
    }

    [Fact]
    public void Transitions_can_be_configured_by_the_consumer()
    {
        var transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = DialGauge.ValueProperty,
                Duration = TimeSpan.FromMilliseconds(650),
            },
            new BrushTransition
            {
                Property = DialGauge.NeedleBrushProperty,
                Duration = TimeSpan.FromMilliseconds(450),
            },
        };
        var gauge = new DialGauge { Transitions = transitions };

        Assert.Same(transitions, gauge.Transitions);
        Assert.Equal(
            TimeSpan.FromMilliseconds(650),
            Assert.Single(transitions.OfType<DoubleTransition>()).Duration);
        Assert.Equal(
            TimeSpan.FromMilliseconds(450),
            Assert.Single(transitions.OfType<BrushTransition>()).Duration);
    }

    [Fact]
    public void Brushes_accept_standard_Avalonia_brushes()
    {
        var brush = new LinearGradientBrush
        {
            GradientStops =
            [
                new GradientStop(Colors.Blue, 0),
                new GradientStop(Colors.Red, 1),
            ],
        };
        var gauge = new DialGauge
        {
            TickBrush = brush,
            NeedleBrush = brush,
            PivotBrush = brush,
        };

        Assert.Same(brush, gauge.TickBrush);
        Assert.Same(brush, gauge.NeedleBrush);
        Assert.Same(brush, gauge.PivotBrush);
    }

    [AvaloniaFact]
    public void Theme_provides_the_shared_palette_and_allows_local_overrides()
    {
        var inherited = new DialGauge();
        var customBrush = Brushes.Magenta;
        var local = new DialGauge { TickBrush = customBrush };
        var window = new Window
        {
            Content = new StackPanel { Children = { inherited, local } },
        };

        try
        {
            window.Show();
            Application application = Assert.IsAssignableFrom<Application>(Application.Current);

            Assert.Same(application.FindResource("AvioraAccentBrush"), inherited.TickBrush);
            Assert.Same(application.FindResource("AvioraAccentStrongBrush"), inherited.NeedleBrush);
            Assert.Same(application.FindResource("AvioraWarningBrush"), inherited.MediumRangeBrush);
            Assert.Same(application.FindResource("AvioraDangerBrush"), inherited.HighRangeBrush);
            Assert.Same(customBrush, local.TickBrush);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Tick_labels_support_format_strings_and_custom_formatters()
    {
        var gauge = new DialGauge { TickLabelFormat = "0.0" };

        Assert.Equal(12.34.ToString("0.0"), gauge.FormatTickLabel(12.34));

        gauge.TickLabelFormatter = value => $"Level {value:0}";

        Assert.Equal("Level 12", gauge.FormatTickLabel(12.34));
    }

    [Theory]
    [InlineData(0, 20, 5, true)]
    [InlineData(5, 20, 5, true)]
    [InlineData(20, 20, 5, true)]
    [InlineData(3, 20, 5, false)]
    public void Tick_label_interval_keeps_endpoints_visible(
        int index,
        int tickCount,
        int interval,
        bool expected)
    {
        Assert.Equal(expected, DialGauge.ShouldDrawTickLabel(index, tickCount, interval));
    }

    [Theory]
    [InlineData(50, 0, 100, 0.5)]
    [InlineData(-10, 0, 100, 0)]
    [InlineData(120, 0, 100, 1)]
    [InlineData(50, 100, 100, 0)]
    public void Value_is_normalized_to_the_configured_range(
        double value,
        double minimum,
        double maximum,
        double expected)
    {
        Assert.Equal(expected, DialGauge.NormalizeValue(value, minimum, maximum));
    }

    [AvaloniaFact]
    public void DialGauge_renders_non_transparent_pixels()
    {
        const int width = 300;
        const int height = 220;
        var gauge = new DialGauge
        {
            Value = 64,
            TickColorMode = DialGaugeTickColorMode.Range,
        };
        gauge.Measure(new Size(width, height));
        gauge.Arrange(new Rect(0, 0, width, height));

        using var target = new RenderTargetBitmap(new PixelSize(width, height));
        target.Render(gauge);
        using var pixels = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using ILockedFramebuffer framebuffer = pixels.Lock();
        target.CopyPixels(framebuffer);

        var buffer = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, buffer, 0, buffer.Length);
        int visiblePixels = 0;
        for (int index = 3; index < buffer.Length; index += 4)
        {
            if (buffer[index] != 0)
            {
                visiblePixels++;
            }
        }

        Assert.True(visiblePixels > 500, $"Expected a rendered dial gauge, but found {visiblePixels} visible pixels.");
    }
}
