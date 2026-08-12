using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aviora.Presentation.Loadings;

namespace Aviora.Controls.Tests;

public class LoadingOverlayTests
{
    [Fact]
    public void LoadingOverlay_has_safe_defaults()
    {
        var overlay = new LoadingOverlay();

        Assert.False(overlay.IsOpen);
        Assert.Equal(LoadingHost.DefaultId, overlay.HostId);
        Assert.Equal(LoadingIndicatorStyle.Ring, overlay.IndicatorStyle);
        Assert.Equal(48, overlay.IndicatorSize);
        Assert.Equal(TimeSpan.Zero, overlay.ShowDelay);
        Assert.Equal(TimeSpan.Zero, overlay.MinimumShowDuration);
        Assert.Equal(TimeSpan.Zero, overlay.CloseDelay);
    }

    [AvaloniaFact]
    public async Task Loading_content_container_uses_the_ambient_foreground()
    {
        var overlay = new LoadingOverlay { IsOpen = true };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            StackPanel contentContainer = Assert.Single(overlay.GetVisualDescendants().OfType<StackPanel>());

            Assert.Equal(Brushes.Black, contentContainer.GetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Requests_created_before_attachment_are_presented()
    {
        var service = new LoadingService();
        using ILoadingSession session = service.Show(new LoadingRequest("Preparing workspace"));
        var overlay = new LoadingOverlay { Service = service };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            Assert.True(overlay.IsOpen);
            Assert.Equal("Preparing workspace", overlay.LoadingContent);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Concurrent_sessions_keep_the_overlay_open_and_restore_previous_content()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay { Service = service };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            ILoadingSession first = service.Show(new LoadingRequest("First operation"));
            ILoadingSession second = service.Show(new LoadingRequest("Second operation"));
            await FlushDispatcherAsync();

            Assert.True(overlay.IsOpen);
            Assert.Equal("Second operation", overlay.LoadingContent);

            Assert.True(second.Close());
            await FlushDispatcherAsync();

            Assert.True(overlay.IsOpen);
            Assert.Equal("First operation", overlay.LoadingContent);

            Assert.True(first.Close());
            await FlushDispatcherAsync();

            Assert.False(overlay.IsOpen);
            Assert.Null(overlay.LoadingContent);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task RunAsync_closes_the_overlay_after_success_and_returns_the_result()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay { Service = service };
        var window = new Window { Content = overlay };
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            window.Show();
            Task<int> operation = service.RunAsync(
                () => completion.Task,
                new LoadingRequest("Loading dashboard"));
            await FlushDispatcherAsync();

            Assert.True(overlay.IsOpen);
            completion.SetResult(42);
            Assert.Equal(42, await operation);
            await FlushDispatcherAsync();

            Assert.False(overlay.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task RunAsync_closes_the_overlay_when_the_operation_fails()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay { Service = service };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(
                () => Task.FromException(new InvalidOperationException("failure")),
                new LoadingRequest("Failing operation")));
            await FlushDispatcherAsync();

            Assert.False(overlay.IsOpen);
            Assert.Null(overlay.LoadingContent);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task RunAsync_closes_the_overlay_when_the_operation_is_canceled()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay { Service = service };
        var window = new Window { Content = overlay };
        using var cancellation = new CancellationTokenSource();

        try
        {
            window.Show();
            Task operation = service.RunAsync(
                async cancellationToken => await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
                new LoadingRequest("Cancelable operation"),
                cancellation.Token);
            await FlushDispatcherAsync();
            Assert.True(overlay.IsOpen);

            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            await FlushDispatcherAsync();

            Assert.False(overlay.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Host_ids_route_loading_presentations_independently()
    {
        const string secondaryHostId = "Secondary";
        var service = new LoadingService();
        var primary = new LoadingOverlay { Service = service };
        var secondary = new LoadingOverlay { Service = service, HostId = secondaryHostId };
        var primaryWindow = new Window { Content = primary };
        var secondaryWindow = new Window { Content = secondary };

        try
        {
            primaryWindow.Show();
            secondaryWindow.Show();
            using ILoadingSession primarySession = service.Show(new LoadingRequest("Primary"));
            using ILoadingSession secondarySession = service.Show(new LoadingRequest("Secondary")
            {
                HostId = secondaryHostId,
            });
            await FlushDispatcherAsync();

            Assert.Equal("Primary", primary.LoadingContent);
            Assert.Equal("Secondary", secondary.LoadingContent);
            Assert.True(primary.IsOpen);
            Assert.True(secondary.IsOpen);
        }
        finally
        {
            primaryWindow.Close();
            secondaryWindow.Close();
        }
    }

    [AvaloniaFact]
    public async Task Show_delay_suppresses_short_presentations()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay
        {
            Service = service,
            ShowDelay = TimeSpan.FromMilliseconds(50),
        };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            ILoadingSession session = service.Show();
            await FlushDispatcherAsync();
            Assert.False(overlay.IsOpen);

            session.Dispose();
            await FlushDispatcherAsync();
            await Task.Delay(80);
            await FlushDispatcherAsync();

            Assert.False(overlay.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Minimum_show_duration_prevents_flashing_after_open()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay
        {
            Service = service,
            MinimumShowDuration = TimeSpan.FromMilliseconds(60),
        };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            ILoadingSession session = service.Show();
            await FlushDispatcherAsync();
            Assert.True(overlay.IsOpen);

            session.Dispose();
            await FlushDispatcherAsync();
            Assert.True(overlay.IsOpen);

            await Task.Delay(90);
            await FlushDispatcherAsync();
            Assert.False(overlay.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Close_delay_keeps_the_overlay_visible_after_the_final_session_closes()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay
        {
            Service = service,
            CloseDelay = TimeSpan.FromMilliseconds(60),
        };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            ILoadingSession session = service.Show(new LoadingRequest("Quick operation"));
            await FlushDispatcherAsync();
            Assert.True(overlay.IsOpen);

            session.Dispose();
            await FlushDispatcherAsync();
            Assert.True(overlay.IsOpen);

            await Task.Delay(90);
            await FlushDispatcherAsync();
            Assert.False(overlay.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task A_new_session_cancels_a_pending_delayed_close()
    {
        var service = new LoadingService();
        var overlay = new LoadingOverlay
        {
            Service = service,
            CloseDelay = TimeSpan.FromMilliseconds(70),
        };
        var window = new Window { Content = overlay };

        try
        {
            window.Show();
            ILoadingSession first = service.Show(new LoadingRequest("First"));
            await FlushDispatcherAsync();
            first.Dispose();
            await FlushDispatcherAsync();

            await Task.Delay(25);
            using ILoadingSession second = service.Show(new LoadingRequest("Second"));
            await FlushDispatcherAsync();
            await Task.Delay(80);
            await FlushDispatcherAsync();

            Assert.True(overlay.IsOpen);
            Assert.Equal("Second", overlay.LoadingContent);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task LoadingOverlay_attaches_through_the_host_service_contract()
    {
        var service = new RecordingLoadingHostService();
        var overlay = new LoadingOverlay { Service = service };
        var window = new Window { Content = overlay };

        window.Show();
        await FlushDispatcherAsync();

        Assert.Same(overlay, service.AttachedHost);
        Assert.Equal(LoadingHost.DefaultId, service.AttachedHostId);

        window.Close();
        Assert.True(service.WasDetached);
    }

    private static async Task FlushDispatcherAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private sealed class RecordingLoadingHostService : ILoadingHostService
    {
        public ILoadingHost? AttachedHost { get; private set; }

        public string? AttachedHostId { get; private set; }

        public bool WasDetached { get; private set; }

        public ILoadingSession Show(LoadingRequest? request = null) => new RecordingLoadingSession();

        public void Attach(ILoadingHost host, string hostId)
        {
            AttachedHost = host;
            AttachedHostId = hostId;
        }

        public void Detach(ILoadingHost host, string hostId)
        {
            WasDetached = ReferenceEquals(AttachedHost, host) && AttachedHostId == hostId;
        }
    }

    private sealed class RecordingLoadingSession : ILoadingSession
    {
        public bool IsClosed { get; private set; }

        public bool Close()
        {
            if (IsClosed)
            {
                return false;
            }

            IsClosed = true;
            return true;
        }

        public void Dispose() => Close();
    }
}
