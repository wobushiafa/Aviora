using Avalonia;
using Avalonia.Headless;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Aviora.Controls.Tests.TestApplication))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Aviora.Controls.Tests;

public sealed class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                ShouldRenderOnUIThread = true,
                UseHeadlessDrawing = false,
            });
}
