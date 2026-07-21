using Aviora.Presentation.Drawers;
using Aviora.Presentation.Dialogs;

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

    [Fact]
    public void Dialog_request_uses_the_default_host()
    {
        var request = new DialogRequest("content");

        Assert.Equal(DialogHost.DefaultId, request.HostId);
    }

    [Fact]
    public async Task Show_dialog_factory_overload_creates_a_session_aware_request()
    {
        var service = new RecordingDialogService();

        await service.ShowAsync(
            session => session,
            TestContext.Current.CancellationToken);

        Assert.NotNull(service.Request?.ContentFactory);
    }

    [Fact]
    public async Task Show_content_overload_creates_a_request()
    {
        var service = new RecordingDrawerService();

        await service.ShowAsync(
            "content",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("content", service.Request?.Content);
        Assert.Equal(DrawerPlacement.Right, service.Request?.Placement);
    }

    [Fact]
    public async Task Show_factory_overload_creates_a_session_aware_request()
    {
        var service = new RecordingDrawerService();

        await service.ShowAsync(
            session => session,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(service.Request?.ContentFactory);
        Assert.Equal(DrawerPlacement.Right, service.Request?.Placement);
    }

    [Fact]
    public async Task Show_content_overload_forwards_placement()
    {
        var service = new RecordingDrawerService();

        await service.ShowAsync(
            "content",
            DrawerPlacement.Left,
            TestContext.Current.CancellationToken);

        Assert.Equal(DrawerPlacement.Left, service.Request?.Placement);
    }

    [Fact]
    public async Task Show_factory_overload_forwards_placement()
    {
        var service = new RecordingDrawerService();

        await service.ShowAsync(
            session => session,
            DrawerPlacement.Bottom,
            TestContext.Current.CancellationToken);

        Assert.NotNull(service.Request?.ContentFactory);
        Assert.Equal(DrawerPlacement.Bottom, service.Request?.Placement);
    }

    private sealed class RecordingDrawerService : IDrawerService
    {
        public DrawerRequest? Request { get; private set; }

        public Task<DrawerResult> ShowAsync(
            DrawerRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new DrawerResult(null, DrawerCloseReason.Programmatic));
        }

        public bool Close(string hostId = DrawerHost.DefaultId, object? result = null) => false;
    }

    private sealed class RecordingDialogService : IDialogService
    {
        public DialogRequest? Request { get; private set; }

        public Task<DialogResult> ShowAsync(
            DialogRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new DialogResult(null, DialogCloseReason.Programmatic));
        }

        public bool Close(string hostId = DialogHost.DefaultId, object? result = null) => false;
    }
}
