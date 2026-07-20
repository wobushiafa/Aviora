using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aviora.Controls.Tests;

public class ThermometerTests
{
    [Fact]
    public void Thermometer_has_usable_defaults()
    {
        var thermometer = new Thermometer();

        Assert.Equal(0, thermometer.Minimum);
        Assert.Equal(100, thermometer.Maximum);
        Assert.Equal(0, thermometer.Value);
        Assert.True(thermometer.ShowTicks);
        Assert.Equal(10, thermometer.TickCount);
        Assert.False(thermometer.ShowTickLabels);
        Assert.Equal(1, thermometer.TickLabelInterval);
        Assert.Equal("0.##", thermometer.TickLabelFormat);
        Assert.Null(thermometer.TickLabelFormatter);
        Assert.Equal(10, thermometer.TickLabelFontSize);
        Assert.Equal(4, thermometer.TickLabelSpacing);
        Assert.Equal(LiquidBrushMappingMode.FilledArea, thermometer.LiquidBrushMappingMode);
    }

    [Fact]
    public void LiquidBrush_accepts_standard_Avalonia_brushes()
    {
        var brush = new LinearGradientBrush
        {
            GradientStops =
            [
                new GradientStop(Colors.Blue, 0),
                new GradientStop(Colors.Red, 1),
            ],
        };
        var thermometer = new Thermometer { LiquidBrush = brush };

        Assert.Same(brush, thermometer.LiquidBrush);
    }

    [AvaloniaFact]
    public void Full_range_mapping_reveals_only_the_reached_gradient_colors()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(Colors.Blue, 0),
                new GradientStop(Colors.Red, 1),
            ],
        };
        Color filledAreaColor = RenderLiquidTopColor(
            brush,
            LiquidBrushMappingMode.FilledArea,
            value: 25);
        Color fullRangeColor = RenderLiquidTopColor(
            brush,
            LiquidBrushMappingMode.FullRange,
            value: 25);

        Assert.True(filledAreaColor.R > filledAreaColor.B);
        Assert.True(fullRangeColor.B > fullRangeColor.R);
    }

    [AvaloniaFact]
    public void Theme_provides_default_palette_without_transitions()
    {
        var thermometer = new Thermometer();
        var window = new Window { Content = thermometer };

        try
        {
            window.Show();
            Application application = Assert.IsAssignableFrom<Application>(Application.Current);

            Assert.Null(thermometer.Transitions);
            Assert.Same(application.FindResource("AvioraAccentBrush"), thermometer.LiquidBrush);
            Assert.Same(application.FindResource("AvioraSubtleBrush"), thermometer.TubeBrush);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Palette_resources_and_local_properties_can_override_theme_defaults()
    {
        var resourceOverride = Brushes.Magenta;
        var directOverride = Brushes.Orange;
        var inherited = new Thermometer();
        var local = new Thermometer { LiquidBrush = directOverride };
        var window = new Window
        {
            Content = new StackPanel { Children = { inherited, local } },
        };
        window.Resources["AvioraAccentBrush"] = resourceOverride;

        try
        {
            window.Show();

            Assert.Same(resourceOverride, inherited.LiquidBrush);
            Assert.Same(directOverride, local.LiquidBrush);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Transitions_can_be_replaced_by_the_consumer()
    {
        var transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Thermometer.ValueProperty,
                Duration = TimeSpan.FromSeconds(1),
            },
        };
        var thermometer = new Thermometer { Transitions = transitions };

        Assert.Same(transitions, thermometer.Transitions);
    }

    [Fact]
    public void Tick_labels_support_format_strings_and_custom_formatters()
    {
        var thermometer = new Thermometer { TickLabelFormat = "0.0" };

        Assert.Equal(thermometer.FormatTickLabel(12.34), 12.34.ToString("0.0"));

        thermometer.TickLabelFormatter = value => $"Level {value:0}";

        Assert.Equal("Level 12", thermometer.FormatTickLabel(12.34));
    }

    [Theory]
    [InlineData(0, 7, 3, true)]
    [InlineData(3, 7, 3, true)]
    [InlineData(6, 7, 3, true)]
    [InlineData(7, 7, 3, true)]
    [InlineData(2, 7, 3, false)]
    public void Tick_label_interval_keeps_endpoints_visible(
        int index,
        int tickCount,
        int interval,
        bool expected)
    {
        Assert.Equal(expected, Thermometer.ShouldDrawTickLabel(index, tickCount, interval));
    }

    [AvaloniaFact]
    public void Tick_labels_expand_the_requested_width()
    {
        var withoutLabels = new Thermometer();
        var withLabels = new Thermometer
        {
            ShowTickLabels = true,
            TickLabelFormatter = value => $"{value:0} degrees",
        };

        withoutLabels.Measure(Size.Infinity);
        withLabels.Measure(Size.Infinity);

        Assert.True(withLabels.DesiredSize.Width > withoutLabels.DesiredSize.Width);
        Assert.Equal(withoutLabels.DesiredSize.Height, withLabels.DesiredSize.Height);
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
        Assert.Equal(expected, Thermometer.NormalizeValue(value, minimum, maximum));
    }

    [AvaloniaFact]
    public void Thermometer_renders_non_transparent_pixels()
    {
        const int width = 80;
        const int height = 280;
        var thermometer = new Thermometer
        {
            Value = 65,
        };
        thermometer.Measure(new Size(width, height));
        thermometer.Arrange(new Rect(0, 0, width, height));

        using var target = new RenderTargetBitmap(new PixelSize(width, height));
        target.Render(thermometer);
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

        Assert.True(visiblePixels > 1_000, $"Expected a rendered thermometer, but found {visiblePixels} visible pixels.");
    }

    private static Color RenderLiquidTopColor(
        IBrush brush,
        LiquidBrushMappingMode mappingMode,
        double value)
    {
        const int width = 80;
        const int height = 280;
        var thermometer = new Thermometer
        {
            Value = value,
            LiquidBrush = brush,
            LiquidBrushMappingMode = mappingMode,
            ShowTicks = false,
            TubeBrush = Brushes.Transparent,
            GlareBrush = Brushes.Transparent,
            Transitions = null,
        };
        thermometer.Measure(new Size(width, height));
        thermometer.Arrange(new Rect(0, 0, width, height));

        using var target = new RenderTargetBitmap(new PixelSize(width, height));
        target.Render(thermometer);
        using var pixels = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using ILockedFramebuffer framebuffer = pixels.Lock();
        target.CopyPixels(framebuffer);

        var buffer = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, buffer, 0, buffer.Length);
        int centerX = width / 2;
        for (int y = 0; y < height; y++)
        {
            int index = (y * framebuffer.RowBytes) + (centerX * 4);
            if (buffer[index + 3] != 0)
            {
                return Color.FromArgb(
                    buffer[index + 3],
                    buffer[index + 2],
                    buffer[index + 1],
                    buffer[index]);
            }
        }

        return Colors.Transparent;
    }
}
