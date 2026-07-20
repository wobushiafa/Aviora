using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aviora.Presentation.Drawers;
using DrawerPlacement = Aviora.Presentation.Drawers.DrawerPlacement;

namespace Aviora.Controls.Tests;

public class DrawerTests
{
    [Fact]
    public void Drawer_has_usable_defaults()
    {
        var drawer = new Drawer();

        Assert.False(drawer.IsOpen);
        Assert.Equal(DrawerPlacement.Right, drawer.Placement);
        Assert.Equal(DrawerDisplayMode.Overlay, drawer.DisplayMode);
        Assert.Equal(360, drawer.DrawerSize);
        Assert.True(drawer.IsLightDismissEnabled);
        Assert.True(drawer.IsEscapeKeyEnabled);
        Assert.True(drawer.IsOverlayVisible);
        Assert.True(drawer.IsAnimationEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(260), drawer.PaneAnimationDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(180), drawer.OverlayAnimationDuration);
    }

    [Fact]
    public void Closing_can_cancel_a_state_change()
    {
        var drawer = new Drawer { IsOpen = true };
        drawer.Closing += (_, args) => args.Cancel = true;

        drawer.IsOpen = false;

        Assert.True(drawer.IsOpen);
    }

    [Fact]
    public void Close_command_returns_its_parameter()
    {
        var drawer = new Drawer { IsOpen = true };
        DrawerClosedEventArgs? closed = null;
        drawer.Closed += (_, args) => closed = args;

        drawer.CloseCommand.Execute("saved");

        Assert.False(drawer.IsOpen);
        Assert.NotNull(closed);
        Assert.Equal(DrawerCloseReason.Command, closed.Reason);
        Assert.Equal("saved", closed.Result);
    }

    [Fact]
    public void Close_command_notifies_when_its_availability_changes()
    {
        var drawer = new Drawer();
        var notifications = 0;
        drawer.CloseCommand.CanExecuteChanged += (_, _) => notifications++;

        Assert.False(drawer.CloseCommand.CanExecute(null));
        drawer.IsOpen = true;

        Assert.True(drawer.CloseCommand.CanExecute(null));
        Assert.Equal(1, notifications);
    }

