using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

namespace Aviora.Controls.Tests;

public class AngleGaugeTests
{
    [Fact]
    public void AngleGauge_has_consistent_gauge_defaults()
    {
        var gauge = new AngleGauge();

        Assert.Equal(0, gauge.Minimum);
        Assert.Equal(180, gauge.Maximum);
        Assert.Equal(0, gauge.Value);
        Assert.True(gauge.ShowTicks);
        Assert.Equal(6, gauge.TickCount);
        Assert.True(gauge.ShowTickLabels);
    }

    [Theory]
    [InlineData(90, 0, 180, 0.5)]
    [InlineData(-10, 0, 180, 0)]
    [InlineData(200, 0, 180, 1)]
    [InlineData(10, 10, 10, 0)]
    public void NormalizeValue_clamps_to_the_configured_range(double value, double minimum, double maximum, double expected)
    {
        Assert.Equal(expected, AngleGauge.NormalizeValue(value, minimum, maximum));
    }

    [AvaloniaFact]
    public void AngleGauge_renders_non_transparent_pixels()
    {
        var gauge = new AngleGauge { Width = 240, Height = 150, Value = 90 };
        gauge.Measure(new(240, 150));
        gauge.Arrange(new(0, 0, 240, 150));

        using var bitmap = new RenderTargetBitmap(new(240, 150));
        bitmap.Render(gauge);

        Assert.NotNull(bitmap);
    }
}
