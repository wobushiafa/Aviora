using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aviora.Presentation.Dialogs;

namespace Aviora.Controls.Tests;

public class DialogTests
{
    [Fact]
    public void Dialog_has_safe_defaults()
    {
        var dialog = new Dialog();

        Assert.False(dialog.IsOpen);
        Assert.True(double.IsNaN(dialog.DialogWidth));
        Assert.True(double.IsNaN(dialog.DialogHeight));
        Assert.Equal(280, dialog.MinDialogWidth);
        Assert.Equal(720, dialog.MaxDialogWidth);
        Assert.False(dialog.IsLightDismissEnabled);
        Assert.True(dialog.IsEscapeKeyEnabled);
        Assert.True(dialog.IsOverlayVisible);
    }

    [Fact]
    public void Closing_can_cancel_a_state_change()
    {
        var dialog = new Dialog { IsOpen = true };
        dialog.Closing += (_, args) => args.Cancel = true;

        dialog.IsOpen = false;

        Assert.True(dialog.IsOpen);
    }

    [Fact]
    public void Close_command_returns_its_parameter()
    {
        var dialog = new Dialog { IsOpen = true };
        DialogClosedEventArgs? closed = null;
        dialog.Closed += (_, args) => closed = args;

        dialog.CloseCommand.Execute("saved");

        Assert.NotNull(closed);
        Assert.Equal("saved", closed.Result);
        Assert.Equal(DialogCloseReason.Command, closed.Reason);
    }