    [AvaloniaFact]
    public async Task Service_queues_requests_until_a_host_is_attached()
    {
        var service = new DrawerService();
        Task<DrawerResult> first = service.ShowAsync(new DrawerRequest("first"));
        Task<DrawerResult> second = service.ShowAsync(new DrawerRequest("second"));
        var drawer = new Drawer { Service = service, IsAnimationEnabled = false };
        var window = new Window { Content = drawer };

        try
        {
            window.Show();
            await FlushDispatcherAsync();

            Assert.True(drawer.IsOpen);
            Assert.Equal("first", drawer.DrawerContent);
            Assert.False(first.IsCompleted);
            Assert.True(service.Close(result: 42));
            await FlushDispatcherAsync();

            DrawerResult firstResult = await first;
            Assert.Equal(42, firstResult.GetValue<int>());
            Assert.Equal(DrawerCloseReason.Programmatic, firstResult.Reason);
            Assert.True(drawer.IsOpen);
            Assert.Equal("second", drawer.DrawerContent);

            drawer.CloseCommand.Execute("done");
            DrawerResult secondResult = await second;
            Assert.Equal("done", secondResult.GetValue<string>());
            Assert.Equal(DrawerCloseReason.Command, secondResult.Reason);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Request_options_are_restored_after_close()
    {
        var service = new DrawerService();
        var drawer = new Drawer
        {
            Service = service,
            Placement = DrawerPlacement.Left,
            DisplayMode = DrawerDisplayMode.Push,
            DrawerSize = 240,
            IsLightDismissEnabled = false,
            IsAnimationEnabled = true,
            PaneAnimationDuration = TimeSpan.FromMilliseconds(111),
            OverlayAnimationDuration = TimeSpan.FromMilliseconds(88),
            PaneCornerRadius = new Avalonia.CornerRadius(7),
        };
        var window = new Window { Content = drawer };

        try
        {
            window.Show();
            Task<DrawerResult> result = service.ShowAsync(new DrawerRequest("temporary")
            {
                Placement = DrawerPlacement.Bottom,
                DisplayMode = DrawerDisplayMode.Overlay,
                Size = 420,
                IsLightDismissEnabled = true,
                IsAnimationEnabled = false,
                PaneAnimationDuration = TimeSpan.FromMilliseconds(5),
                OverlayAnimationDuration = TimeSpan.FromMilliseconds(4),
            });
            await FlushDispatcherAsync();

            Assert.Equal(DrawerPlacement.Bottom, drawer.Placement);
            Assert.Equal(420, drawer.DrawerSize);
            Assert.False(drawer.IsAnimationEnabled);
            Assert.Equal(TimeSpan.FromMilliseconds(5), drawer.PaneAnimationDuration);
            Assert.Equal(new Avalonia.CornerRadius(7), drawer.PaneCornerRadius);
            drawer.TryClose();
            await result;

            Assert.Equal(DrawerPlacement.Left, drawer.Placement);
            Assert.Equal(DrawerDisplayMode.Push, drawer.DisplayMode);
            Assert.Equal(240, drawer.DrawerSize);
            Assert.False(drawer.IsLightDismissEnabled);
            Assert.True(drawer.IsAnimationEnabled);
            Assert.Equal(TimeSpan.FromMilliseconds(111), drawer.PaneAnimationDuration);
            Assert.Equal(TimeSpan.FromMilliseconds(88), drawer.OverlayAnimationDuration);
            Assert.Equal(new Avalonia.CornerRadius(7), drawer.PaneCornerRadius);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(DrawerPlacement.Left, true)]
    [InlineData(DrawerPlacement.Right, true)]
    [InlineData(DrawerPlacement.Top, false)]
    [InlineData(DrawerPlacement.Bottom, false)]
    public void Push_mode_reserves_the_requested_edge(DrawerPlacement placement, bool horizontal)
    {
        var drawer = new Drawer
        {
            IsOpen = true,
            Placement = placement,
            DisplayMode = DrawerDisplayMode.Push,
            DrawerSize = 120,
            IsAnimationEnabled = false,
            DrawerContent = "pane",
            Content = "primary",
        };
        var window = new Window
        {
            Width = 500,
            Height = 400,
            Content = drawer,
        };

        try
        {
            window.Show();
            var pane = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_PaneSurface");

            Assert.Equal(120, horizontal ? pane.Bounds.Width : pane.Bounds.Height);
            if (placement == DrawerPlacement.Left)
            {
                Assert.Equal(0, pane.Bounds.X);
            }
            else if (placement == DrawerPlacement.Right)
            {
                Assert.Equal(drawer.Bounds.Width, pane.Bounds.Right);
            }
            else if (placement == DrawerPlacement.Top)
            {
                Assert.Equal(0, pane.Bounds.Y);
            }
            else
            {
                Assert.Equal(drawer.Bounds.Height, pane.Bounds.Bottom);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Overlay_is_a_neutral_hit_target_and_dismisses_on_release()
    {
        var drawer = new Drawer
        {
            IsOpen = true,
            DrawerSize = 120,
            IsAnimationEnabled = false,
            DrawerContent = "pane",
            Content = "primary",
        };
        var window = new Window
        {
            Width = 500,
            Height = 400,
            Content = drawer,
        };

        try
        {
            window.Show();
            var overlay = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_Overlay");
            var originalBackground = overlay.Background;
            var originalTransform = overlay.RenderTransform;

            window.MouseDown(new Avalonia.Point(40, 200), MouseButton.Left, RawInputModifiers.None);

            Assert.True(drawer.IsOpen);
            Assert.Same(originalBackground, overlay.Background);
            Assert.Same(originalTransform, overlay.RenderTransform);

            window.MouseUp(new Avalonia.Point(40, 200), MouseButton.Left, RawInputModifiers.None);

            Assert.False(drawer.IsOpen);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(DrawerPlacement.Left, -140, 0)]
    [InlineData(DrawerPlacement.Top, 0, -140)]
    [InlineData(DrawerPlacement.Right, 140, 0)]
    [InlineData(DrawerPlacement.Bottom, 0, 140)]
    public void Closed_pane_is_offset_by_its_full_size(DrawerPlacement placement, double expectedX, double expectedY)
    {
        var drawer = new Drawer
        {
            Placement = placement,
            DrawerSize = 140,
            DrawerContent = "pane",
        };
        var window = new Window
        {
            Width = 500,
            Height = 400,
            Content = drawer,
        };

        try
        {
            window.Show();
            var pane = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_PaneSurface");
            var transform = Assert.IsType<TranslateTransform>(pane.RenderTransform);

            Assert.Equal(expectedX, transform.X);
            Assert.Equal(expectedY, transform.Y);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Pane_and_overlay_use_the_configured_transitions()
    {
        var paneEasing = new QuadraticEaseOut();
        var overlayEasing = new SineEaseOut();
        var drawer = new Drawer
        {
            DrawerContent = "pane",
            PaneAnimationDuration = TimeSpan.FromMilliseconds(321),
            OverlayAnimationDuration = TimeSpan.FromMilliseconds(123),
            PaneEasing = paneEasing,
            OverlayEasing = overlayEasing,
        };
        var window = new Window { Content = drawer };

        try
        {
            window.Show();
            var pane = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_PaneSurface");
            var overlay = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_Overlay");
            var transform = Assert.IsType<TranslateTransform>(pane.RenderTransform);
            DoubleTransition[] paneTransitions = Assert.IsType<Transitions>(transform.Transitions)
                .OfType<DoubleTransition>()
                .ToArray();
            DoubleTransition overlayTransition = Assert.Single(
                Assert.IsType<Transitions>(overlay.Transitions).OfType<DoubleTransition>());

            Assert.Equal(2, paneTransitions.Length);
            Assert.All(paneTransitions, transition => Assert.Equal(TimeSpan.FromMilliseconds(321), transition.Duration));
            Assert.All(paneTransitions, transition => Assert.Same(paneEasing, transition.Easing));
            Assert.Equal(TimeSpan.FromMilliseconds(123), overlayTransition.Duration);
            Assert.Same(overlayEasing, overlayTransition.Easing);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Closing_keeps_the_visuals_present_until_the_animation_finishes()
    {
        var drawer = new Drawer
        {
            DrawerSize = 140,
            DrawerContent = "pane",
            PaneAnimationDuration = TimeSpan.FromMilliseconds(30),
            OverlayAnimationDuration = TimeSpan.FromMilliseconds(20),
        };
        var window = new Window { Width = 500, Height = 400, Content = drawer };

        try
        {
            window.Show();
            drawer.IsOpen = true;
            await Task.Delay(60);
            await FlushDispatcherAsync();
            var pane = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_PaneSurface");
            var overlay = drawer.GetVisualDescendants()
                .OfType<Border>()
                .Single(control => control.Name == "PART_Overlay");

            Assert.True(pane.IsVisible);
            Assert.Equal(1, overlay.Opacity);

            Assert.True(drawer.TryClose());

            Assert.True(pane.IsVisible);
            Assert.True(overlay.IsVisible);
            Assert.False(overlay.IsHitTestVisible);

            await Task.Delay(70);
            await FlushDispatcherAsync();

            Assert.False(pane.IsVisible);
            Assert.False(overlay.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task Canceled_queued_request_completes_with_a_canceled_result()
    {
        var service = new DrawerService();
        using var cancellation = new CancellationTokenSource();
        Task<DrawerResult> result = service.ShowAsync(new DrawerRequest("content"), cancellation.Token);

        cancellation.Cancel();
        DrawerResult drawerResult = await result;

        Assert.True(drawerResult.IsCanceled);
    }

    [AvaloniaFact]
    public async Task Active_cancellation_completes_even_when_closing_is_declined()
    {
        var service = new DrawerService();
        var drawer = new Drawer { Service = service };
        drawer.Closing += (_, args) => args.Cancel = true;
        var window = new Window { Content = drawer };
        using var cancellation = new CancellationTokenSource();

        try
        {
            window.Show();
            Task<DrawerResult> result = service.ShowAsync(new DrawerRequest("content"), cancellation.Token);
            await FlushDispatcherAsync();

            cancellation.Cancel();
            await FlushDispatcherAsync();

            Assert.True((await result).IsCanceled);
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
}
