using System.Windows.Input;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Aviora.Presentation.Toasts;

namespace Aviora.Controls.Tests;

public class ToastTests
{
    [Fact]
    public void ToastHost_has_usable_defaults()
    {
        var host = new ToastHost();

        Assert.Equal(ToastPlacement.TopRight, host.Placement);
        Assert.Equal(TimeSpan.FromSeconds(4), host.DefaultDuration);
        Assert.Equal(0, host.MaxVisible);
        Assert.True(host.IsDismissible);
        Assert.True(host.IsClickDismissEnabled);
        Assert.True(host.PauseOnPointerOver);
        Assert.True(host.IsAnimationEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(220), host.EntryAnimationDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(150), host.ExitAnimationDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(180), host.ReflowAnimationDuration);
        Assert.NotNull(host.EntryAnimationEasing);
        Assert.NotNull(host.ExitAnimationEasing);
        Assert.Equal(12, host.SlideDistance);
        Assert.Equal(8, host.ToastSpacing);
        Assert.Equal(new Thickness(16), host.ToastMargin);
    }

    [Fact]
    public void Toast_exposes_a_dismiss_command()
    {
        var toast = new Toast();

        Assert.NotNull(toast.DismissCommand);
    }

    [Fact]
    public void Legacy_toast_api_aliases_forward_to_the_canonical_api()
    {
        var host = new ToastHost();
        var entryEasing = new SineEaseOut();
        var exitEasing = new SineEaseIn();
        var toast = new Toast();
#pragma warning disable CS0618
        host.AnimationDuration = TimeSpan.FromMilliseconds(321);
        host.EntryEasing = entryEasing;
        host.ExitEasing = exitEasing;
        ICommand legacyCloseCommand = toast.CloseCommand;
#pragma warning restore CS0618

        Assert.Equal(TimeSpan.FromMilliseconds(321), host.EntryAnimationDuration);
        Assert.Same(entryEasing, host.EntryAnimationEasing);
        Assert.Same(exitEasing, host.ExitAnimationEasing);
        Assert.Same(toast.DismissCommand, legacyCloseCommand);
    }

