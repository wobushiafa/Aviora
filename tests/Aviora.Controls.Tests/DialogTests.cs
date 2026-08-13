using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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
        Assert.Equal(0, dialog.MinDialogWidth);
        Assert.True(double.IsPositiveInfinity(dialog.MaxDialogWidth));
        Assert.Equal(default, dialog.SurfaceCornerRadius);
        Assert.Equal(default, dialog.SurfaceBoxShadow);
        Assert.Equal(default, dialog.SurfacePadding);
        Assert.Equal(default, dialog.SurfaceMargin);
        Assert.False(dialog.IsLightDismissEnabled);
        Assert.False(dialog.IsEscapeKeyEnabled);
        Assert.True(dialog.IsOverlayVisible);
        Assert.True(dialog.IsAnimationEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(180), dialog.AnimationDuration);
        Assert.NotNull(dialog.AnimationEasing);
        Assert.Null(dialog.Title);
        Assert.Null(dialog.Description);
        Assert.Null(dialog.InitialFocus);
        Assert.Null(dialog.RestoreFocusTarget);
    }

    [Fact]
    public void Dialog_service_options_have_safe_defaults()
    {
        var options = new DialogServiceOptions();

        Assert.False(options.IsLightDismissEnabled);
        Assert.False(options.IsEscapeKeyEnabled);
        Assert.True(options.IsOverlayVisible);
        Assert.True(options.IsAnimationEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(180), options.AnimationDuration);
    }

    [Fact]
    public void Dialog_service_options_reject_a_negative_animation_duration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DialogServiceOptions
        {
            AnimationDuration = TimeSpan.FromMilliseconds(-1),
        });
    }

    [Fact]
    public void Legacy_dialog_api_aliases_forward_to_the_canonical_api()
    {
        var easing = new Avalonia.Animation.Easings.SineEaseOut();
#pragma warning disable CS0618
        var legacyOptions = new DialogOptions();
        var dialog = new Dialog { DialogEasing = easing };
        var service = new DialogService(legacyOptions);
#pragma warning restore CS0618

        Assert.Same(easing, dialog.AnimationEasing);
        Assert.NotNull(service);
    }

    [AvaloniaFact]
    public async Task Dialog_surface_defaults_to_white_and_can_be_made_transparent()
    {
        var dialog = new Dialog
        {
            IsOpen = true,
            IsAnimationEnabled = false,
            SurfaceBackground = Brushes.Transparent,
            SurfaceBorderBrush = Brushes.Navy,
            SurfaceBorderThickness = new Thickness(2),
            SurfacePadding = new Thickness(12),
            SurfaceMargin = new Thickness(16),
            SurfaceCornerRadius = new CornerRadius(6),
            SurfaceBoxShadow = new BoxShadows(new BoxShadow { Blur = 8 }),
        };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            var surface = dialog.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_DialogSurface");
            var presenter = dialog.GetVisualDescendants()
                .OfType<ContentControl>()
                .Single(control => control.Name == "PART_DialogPresenter");

            Assert.Equal(Brushes.Transparent, surface.Background);
            Assert.Equal(Brushes.Navy, surface.BorderBrush);
            Assert.Equal(new Thickness(2), surface.BorderThickness);
            Assert.Equal(new CornerRadius(6), surface.CornerRadius);
            Assert.Equal(new BoxShadows(new BoxShadow { Blur = 8 }), surface.BoxShadow);
            Assert.Equal(new Thickness(16), surface.Margin);
            Assert.Equal(new Thickness(12), presenter.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Dialog_surface_defaults_to_white()
    {
        var dialog = new Dialog { IsOpen = true, IsAnimationEnabled = false };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            var surface = dialog.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_DialogSurface");

            Assert.Equal(Brushes.White, surface.Background);
        }
        finally
        {
            window.Close();
        }
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
    public async Task Dialog_exposes_title_and_description_to_automation()
    {
        var dialog = new Dialog
        {
            IsOpen = true,
            IsAnimationEnabled = false,
            Title = "Edit profile",
            Description = "Update your account details.",
        };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            var surface = dialog.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_DialogSurface");

            Assert.Equal("Edit profile", AutomationProperties.GetName(surface));
            Assert.Equal("Update your account details.", AutomationProperties.GetHelpText(surface));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Dialog_prefers_the_specified_initial_and_restore_focus_targets()
    {
        var opener = new Button { Content = "Open" };
        var initial = new TextBox();
        var restoreTarget = new Button { Content = "Other" };
        var dialog = new Dialog
        {
            IsAnimationEnabled = false,
            InitialFocus = initial,
            RestoreFocusTarget = restoreTarget,
            DialogContent = new StackPanel
            {
                Children = { initial, new Button { Content = "Save" } },
            },
        };
        var window = new Window
        {
            Content = new StackPanel { Children = { opener, restoreTarget, dialog } },
        };

        try
        {
            window.Show();
            opener.Focus();
            dialog.IsOpen = true;
            await FlushDispatcherAsync();

            Assert.Same(initial, window.FocusManager?.GetFocusedElement());

            Assert.True(dialog.TryClose());
            await FlushDispatcherAsync();

            Assert.Same(restoreTarget, window.FocusManager?.GetFocusedElement());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Service_request_applies_dialog_automation_metadata()
    {
        var service = new DialogService();
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            Task<DialogResult> result = service.ShowAsync(new DialogRequest("content")
            {
                Title = "Confirm deletion",
                Description = "This action cannot be undone.",
            });
            await FlushDispatcherAsync();

            Assert.Equal("Confirm deletion", dialog.Title);
            Assert.Equal("This action cannot be undone.", dialog.Description);

            dialog.TryClose();
            await result;
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Navigate_request_does_not_inherit_automation_metadata()
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
                Title = "Child dialog",
                Description = "Child description.",
                PresentationMode = DialogPresentationMode.Navigate,
            });
            await FlushDispatcherAsync();

            Assert.Equal("Child dialog", dialog.Title);
            Assert.Equal("Child description.", dialog.Description);

            Assert.True(service.Close());
            await FlushDispatcherAsync();
            await child;

            Assert.Null(dialog.Title);
            Assert.Null(dialog.Description);

            dialog.TryClose();
            await FlushDispatcherAsync();
            await parent;
        }
        finally
        {
            window.Close();
        }
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
            Assert.False(dialog.IsEscapeKeyEnabled);
            Assert.True(dialog.IsOverlayVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Global_options_apply_and_request_options_take_precedence()
    {
        var service = new DialogService(new DialogServiceOptions
        {
            IsLightDismissEnabled = true,
            IsEscapeKeyEnabled = true,
            IsOverlayVisible = false,
            IsAnimationEnabled = false,
            AnimationDuration = TimeSpan.FromMilliseconds(5),
        });
        var dialog = new Dialog { Service = service };
        var window = new Window { Content = dialog };

        try
        {
            window.Show();
            Task<DialogResult> first = service.ShowAsync(new DialogRequest("global"));
            await FlushDispatcherAsync();

            Assert.True(dialog.IsLightDismissEnabled);
            Assert.True(dialog.IsEscapeKeyEnabled);
            Assert.False(dialog.IsOverlayVisible);
            Assert.False(dialog.IsAnimationEnabled);
            Assert.Equal(TimeSpan.FromMilliseconds(5), dialog.AnimationDuration);

            dialog.TryClose();
            await first;

            Task<DialogResult> second = service.ShowAsync(new DialogRequest("request")
            {
                IsLightDismissEnabled = false,
                IsEscapeKeyEnabled = false,
                IsOverlayVisible = true,
                IsAnimationEnabled = false,
                AnimationDuration = TimeSpan.FromMilliseconds(10),
            });
            await FlushDispatcherAsync();

            Assert.False(dialog.IsLightDismissEnabled);
            Assert.False(dialog.IsEscapeKeyEnabled);
            Assert.True(dialog.IsOverlayVisible);
            Assert.False(dialog.IsAnimationEnabled);
            Assert.Equal(TimeSpan.FromMilliseconds(10), dialog.AnimationDuration);

            dialog.TryClose();
            await second;
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
    public async Task Active_cancellation_respects_a_declined_close()
    {
        var service = new DialogService();
        var dialog = new Dialog { Service = service, IsAnimationEnabled = false };
        EventHandler<DialogClosingEventArgs> rejectClose = (_, args) => args.Cancel = true;
        dialog.Closing += rejectClose;
        var window = new Window { Content = dialog };
        using var cancellation = new CancellationTokenSource();

        try
        {
            window.Show();
            Task<DialogResult> result = service.ShowAsync(new DialogRequest("content"), cancellation.Token);
            await FlushDispatcherAsync();

            cancellation.Cancel();
            await FlushDispatcherAsync();

            Assert.False(result.IsCompleted);
            Assert.True(dialog.IsOpen);

            dialog.Closing -= rejectClose;
            Assert.True(dialog.TryClose());
            Assert.Equal(DialogCloseReason.Programmatic, (await result).Reason);
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
