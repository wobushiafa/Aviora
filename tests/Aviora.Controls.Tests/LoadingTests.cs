using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aviora.Controls.Tests;

public class LoadingTests
{
    [Fact]
    public void Loading_has_usable_defaults()
    {
        var loading = new Loading();

        Assert.Equal(LoadingIndicatorStyle.Ring, loading.IndicatorStyle);
        Assert.True(loading.IsActive);
        Assert.Equal(3, loading.StrokeThickness);
        Assert.Equal(TimeSpan.FromMilliseconds(900), loading.AnimationDuration);
        Assert.Null(loading.Content);
    }

    [Theory]
    [InlineData(LoadingIndicatorStyle.Ring)]
    [InlineData(LoadingIndicatorStyle.Dots)]
    [InlineData(LoadingIndicatorStyle.Pulse)]
    [InlineData(LoadingIndicatorStyle.Bars)]
    public void Built_in_styles_can_be_selected(LoadingIndicatorStyle style)
    {
        var loading = new Loading { IndicatorStyle = style };

        Assert.Equal(style, loading.IndicatorStyle);
    }

    [Theory]
    [InlineData(0, 70)]
    [InlineData(0.25, 175)]
    [InlineData(0.5, 280)]
    [InlineData(0.75, 175)]
    [InlineData(1, 70)]
    public void Ring_arc_expands_and_contracts_during_each_cycle(double progress, double expectedSweep)
    {
        (double start, double sweep) = Loading.CalculateRingArc(progress);

        Assert.Equal(expectedSweep, sweep, 6);
        Assert.True(double.IsFinite(start));
    }

    [Fact]
    public void Ring_arc_is_an_open_unfilled_path()
    {
        PathGeometry geometry = Loading.CreateRingArcGeometry(new Point(24, 24), 20, -90, 180);

        PathFigure figure = Assert.Single(geometry.Figures!);
        Assert.False(figure.IsClosed);
        Assert.False(figure.IsFilled);
        Assert.IsType<ArcSegment>(Assert.Single(figure.Segments!));
    }

    [Fact]
    public void Content_replaces_the_builtin_indicator()
    {
        var customIndicator = new Border { Background = Brushes.Magenta };
        var loading = new Loading { Content = customIndicator };

        Assert.Same(customIndicator, loading.Content);
    }

    [AvaloniaFact]
    public void Theme_uses_palette_resources_and_allows_local_overrides()
    {
        var inherited = new Loading();
        var local = new Loading { IndicatorBrush = Brushes.Orange };
        var window = new Window
        {
            Content = new StackPanel { Children = { inherited, local } },
        };
        window.Resources["AvioraAccentBrush"] = Brushes.Magenta;

        try
        {
            window.Show();

            Assert.Same(Brushes.Magenta, inherited.IndicatorBrush);
            Assert.Same(Brushes.Orange, local.IndicatorBrush);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Every_builtin_style_renders_visible_pixels()
    {
        foreach (LoadingIndicatorStyle style in Enum.GetValues<LoadingIndicatorStyle>())
        {
            var loading = new Loading
            {
                Width = 48,
                Height = 48,
                IndicatorStyle = style,
                IndicatorBrush = Brushes.White,
                TrackBrush = Brushes.Gray,
            };
            loading.Measure(new Size(48, 48));
            loading.Arrange(new Rect(0, 0, 48, 48));

            Assert.True(CountVisiblePixels(loading) > 20, $"Expected {style} to render visible pixels.");
        }
    }

    [AvaloniaFact]
    public void Themed_ring_renders_track_and_active_arc()
    {
        var loading = new Loading
        {
            Width = 48,
            Height = 48,
            IndicatorStyle = LoadingIndicatorStyle.Ring,
            IndicatorBrush = Brushes.Magenta,
            TrackBrush = Brushes.Lime,
            StrokeThickness = 5,
        };
        var window = new Window { Content = loading };

        try
        {
            window.Show();

            (int activePixels, int trackPixels) = CountRingPixels(loading);
            Assert.True(activePixels > 20, $"Expected an active ring arc, but found {activePixels} pixels.");
            Assert.True(trackPixels > 5, $"Expected a visible ring track, but found {trackPixels} pixels.");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Inactive_indicator_does_not_render_builtin_pixels()
    {
        var loading = new Loading
        {
            Width = 48,
            Height = 48,
            IsActive = false,
        };
        loading.Measure(new Size(48, 48));
        loading.Arrange(new Rect(0, 0, 48, 48));

        Assert.Equal(0, CountVisiblePixels(loading));
    }

    private static int CountVisiblePixels(Control control)
    {
        const int size = 48;
        using var target = new RenderTargetBitmap(new PixelSize(size, size));
        target.Render(control);
        using var pixels = new WriteableBitmap(
            new PixelSize(size, size),
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

        return visiblePixels;
    }

    private static (int Active, int Track) CountRingPixels(Control control)
    {
        const int size = 48;
        using var target = new RenderTargetBitmap(new PixelSize(size, size));
        target.Render(control);
        using var pixels = new WriteableBitmap(
            new PixelSize(size, size),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using ILockedFramebuffer framebuffer = pixels.Lock();
        target.CopyPixels(framebuffer);

        var buffer = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, buffer, 0, buffer.Length);
        int activePixels = 0;
        int trackPixels = 0;
        for (int index = 0; index < buffer.Length; index += 4)
        {
            byte blue = buffer[index];
            byte green = buffer[index + 1];
            byte red = buffer[index + 2];
            if (red > 180 && blue > 180 && green < 100)
            {
                activePixels++;
            }
            else if (green > 180 && red < 100 && blue < 100)
            {
                trackPixels++;
            }
        }

        return (activePixels, trackPixels);
    }
}
