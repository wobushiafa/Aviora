using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Aviora.Controls.Tests.TestApplication))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Aviora.Controls.Tests;

public sealed class TestApplication : Application
{
    public override void Initialize()
    {
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Aviora.Controls/"))
        {
            Source = new Uri("avares://Aviora.Controls/Themes/Generic.axaml"),
        });
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                ShouldRenderOnUIThread = true,
                UseHeadlessDrawing = false,
            });
}