    [AvaloniaFact]
    public async Task Requests_created_before_attachment_are_presented_and_dismissed()
    {
        var service = new ToastService();
        IToastSession session = service.Show(new ToastRequest("Workspace ready")
        {
            Title = "Completed",
            Severity = ToastSeverity.Success,
            IsClickDismissEnabled = false,
        });
        var host = CreateHost(service);
        var window = new Window { Content = host };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            Toast toast = Assert.Single(host.ActiveToasts);
            Assert.Equal("Completed", toast.Title);
            Assert.Equal("Workspace ready", toast.Content);
            Assert.Equal(ToastSeverity.Success, toast.Severity);
            Assert.False(toast.IsClickDismissEnabled);

            Assert.True(session.Dismiss());
            await FlushDispatcherAsync();

            Assert.Equal(ToastDismissReason.Programmatic, await session.Completion);
            Assert.Empty(host.ActiveToasts);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MaxVisible_queues_and_promotes_notifications_in_order()
    {
        var service = new ToastService();
        var host = CreateHost(service);
        host.MaxVisible = 1;
        var window = new Window { Content = host };

        try
        {
            window.Show();
            IToastSession first = service.Show(Persistent("first"));
            IToastSession second = service.Show(Persistent("second"));
            IToastSession third = service.Show(Persistent("third"));
            await FlushDispatcherAsync();

            Assert.Equal("first", Assert.Single(host.ActiveToasts).Content);
            Assert.Equal(2, host.WaitingCount);

            Assert.True(first.Dismiss());
            await FlushDispatcherAsync();

            Assert.Equal("second", Assert.Single(host.ActiveToasts).Content);
            Assert.Equal(1, host.WaitingCount);
            Assert.Equal(2, service.Clear());
            await FlushDispatcherAsync();

            Assert.Equal(ToastDismissReason.Cleared, await second.Completion);
            Assert.Equal(ToastDismissReason.Cleared, await third.Completion);
            Assert.Empty(host.ActiveToasts);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task MaxVisible_zero_does_not_limit_visible_notifications()
    {
        var service = new ToastService();
        var host = CreateHost(service);
        var window = new Window { Content = host };

        try
        {
            window.Show();
            service.Show(Persistent("first"));
            service.Show(Persistent("second"));
            service.Show(Persistent("third"));
            service.Show(Persistent("fourth"));
            await FlushDispatcherAsync();

            Assert.Equal(4, host.ActiveToasts.Count);
            Assert.Equal(0, host.WaitingCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Requests_can_mix_placements_within_one_host()
    {
        var service = new ToastService();
        var host = CreateHost(service);
        var window = new Window { Content = host };

        try
        {
            window.Show();
            service.Show(new ToastRequest("left")
            {
                Duration = Timeout.InfiniteTimeSpan,
                Placement = ToastPlacement.TopLeft,
            });
            service.Show(new ToastRequest("right")
            {
                Duration = Timeout.InfiniteTimeSpan,
                Placement = ToastPlacement.BottomRight,
            });
            await FlushDispatcherAsync();

            Toast left = host.ActiveToasts.Single(toast => toast.Placement == ToastPlacement.TopLeft);
            Toast right = host.ActiveToasts.Single(toast => toast.Placement == ToastPlacement.BottomRight);
            Assert.Equal(HorizontalAlignment.Left, left.HorizontalAlignment);
            Assert.Equal(HorizontalAlignment.Right, right.HorizontalAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ToastTemplate_is_applied_to_generated_controls()
    {
        IDataTemplate template = new FuncDataTemplate<string>(
            (value, _) => new TextBlock { Text = value?.ToUpperInvariant() });
        var service = new ToastService();
        var host = CreateHost(service);
        host.ToastTemplate = template;
        var window = new Window { Content = host };

        try
        {
            window.Show();
            service.Show(Persistent("custom content"));
            await FlushDispatcherAsync();

            Assert.Same(template, Assert.Single(host.ActiveToasts).ContentTemplate);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Action_command_can_dismiss_with_the_action_reason()
    {
        var command = new RecordingCommand();
        var service = new ToastService();
        var host = CreateHost(service);
        var window = new Window { Content = host };
        object parameter = new();

        try
        {
            window.Show();
            IToastSession session = service.Show(new ToastRequest("A newer version is available")
            {
                Duration = Timeout.InfiniteTimeSpan,
                ActionText = "Install",
                ActionCommand = command,
                ActionCommandParameter = parameter,
            });
            await FlushDispatcherAsync();

            Toast toast = Assert.Single(host.ActiveToasts);
            Assert.NotNull(toast.ActionCommand);
            toast.ActionCommand.Execute(null);
            await FlushDispatcherAsync();

            Assert.Same(parameter, command.Parameter);
            Assert.Equal(ToastDismissReason.Action, await session.Completion);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Toast_auto_dismisses_after_its_duration()
    {
        var service = new ToastService();
        var host = CreateHost(service);
        host.DefaultDuration = TimeSpan.FromMilliseconds(30);
        var window = new Window { Content = host };

        try
        {
            window.Show();
            IToastSession session = service.Show(new ToastRequest("Saved"));
            await FlushDispatcherAsync();
            await Task.Delay(80);
            await FlushDispatcherAsync();

            Assert.Equal(ToastDismissReason.Timeout, await session.Completion);
            Assert.Empty(host.ActiveToasts);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task Cancellation_before_attachment_completes_the_session()
    {
        var service = new ToastService();
        using var cancellation = new CancellationTokenSource();
        IToastSession session = service.Show(Persistent("pending"), cancellation.Token);

        cancellation.Cancel();

        Assert.Equal(ToastDismissReason.Canceled, await session.Completion);
        Assert.True(session.IsDismissed);
    }

    [AvaloniaFact]
    public async Task Host_ids_route_notifications_independently()
    {
        const string secondaryId = "Secondary";
        var service = new ToastService();
        var primary = CreateHost(service);
        var secondary = CreateHost(service);
        secondary.HostId = secondaryId;
        var primaryWindow = new Window { Content = primary };
        var secondaryWindow = new Window { Content = secondary };

        try
        {
            primaryWindow.Show();
            secondaryWindow.Show();
            service.Show(Persistent("primary"));
            service.Show(new ToastRequest("secondary")
            {
                HostId = secondaryId,
                Duration = Timeout.InfiniteTimeSpan,
            });
            await FlushDispatcherAsync();

            Assert.Equal("primary", Assert.Single(primary.ActiveToasts).Content);
            Assert.Equal("secondary", Assert.Single(secondary.ActiveToasts).Content);
        }
        finally
        {
            primaryWindow.Close();
            secondaryWindow.Close();
        }
    }

    [AvaloniaFact]
    public async Task Content_factory_receives_the_presentation_session()
    {
        IToastSession? received = null;
        var service = new ToastService();
        var host = CreateHost(service);
        var window = new Window { Content = host };

        try
        {
            window.Show();
            IToastSession session = service.Show(ToastRequest.Create(candidate =>
            {
                received = candidate;
                return "session content";
            }));
            await FlushDispatcherAsync();

            Assert.Same(session, received);
            Assert.Equal("session content", Assert.Single(host.ActiveToasts).Content);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void Invalid_duration_is_rejected()
    {
        var service = new ToastService();

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Show(new ToastRequest("invalid")
        {
            Duration = TimeSpan.Zero,
        }, TestContext.Current.CancellationToken));
    }

    private static ToastHost CreateHost(IToastHostService service) => new()
    {
        Service = service,
        IsAnimationEnabled = false,
        DefaultDuration = Timeout.InfiniteTimeSpan,
    };

    private static ToastRequest Persistent(string content) => new(content)
    {
        Duration = Timeout.InfiniteTimeSpan,
    };

    private static async Task FlushDispatcherAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private sealed class RecordingCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public object? Parameter { get; private set; }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Parameter = parameter;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
