using Aviora.Presentation.Drawers;

namespace Aviora.Controls.Tests;

public class PresentationAbstractionsTests
{
    [Fact]
    public void Presentation_abstractions_do_not_reference_Avalonia()
    {
        string[] referencedAssemblies = typeof(IDrawerService).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            referencedAssemblies,
            name => name.StartsWith("Avalonia", StringComparison.Ordinal));
    }

    [Fact]
    public void Drawer_request_uses_the_default_host()
    {
        var request = new DrawerRequest("content");

        Assert.Equal(DrawerHost.DefaultId, request.HostId);
    }
}
