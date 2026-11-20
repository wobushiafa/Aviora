using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Aviora.Controls.Tests;

public class BackdropBlurTests
{
    private const int Size = 160;

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(.75)]
    [InlineData(.5)]
    public void Parent_clip_preserves_blur_input_outside_the_visible_output(double scale)
    {
        var blur = new BackdropBlur { BlurRadius = 8, Width = 80, Height = 80, RenderScale = scale };
        var clipped = new Grid { ClipToBounds = true, Width = 40, Height = 80 };
        clipped.Children.Add(new BackdropBlur
        {
            BlurRadius = 8, Width = 80, Height = 80, RenderScale = scale,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        });
        var scene = CreateScene(blur);
        scene.Children.Remove(blur);
        var canvas = new Canvas();
        Canvas.SetLeft(clipped, 40);
        Canvas.SetTop(clipped, 40);
        canvas.Children.Add(clipped);
        scene.Children.Add(canvas);
        byte[] pixels = Render(scene);
        Assert.InRange(Channel(pixels, 42, 80, 0), 30, 200);
        Assert.Equal(0, Channel(pixels, 117, 80, 0));
        Assert.Equal(255, Channel(pixels, 38, 80, 0));
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(.75)]
    [InlineData(.5)]
    public void Blur_inside_an_opacity_group_remains_visible(double scale)
    {
        var scene = CreateScene(new BackdropBlur { BlurRadius = 8, Width = 80, Height = 80, RenderScale = scale });
        scene.Opacity = .5;
        byte[] pixels = Render(scene);
        Assert.InRange(Channel(pixels, 42, 80, 0), 30, 200);
        Assert.InRange(Channel(pixels, 42, 80, 3), 1, 254);
    }

    [AvaloniaFact]
    public void Invalid_render_scales_are_rejected()
    {
        var control = new BackdropBlur();
        Assert.Equal(1, control.RenderScale);
        foreach (double value in new[] { 0d, .249, 1.001, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentException>(() => control.RenderScale = value);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(.75)]
    [InlineData(.5)]
    [InlineData(.25)]
    public void Sampling_at_surface_edges_has_no_dark_or_transparent_fringe(double scale)
    {
        var scene = new Grid { Background = Brushes.White };
        scene.Children.Add(new BackdropBlur { BlurRadius = 12, RenderScale = scale });
        byte[] pixels = Render(scene);
        foreach (var (x, y) in new[] { (0, 0), (159, 0), (0, 159), (159, 159), (80, 0), (0, 80) })
        {
            Assert.Equal(255, Channel(pixels, x, y, 0));
            Assert.Equal(255, Channel(pixels, x, y, 3));
        }
    }

    [AvaloniaTheory]
    [InlineData(1, 1)]
    [InlineData(.75, 1)]
    [InlineData(.5, 1)]
    [InlineData(.25, 1)]
    [InlineData(.75, 1.25)]
    [InlineData(.5, 1.25)]
    [InlineData(.75, 2)]
    [InlineData(.5, 2)]
    public void Sampling_scale_preserves_blur_spread_and_full_resolution_corners(double renderScale, double dpi)
    {
        byte[] Draw(double sampling, CornerRadius corners)
        {
            var scene = CreateScene(new BackdropBlur
            {
                BlurRadius = 8, Width = 80, Height = 80, RenderScale = sampling, CornerRadius = corners,
            });
            scene.Measure(new Size(Size, Size));
            scene.Arrange(new Rect(0, 0, Size, Size));
            int pixels = (int)(Size * dpi);
            using var bitmap = new RenderTargetBitmap(new PixelSize(pixels, pixels), new Vector(96 * dpi, 96 * dpi));
            bitmap.Render(scene);
            return ReadPixels(bitmap, pixels);
        }
        int size = (int)(Size * dpi);
        byte[] reference = Draw(1, default);
        byte[] actual = Draw(renderScale, new CornerRadius(32, 0, 0, 0));
        int x = (int)(42 * dpi), y = size / 2;
        int left = Channel(actual, x, y, 0, size);
        Assert.InRange(left, 30, 200);
        Assert.InRange(Math.Abs(left - Channel(reference, x, y, 0, size)), 0, 5);
        Assert.InRange(Math.Abs(left - Channel(actual, size - x - 1, y, 0, size)), 0, 5);
        Assert.Equal(0, Channel(actual, x, x, 0, size));
        Assert.InRange(Channel(actual, size - x - 1, x, 0, size), 30, 200);
        Assert.Equal(255, Channel(actual, (int)(20 * dpi), y, 0, size));
    }

    [AvaloniaFact]
    public void Sampling_scale_and_background_changes_refresh_without_stale_pixels()
    {
        var blur = new BackdropBlur { BlurRadius = 8, Width = 80, Height = 80 };
        var scene = CreateScene(blur);
        var window = new Window { Width = Size, Height = Size, Content = scene };
        try
        {
            window.Show();
            foreach (double scale in new[] { .5, .75, 1, .5 })
            {
                blur.RenderScale = scale;
                scene.Background = Brushes.White;
                Assert.InRange(Channel(Capture(window), 42, 80, 0), 30, 200);
                scene.Background = Brushes.Red;
                byte[] pixels = Capture(window);
                Assert.Equal(0, Channel(pixels, 42, 80, 0));
                Assert.InRange(Channel(pixels, 42, 80, 2), 30, 200);
            }
            blur.BlurRadius = 0;
            Assert.Equal(0, Channel(Capture(window), 42, 80, 2));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Typed_corner_radius_binding_updates_the_blur_clip()
    {
        var source = new Border { CornerRadius = new CornerRadius(0) };
        var blur = new BackdropBlur { BlurRadius = 8, Width = 80, Height = 80 };
        blur.Bind(BackdropBlur.CornerRadiusProperty, new Avalonia.Data.Binding(nameof(Border.CornerRadius)) { Source = source });
        var window = new Window { Width = Size, Height = Size, Content = CreateScene(blur) };
        try
        {
            window.Show();
            Assert.InRange(Channel(Capture(window), 42, 42, 0), 30, 200);
            source.CornerRadius = new CornerRadius(32, 0, 0, 0);
            Assert.Equal(source.CornerRadius, blur.CornerRadius);
            byte[] pixels = Capture(window);
            Assert.Equal(0, Channel(pixels, 42, 42, 0));
            Assert.InRange(Channel(pixels, 117, 42, 0), 30, 200);
            Assert.InRange(Channel(pixels, 42, 117, 0), 30, 200);

            source.CornerRadius = new CornerRadius(0);
            Assert.InRange(Channel(Capture(window), 42, 42, 0), 30, 200);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Invalid_blur_radii_are_rejected()
    {
        var control = new BackdropBlur();
        foreach (double value in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentException>(() => control.BlurRadius = value);
    }

    [AvaloniaFact]
    public void Asymmetric_borders_and_corners_match_the_standard_border()
    {
        var thickness = new Thickness(4, 8, 12, 16);
        var corners = new CornerRadius(24, 12, 32, 8);
        var actual = new BackdropBlur
        {
            BlurRadius = 0, Background = Brushes.Blue, BorderBrush = Brushes.Red,
            BorderThickness = thickness, CornerRadius = corners,
        };
        var expected = new Border
        {
            Background = Brushes.Blue, BorderBrush = Brushes.Red,
            BorderThickness = thickness, CornerRadius = corners,
        };

        Assert.Equal(Render(expected), Render(actual));
    }

    [AvaloniaFact]
    public void Bottom_only_border_is_visible_and_stays_inside_bounds()
    {
        var control = new BackdropBlur
        {
            BlurRadius = 0, BorderBrush = Brushes.Red, BorderThickness = new Thickness(0, 0, 0, 8),
        };
        byte[] pixels = Render(control);
        Assert.Equal(255, Channel(pixels, 80, 158, 2));
        Assert.Equal(255, Channel(pixels, 80, 158, 3));
        Assert.Equal(0, Channel(pixels, 80, 148, 3));
        Assert.Equal(0, Channel(pixels, 1, 80, 3));
    }

    [AvaloniaFact]
    public void Sampling_outside_the_output_clip_is_symmetric()
    {
        var blur = new BackdropBlur { BlurRadius = 8, Width = 80, Height = 80 };
        var scene = CreateScene(blur);
        byte[] pixels = Render(scene);
        int left = Channel(pixels, 42, 80, 0);
        int right = Channel(pixels, 117, 80, 0);

        Assert.InRange(left, 30, 200);
        Assert.InRange(right, 30, 200);
        Assert.InRange(Math.Abs(left - right), 0, 3);
        Assert.InRange(Channel(pixels, 80, 42, 0), left - 3, left + 3);
        Assert.InRange(Channel(pixels, 80, 117, 0), left - 3, left + 3);
        Assert.Equal(255, Channel(pixels, 20, 80, 0));
    }

    [AvaloniaFact]
    public void Zero_radius_leaves_the_backdrop_sharp()
    {
        var blur = new BackdropBlur { BlurRadius = 0, Width = 80, Height = 80 };
        byte[] pixels = Render(CreateScene(blur));
        Assert.Equal(0, Channel(pixels, 42, 80, 0));
        Assert.Equal(255, Channel(pixels, 38, 80, 0));
    }

    [AvaloniaTheory]
    [InlineData(1.25)]
    [InlineData(2.0)]
    public void Blur_spread_remains_symmetric_at_high_dpi(double scale)
    {
        var scene = CreateScene(new BackdropBlur { BlurRadius = 8, Width = 80, Height = 80 });
        scene.Measure(new Size(Size, Size));
        scene.Arrange(new Rect(0, 0, Size, Size));
        int pixelSize = (int)(Size * scale);
        using var bitmap = new RenderTargetBitmap(new PixelSize(pixelSize, pixelSize), new Vector(96 * scale, 96 * scale));
        bitmap.Render(scene);
        byte[] pixels = ReadPixels(bitmap, pixelSize);
        int x = (int)(42 * scale);
        int y = pixelSize / 2;
        int left = Channel(pixels, x, y, 0, pixelSize);
        int right = Channel(pixels, pixelSize - x - 1, y, 0, pixelSize);
        Assert.InRange(left, 30, 200);
        Assert.InRange(Math.Abs(left - right), 0, 3);
    }

    [AvaloniaFact]
    public void Property_changes_refresh_an_attached_control()
    {
        var blur = new BackdropBlur { BlurRadius = 0, Background = Brushes.Red };
        var window = new Window { Width = Size, Height = Size, Content = blur, Background = Brushes.Black };
        try
        {
            window.Show();
            byte[] before = Capture(window);
            Assert.Equal(255, Channel(before, 80, 80, 2));

            blur.Background = Brushes.Blue;
            Assert.Equal(255, Channel(Capture(window), 80, 80, 0));

            blur.CornerRadius = new CornerRadius(40);
            Assert.NotEqual(255, Channel(Capture(window), 1, 1, 0));

            blur.BorderBrush = Brushes.Lime;
            blur.BorderThickness = new Thickness(0, 0, 0, 8);
            Assert.Equal(255, Channel(Capture(window), 80, 158, 1));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Blur_radius_and_background_changes_refresh_without_caching_old_pixels()
    {
        var blur = new BackdropBlur { BlurRadius = 0, Width = 80, Height = 80 };
        var scene = CreateScene(blur);
        var window = new Window { Width = Size, Height = Size, Content = scene };
        try
        {
            window.Show();
            Assert.Equal(0, Channel(Capture(window), 42, 80, 0));
            blur.BlurRadius = 8;
            Assert.InRange(Channel(Capture(window), 42, 80, 0), 30, 200);

            scene.Background = Brushes.Red;
            byte[] pixels = Capture(window);
            Assert.Equal(0, Channel(pixels, 42, 80, 0));
            Assert.InRange(Channel(pixels, 42, 80, 2), 30, 200);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(.75)]
    [InlineData(.5)]
    public void Foreground_slider_updates_do_not_corrupt_backdrop_blur(double scale)
    {
        static Grid BuildScene(double renderScale, out Slider slider)
        {
            var scene = new Grid { Background = Brushes.White };
            scene.Children.Add(new Border
            {
                Background = Brushes.Black, Width = 80, Height = 160,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            });
            scene.Children.Add(new BackdropBlur
            {
                BlurRadius = 12,
                RenderScale = renderScale,
                Background = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Width = 120,
                Height = 100,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            });
            slider = CreateTestSlider();
            scene.Children.Add(slider);
            return scene;
        }

        var incrementalScene = BuildScene(scale, out var slider);
        var incrementalWindow = new Window { Width = Size, Height = Size, Content = incrementalScene };
        var sliderReferenceWindow = new Window { Width = Size, Height = Size, Content = BuildScene(scale, out var sliderRef) };
        var referenceScene = BuildScene(scale, out var refSlider);
        var referenceWindow = new Window { Width = Size, Height = Size, Content = referenceScene };
        try
        {
            static void AssertMatches(byte[] actual, byte[] expected)
            {
                int maxDiff = 0;
                int diffPixels = 0;
                for (int i = 0; i < actual.Length; i += 4)
                {
                    int d = Math.Max(Math.Max(Math.Abs(actual[i] - expected[i]), Math.Abs(actual[i + 1] - expected[i + 1])),
                        Math.Abs(actual[i + 2] - expected[i + 2]));
                    if (d > 2) diffPixels++;
                    if (d > maxDiff) maxDiff = d;
                }
                Assert.True(maxDiff <= 2 && diffPixels == 0, $"maxDiff={maxDiff}, diffPixels={diffPixels}");
            }

            incrementalWindow.Show();
            Capture(incrementalWindow);
            for (int v = 25; v <= 55; v += 5)
            {
                slider.Value = v;
                Capture(incrementalWindow);
            }
            byte[] afterSliderOnly = Capture(incrementalWindow);
            sliderRef.Value = 55;
            sliderReferenceWindow.Show();
            AssertMatches(afterSliderOnly, Capture(sliderReferenceWindow));

            // Simulate a video frame / background refresh followed by more Slider ticks.
            incrementalScene.Background = Brushes.CornflowerBlue;
            Capture(incrementalWindow);
            slider.Value = 65;
            Capture(incrementalWindow);
            slider.Value = 75;
            byte[] actual = Capture(incrementalWindow);

            referenceScene.Background = Brushes.CornflowerBlue;
            refSlider.Value = 75;
            referenceWindow.Show();
            AssertMatches(actual, Capture(referenceWindow));
        }
        finally
        {
            incrementalWindow.Close();
            sliderReferenceWindow.Close();
            referenceWindow.Close();
        }
    }

    private static Slider CreateTestSlider() => new()
    {
        Minimum = 0,
        Maximum = 100,
        Value = 20,
        Width = 100,
        Height = 20,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        Template = new Avalonia.Controls.Templates.FuncControlTemplate<Slider>((_, scope) =>
        {
            var decrease = new Button { Name = "PART_DecreaseButton", Background = Brushes.DodgerBlue, Height = 4 };
            var increase = new Button { Name = "PART_IncreaseButton", Background = Brushes.Gray, Height = 4 };
            var thumb = new Avalonia.Controls.Primitives.Thumb { Width = 12, Height = 12, Background = Brushes.White };
            var track = new Avalonia.Controls.Primitives.Track
            {
                Name = "PART_Track",
                DecreaseButton = decrease,
                IncreaseButton = increase,
                Thumb = thumb,
            };
            track.Bind(Avalonia.Controls.Primitives.Track.MinimumProperty, new Avalonia.Data.Binding(nameof(Slider.Minimum)) { RelativeSource = new Avalonia.Data.RelativeSource(Avalonia.Data.RelativeSourceMode.TemplatedParent) });
            track.Bind(Avalonia.Controls.Primitives.Track.MaximumProperty, new Avalonia.Data.Binding(nameof(Slider.Maximum)) { RelativeSource = new Avalonia.Data.RelativeSource(Avalonia.Data.RelativeSourceMode.TemplatedParent) });
            track.Bind(Avalonia.Controls.Primitives.Track.ValueProperty, new Avalonia.Data.Binding(nameof(Slider.Value)) { RelativeSource = new Avalonia.Data.RelativeSource(Avalonia.Data.RelativeSourceMode.TemplatedParent) });
            scope.Register("PART_Track", track);
            scope.Register("PART_DecreaseButton", decrease);
            scope.Register("PART_IncreaseButton", increase);
            return track;
        }),
    };

    private static Grid CreateScene(BackdropBlur blur)
    {
        var scene = new Grid { Background = Brushes.White };
        scene.Children.Add(new Border
        {
            Background = Brushes.Black, Width = 80, Height = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        });
        scene.Children.Add(blur);
        return scene;
    }

    private static byte[] Render(Control control)
    {
        control.Measure(new Size(Size, Size));
        control.Arrange(new Rect(0, 0, Size, Size));
        using var bitmap = new RenderTargetBitmap(new PixelSize(Size, Size));
        bitmap.Render(control);
        return ReadPixels(bitmap);
    }

    private static byte[] Capture(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var bitmap = window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        return ReadPixels(bitmap);
    }

    private static byte[] ReadPixels(Bitmap bitmap, int size = Size)
    {
        using var pixels = new WriteableBitmap(new PixelSize(size, size), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using ILockedFramebuffer framebuffer = pixels.Lock();
        bitmap.CopyPixels(framebuffer);
        var bytes = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
            Marshal.Copy(framebuffer.Address + y * framebuffer.RowBytes, bytes, y * size * 4, size * 4);
        return bytes;
    }

    private static byte Channel(byte[] pixels, int x, int y, int channel, int size = Size) => pixels[(y * size + x) * 4 + channel];
}