    [AvaloniaFact]
    public async Task Service_queues_requests_until_a_host_is_attached()
    {
        var service = new DialogService();
        Task<DialogResult> first = service.ShowAsync(new DialogRequest("first"));
        Task<DialogResult> second = service.ShowAsync(new DialogRequest("second"));
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            Assert.True(dialog.IsOpen);
            Assert.Equal("first", dialog.DialogContent);
            Assert.False(first.IsCompleted);

            Assert.True(service.Close(result: 42));
            await FlushDispatcherAsync();

            Assert.Equal(42, (await first).GetValue<int>());
            Assert.True(dialog.IsOpen);
            Assert.Equal("second", dialog.DialogContent);

            dialog.CloseCommand.Execute("done");
            Assert.Equal("done", (await second).GetValue<string>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Navigate_request_shows_one_layer_and_restores_the_parent()
    {
        var service = new DialogService();
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            Task<DialogResult> parent = service.ShowAsync(new DialogRequest("parent"));
            await FlushDispatcherAsync();

            Task<DialogResult> child = service.ShowAsync(new DialogRequest("child")
            {
                PresentationMode = DialogPresentationMode.Navigate,
            });
            await FlushDispatcherAsync();

            Assert.Equal("child", dialog.DialogContent);
            Assert.False(parent.IsCompleted);

            Assert.True(service.Close(result: "child-result"));
            await FlushDispatcherAsync();

            Assert.Equal("child-result", (await child).GetValue<string>());
            Assert.True(dialog.IsOpen);
            Assert.Equal("parent", dialog.DialogContent);
            Assert.False(parent.IsCompleted);

            Assert.True(service.Close(result: "parent-result"));
            await FlushDispatcherAsync();
            Assert.Equal("parent-result", (await parent).GetValue<string>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Stack_request_keeps_the_parent_visible_below_the_child()
    {
        var service = new DialogService();
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            Task<DialogResult> parent = service.ShowAsync(new DialogRequest("parent"));
            await FlushDispatcherAsync();

            Task<DialogResult> child = service.ShowAsync(new DialogRequest("child")
            {
                PresentationMode = DialogPresentationMode.Stack,
                IsAnimationEnabled = false,
            });
            await FlushDispatcherAsync();

            Assert.Equal("parent", dialog.DialogContent);
            Dialog nested = Assert.Single(dialog.GetVisualDescendants().OfType<Dialog>());
            Assert.True(nested.IsOpen);
            Assert.Equal("child", nested.DialogContent);
            Assert.False(parent.IsCompleted);

            Task<DialogResult> grandchild = service.ShowAsync(new DialogRequest("grandchild")
            {
                PresentationMode = DialogPresentationMode.Stack,
                IsAnimationEnabled = false,
            });
            await FlushDispatcherAsync();

            Assert.Equal(2, dialog.GetVisualDescendants().OfType<Dialog>().Count());
            Assert.True(service.Close(result: "grandchild-result"));
            await FlushDispatcherAsync();
            Assert.Equal("grandchild-result", (await grandchild).GetValue<string>());
            Assert.Single(dialog.GetVisualDescendants().OfType<Dialog>());
            Assert.Equal("child", nested.DialogContent);

            Assert.True(service.Close(result: "child-result"));
            await FlushDispatcherAsync();

            Assert.Equal("child-result", (await child).GetValue<string>());
            Assert.Empty(dialog.GetVisualDescendants().OfType<Dialog>());
            Assert.Equal("parent", dialog.DialogContent);
            Assert.False(parent.IsCompleted);

            Assert.True(service.Close(result: "parent-result"));
            await FlushDispatcherAsync();
            Assert.Equal("parent-result", (await parent).GetValue<string>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Request_options_are_restored_after_close()
    {
        var service = new DialogService();
        var dialog = new Dialog
        {
            Service = service,
            DialogWidth = 500,
            DialogHeight = 300,
            IsLightDismissEnabled = false,
            IsAnimationEnabled = false,
        };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            Task<DialogResult> result = service.ShowAsync(new DialogRequest("temporary")
            {
                Width = 420,
                Height = 240,
                IsLightDismissEnabled = true,
                IsEscapeKeyEnabled = false,
                IsOverlayVisible = false,
                IsAnimationEnabled = false,
                AnimationDuration = TimeSpan.FromMilliseconds(5),
            });
            await FlushDispatcherAsync();

            Assert.Equal(420, dialog.DialogWidth);
            Assert.Equal(240, dialog.DialogHeight);
            Assert.True(dialog.IsLightDismissEnabled);
            Assert.False(dialog.IsEscapeKeyEnabled);
            Assert.False(dialog.IsOverlayVisible);

            dialog.TryClose();
            await result;

            Assert.Equal(500, dialog.DialogWidth);
            Assert.Equal(300, dialog.DialogHeight);
            Assert.False(dialog.IsLightDismissEnabled);
            Assert.True(dialog.IsEscapeKeyEnabled);
            Assert.True(dialog.IsOverlayVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Session_closes_only_its_own_presentation()
    {
        var service = new DialogService();
        IDialogSession? firstSession = null;
        IDialogSession? secondSession = null;
        Task<DialogResult> first = service.ShowAsync(DialogRequest.Create(session =>
        {
            firstSession = session;
            return "first";
        }));
        Task<DialogResult> second = service.ShowAsync(DialogRequest.Create(session =>
        {
            secondSession = session;
            return "second";
        }));
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            Assert.NotNull(firstSession);
            Assert.NotNull(secondSession);
            Assert.True(secondSession.Cancel());
            Assert.True((await second).IsCanceled);
            Assert.Equal("first", dialog.DialogContent);
            Assert.False(first.IsCompleted);

            Assert.True(firstSession.Close("done"));
            await FlushDispatcherAsync();

            Assert.Equal("done", (await first).GetValue<string>());
            Assert.True(firstSession.IsClosed);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Dialog_attaches_through_the_host_service_contract()
    {
        var service = new RecordingDialogHostService();
        var dialog = new Dialog { Service = service };
        var window = new Window { Content = dialog };

        window.Show();
        await FlushDispatcherAsync();

        Assert.Same(dialog, service.AttachedHost);
        Assert.Equal(DialogHost.DefaultId, service.AttachedHostId);

        window.Close();

        Assert.True(service.WasDetached);
    }

    [AvaloniaFact]
    public async Task Host_ids_route_presentations_independently()
    {
        const string secondaryHostId = "Secondary";
        var service = new DialogService();
        var primary = new Dialog { Service = service, IsAnimationEnabled = false };
        var secondary = new Dialog
        {
            Service = service,
            HostId = secondaryHostId,
            IsAnimationEnabled = false,
        };
        var primaryWindow = new Window { Content = primary };
        var secondaryWindow = new Window { Content = secondary };

        try
        {
            primaryWindow.Show();
            secondaryWindow.Show();
            Task<DialogResult> primaryResult = service.ShowAsync(new DialogRequest("primary"));
            Task<DialogResult> secondaryResult = service.ShowAsync(new DialogRequest("secondary")
            {
                HostId = secondaryHostId,
            });
            await FlushDispatcherAsync();

            Assert.Equal("primary", primary.DialogContent);
            Assert.Equal("secondary", secondary.DialogContent);
            Assert.True(service.Close(secondaryHostId, "secondary-result"));
            Assert.True(service.Close(result: "primary-result"));
            await FlushDispatcherAsync();

            Assert.Equal("primary-result", (await primaryResult).GetValue<string>());
            Assert.Equal("secondary-result", (await secondaryResult).GetValue<string>());
        }
        finally
        {
            primaryWindow.Close();
            secondaryWindow.Close();
        }
    }

    [Fact]
    public async Task Canceled_queued_request_completes_with_a_canceled_result()
    {
        var service = new DialogService();
        using var cancellation = new CancellationTokenSource();
        Task<DialogResult> result = service.ShowAsync(new DialogRequest("content"), cancellation.Token);

        cancellation.Cancel();

        Assert.True((await result).IsCanceled);
    }

    [AvaloniaFact]
    public async Task Active_cancellation_completes_even_when_closing_is_declined()
    {
        var service = new DialogService();
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        dialog.Closing += (_, args) => args.Cancel = true;
        var window = new Window { Content = dialog };
        using var cancellation = new CancellationTokenSource();

        try
        {
            window.Show();
            Task<DialogResult> result = service.ShowAsync(new DialogRequest("content"), cancellation.Token);
            await FlushDispatcherAsync();

            cancellation.Cancel();
            await FlushDispatcherAsync();

            Assert.True((await result).IsCanceled);
            Assert.False(dialog.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    private static async Task FlushDispatcherAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private sealed class RecordingDialogHostService : IDialogHostService
    {
        public IDialogHost? AttachedHost { get; private set; }

        public string? AttachedHostId { get; private set; }

        public bool WasDetached { get; private set; }

        public Task<DialogResult> ShowAsync(
            DialogRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DialogResult(null, DialogCloseReason.Programmatic));

        public bool Close(string hostId = DialogHost.DefaultId, object? result = null) => false;

        public void Attach(IDialogHost host, string hostId)
        {
            AttachedHost = host;
            AttachedHostId = hostId;
        }

        public void Detach(IDialogHost host, string hostId)
        {
            WasDetached = ReferenceEquals(AttachedHost, host) && AttachedHostId == hostId;
            AttachedHost = null;
            AttachedHostId = null;
        }

        public void Complete(IDialogHost host, string hostId, object? result, DialogCloseReason reason)
        {
        }
    }
}
