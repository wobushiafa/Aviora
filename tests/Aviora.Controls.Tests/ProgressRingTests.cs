using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aviora.Controls.Tests;

public class ProgressRingTests
{
    [Fact]
    public void Start_angle_defaults_to_top()
    {
        var progress = new ProgressRing();
        Assert.Equal(-90, progress.StartAngle);
    }

    [Fact]
    public void Progress_ring_has_usable_defaults()
    {
        var progress = new ProgressRing();
        Assert.Equal(0, progress.Minimum);
        Assert.Equal(100, progress.Maximum);
        Assert.Equal(0, progress.Value);
        Assert.Equal(4, progress.StrokeThickness);
        Assert.False(progress.IsIndeterminate);
    }

    [AvaloniaFact]
    public void Determinate_progress_renders_visible_pixels()
    {
        var progress = new ProgressRing
        {
            Width = 48, Height = 48, Value = 65,
            IndicatorBrush = Brushes.Magenta, TrackBrush = Brushes.Lime,
        };
        progress.Measure(new Size(48, 48));
        progress.Arrange(new Rect(0, 0, 48, 48));
        Assert.True(CountVisiblePixels(progress) > 50);
    }

    [AvaloniaFact]
    public void Start_angle_changes_determinate_arc_origin()
    {
        var progress = new ProgressRing
        {
            Width = 48,
            Height = 48,
            Value = 10,
            StartAngle = 0,
            IndicatorBrush = Brushes.Magenta,
            TrackBrush = null,
        };
        progress.Measure(new Size(48, 48));
        progress.Arrange(new Rect(0, 0, 48, 48));

        Assert.True(GetAlpha(progress, 46, 24) > 0);
        Assert.Equal(0, GetAlpha(progress, 24, 2));
    }

    [AvaloniaFact]
    public void Indeterminate_progress_renders_visible_pixels()
    {
        var progress = new ProgressRing
        {
            Width = 48, Height = 48, IsIndeterminate = true,
            IndicatorBrush = Brushes.Magenta, TrackBrush = Brushes.Lime,
        };
        progress.Measure(new Size(48, 48));
        progress.Arrange(new Rect(0, 0, 48, 48));
        Assert.True(CountVisiblePixels(progress) > 50);
    }

    private static int CountVisiblePixels(Control control)
    {
        const int size = 48;
        using var target = new RenderTargetBitmap(new PixelSize(size, size));
        target.Render(control);
        using var pixels = new WriteableBitmap(
            new PixelSize(size, size), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
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

    private static byte GetAlpha(Control control, int x, int y)
    {
        const int size = 48;
        using var target = new RenderTargetBitmap(new PixelSize(size, size));
        target.Render(control);
        using var pixels = new WriteableBitmap(
            new PixelSize(size, size), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using ILockedFramebuffer framebuffer = pixels.Lock();
        target.CopyPixels(framebuffer);

        var buffer = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
        Marshal.Copy(framebuffer.Address, buffer, 0, buffer.Length);
        return buffer[(y * framebuffer.RowBytes) + (x * 4) + 3];
    }
}
