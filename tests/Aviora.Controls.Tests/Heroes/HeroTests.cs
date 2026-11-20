using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Aviora.Controls.Tests;

public class HeroTests
{
    [Fact]
    public void Interpolate_rect_linear_matches_endpoints_and_midpoint()
    {
        Rect from = new(10, 20, 40, 40);
        Rect to = new(110, 220, 140, 80);

        Assert.Equal(from, HeroHost.InterpolateRect(from, to, 0, HeroFlightMode.Linear));
        Assert.Equal(to, HeroHost.InterpolateRect(from, to, 1, HeroFlightMode.Linear));
        Assert.Equal(new Rect(60, 120, 90, 60), HeroHost.InterpolateRect(from, to, 0.5, HeroFlightMode.Linear));
    }

    [Fact]
    public void Interpolate_rect_arc_matches_endpoints_and_curves_midpoint()
    {
        Rect from = new(0, 0, 40, 40);
        Rect to = new(200, 0, 40, 40);

        Assert.Equal(from, HeroHost.InterpolateRect(from, to, 0, HeroFlightMode.Arc));
        Assert.Equal(to, HeroHost.InterpolateRect(from, to, 1, HeroFlightMode.Arc));

        Rect mid = HeroHost.InterpolateRect(from, to, 0.5, HeroFlightMode.Arc);
        Assert.Equal(120, mid.Center.X, 3);
        Assert.True(mid.Center.Y > from.Center.Y);
    }

    [Fact]
    public void Interpolate_corner_radius_interpolates_each_corner()
    {
        CornerRadius from = new(28, 28, 28, 28);
        CornerRadius to = new(8, 12, 16, 4);

        CornerRadius mid = HeroHost.InterpolateCornerRadius(from, to, 0.5);
        Assert.Equal(18, mid.TopLeft, 3);
        Assert.Equal(20, mid.TopRight, 3);
        Assert.Equal(22, mid.BottomRight, 3);
        Assert.Equal(16, mid.BottomLeft, 3);
    }

    [AvaloniaFact]
    public void Swapping_content_with_matching_hero_tags_starts_and_completes_flight()
    {
        Hero sourceHero = CreateSizedHero("card-1", 10, 20, 80, 60, Brushes.Teal);
        HeroHost host = new()
        {
            Duration = TimeSpan.FromMilliseconds(250),
            Content = CreateCanvasWithChild(sourceHero),
        };

        Window window = new() { Width = 600, Height = 400, Content = host };
        HeroFlightEventArgs? started = null;
        HeroFlightEventArgs? completed = null;
        host.HeroFlightStarted += (_, args) => started = args;
        host.HeroFlightCompleted += (_, args) => completed = args;

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Hero destHero = CreateSizedHero("card-1", 260, 140, 220, 120, Brushes.Coral);
        host.Content = CreateCanvasWithChild(destHero);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(started);
        Assert.Equal("card-1", started!.Tag);
        Assert.Equal(new Rect(10, 20, 80, 60), started.SourceBounds);
        Assert.Equal(new Rect(260, 140, 220, 120), started.DestinationBounds);
        Assert.Equal(1, host.ActiveFlightCount);
        Assert.Equal(0, destHero.Opacity);

        Canvas? overlay = host.GetTemplateDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == HeroHost.OverlayLayerPartName);
        Assert.NotNull(overlay);
        Assert.Single(overlay!.Children);

        host.CancelAllFlights();
        Assert.Equal(0, host.ActiveFlightCount);
        Assert.Equal(1, destHero.Opacity);
        Assert.Empty(overlay.Children);
        Assert.NotNull(completed);
        Assert.Equal("card-1", completed!.Tag);

        window.Close();
    }

    [AvaloniaFact]
    public void Toggling_is_visible_between_matching_heroes_starts_flight()
    {
        Hero compactHero = CreateSizedHero("avatar", 20, 20, 48, 48, Brushes.SteelBlue);
        Hero expandedHero = CreateSizedHero("avatar", 180, 100, 160, 120, Brushes.SteelBlue);
        expandedHero.IsVisible = false;

        Canvas canvas = new();
        canvas.Children.Add(compactHero);
        canvas.Children.Add(expandedHero);

        HeroHost host = new() { Content = canvas };
        Window window = new() { Width = 600, Height = 400, Content = host };

        int startedCount = 0;
        host.HeroFlightStarted += (_, _) => startedCount++;

        window.Show();
        Dispatcher.UIThread.RunJobs();

        compactHero.IsVisible = false;
        expandedHero.IsVisible = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, startedCount);
        Assert.Equal(1, host.ActiveFlightCount);
        Assert.Equal(0, expandedHero.Opacity);

        host.CancelAllFlights();
        window.Close();
    }

    [AvaloniaFact]
    public void Non_matching_tags_or_disabled_animation_do_not_start_flight()
    {
        Hero sourceHero = CreateSizedHero("alpha", 10, 10, 60, 60, Brushes.Teal);
        HeroHost host = new() { Content = CreateCanvasWithChild(sourceHero) };
        Window window = new() { Width = 600, Height = 400, Content = host };

        int startedCount = 0;
        host.HeroFlightStarted += (_, _) => startedCount++;

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Hero mismatchHero = CreateSizedHero("beta", 200, 120, 100, 80, Brushes.Orange);
        host.Content = CreateCanvasWithChild(mismatchHero);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, startedCount);
        Assert.Equal(0, host.ActiveFlightCount);

        host.IsAnimationEnabled = false;
        Hero matchingHero = CreateSizedHero("beta", 300, 160, 120, 90, Brushes.Purple);
        host.Content = CreateCanvasWithChild(matchingHero);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, startedCount);
        Assert.Equal(0, host.ActiveFlightCount);
        Assert.Equal(1, matchingHero.Opacity);

        window.Close();
    }

    [AvaloniaFact]
    public void Custom_shuttle_template_is_used_when_specified()
    {
        Hero sourceHero = CreateSizedHero("custom", 10, 10, 50, 50, Brushes.Green);
        HeroHost host = new() { Content = CreateCanvasWithChild(sourceHero) };
        Window window = new() { Width = 600, Height = 400, Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Hero destHero = CreateSizedHero("custom", 200, 150, 100, 100, Brushes.Red);
        destHero.ShuttleTemplate = new FuncDataTemplate<object?>((_, _) => new TextBlock { Name = "CustomShuttleText", Text = "Flying" });

        host.Content = CreateCanvasWithChild(destHero);
        Dispatcher.UIThread.RunJobs();

        Canvas? overlay = host.GetTemplateDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == HeroHost.OverlayLayerPartName);
        Assert.NotNull(overlay);
        Border shuttle = Assert.IsType<Border>(Assert.Single(overlay!.Children));
        TextBlock text = Assert.IsType<TextBlock>(shuttle.Child);
        Assert.Equal("CustomShuttleText", text.Name);
        Assert.Equal(0, host.SnapshotCaptureCount);

        host.CancelAllFlights();
        window.Close();
    }

    [AvaloniaFact]
    public void Hero_transition_control_swaps_content_and_triggers_hero_flight()
    {
        Hero sourceHero = CreateSizedHero("page-hero", 10, 10, 60, 60, Brushes.Teal);
        HeroTransitionControl transition = new()
        {
            TransitionDuration = TimeSpan.FromMilliseconds(200),
            Content = CreateCanvasWithChild(sourceHero),
        };
        HeroHost host = new() { Content = transition };
        Window window = new() { Width = 600, Height = 400, Content = host };

        int started = 0;
        host.HeroFlightStarted += (_, _) => started++;

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Hero destHero = CreateSizedHero("page-hero", 200, 120, 160, 100, Brushes.Coral);
        transition.Content = CreateCanvasWithChild(destHero);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, started);
        Assert.Equal(1, host.ActiveFlightCount);

        host.CancelAllFlights();
        window.Close();
    }

    [AvaloniaFact]
    public void Hero_overlay_open_and_close_triggers_enter_and_exit_flights()
    {
        Hero sourceHero = CreateSizedHero("fs-1", 20, 20, 80, 60, Brushes.Teal);
        Hero overlayHero = CreateSizedHero("fs-1", 40, 40, 480, 300, Brushes.Teal);

        HeroOverlay overlay = new()
        {
            TransitionDuration = TimeSpan.FromMilliseconds(220),
            Content = CreateCanvasWithChild(overlayHero),
        };

        Grid root = new();
        root.Children.Add(CreateCanvasWithChild(sourceHero));
        root.Children.Add(overlay);

        HeroHost host = new() { Content = root };
        Window window = new() { Width = 600, Height = 400, Content = host };

        List<HeroFlightEventArgs> flights = [];
        host.HeroFlightStarted += (_, args) => flights.Add(args);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Enter fullscreen
        overlay.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(overlay.IsVisible);
        Assert.Single(flights);
        Assert.Equal(new Rect(20, 20, 80, 60), flights[0].SourceBounds);
        Assert.Equal(new Rect(40, 40, 480, 300), flights[0].DestinationBounds);

        host.CancelAllFlights();

        // Exit fullscreen
        overlay.IsOpen = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, flights.Count);
        Assert.Equal(new Rect(40, 40, 480, 300), flights[1].SourceBounds);
        Assert.Equal(new Rect(20, 20, 80, 60), flights[1].DestinationBounds);

        host.CancelAllFlights();
        window.Close();
    }

    [AvaloniaFact]
    public void Large_fullscreen_flight_attaches_composition_visual_without_ui_corner_loop()
    {
        Hero sourceHero = CreateSizedHero("fs-comp", 20, 20, 200, 120, Brushes.Teal);
        Hero overlayHero = CreateSizedHero("fs-comp", 40, 40, 520, 320, Brushes.Teal);
        overlayHero.CornerRadius = new CornerRadius(20);

        HeroOverlay overlay = new()
        {
            TransitionDuration = TimeSpan.FromMilliseconds(220),
            Content = CreateCanvasWithChild(overlayHero),
        };

        Grid root = new();
        root.Children.Add(CreateCanvasWithChild(sourceHero));
        root.Children.Add(overlay);

        HeroHost host = new() { Content = root };
        Window window = new() { Width = 600, Height = 400, Content = host };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        overlay.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        Canvas? overlayLayer = host.GetTemplateDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == HeroHost.OverlayLayerPartName);
        Assert.NotNull(overlayLayer);
        Border shuttle = Assert.IsType<Border>(Assert.Single(overlayLayer!.Children));
        Assert.NotNull(ElementComposition.GetElementVisual(shuttle));

        host.CancelAllFlights();
        window.Close();
    }

    [AvaloniaFact]
    public void Direct_shuttle_mode_runs_fullscreen_enter_and_exit_with_zero_snapshots()
    {
        Hero sourceHero = CreateSizedHero("video-1", 20, 20, 200, 120, Brushes.Teal);
        Hero overlayHero = CreateSizedHero("video-1", 40, 40, 520, 320, Brushes.Teal);
        sourceHero.ShuttleMode = HeroShuttleMode.Direct;
        overlayHero.ShuttleMode = HeroShuttleMode.Direct;

        HeroOverlay overlay = new()
        {
            TransitionDuration = TimeSpan.FromMilliseconds(220),
            Content = CreateCanvasWithChild(overlayHero),
        };

        Grid root = new();
        root.Children.Add(CreateCanvasWithChild(sourceHero));
        root.Children.Add(overlay);

        HeroHost host = new() { Content = root };
        Window window = new() { Width = 600, Height = 400, Content = host };

        List<HeroFlightEventArgs> flights = [];
        host.HeroFlightStarted += (_, args) => flights.Add(args);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Enter fullscreen: animates overlayHero directly, zero snapshots, zero overlay shuttles.
        overlay.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        Canvas? overlayLayer = host.GetTemplateDescendants().OfType<Canvas>().FirstOrDefault(c => c.Name == HeroHost.OverlayLayerPartName);
        Assert.NotNull(overlayLayer);
        Assert.Single(flights);
        Assert.Equal(1, host.ActiveFlightCount);
        Assert.Equal(0, host.SnapshotCaptureCount);
        Assert.Empty(overlayLayer!.Children);
        Assert.True(overlayHero.IsDirectFlightActive);
        Assert.Equal(1, overlayHero.Opacity);
        Assert.Equal(0, sourceHero.Opacity);

        host.CancelAllFlights();
        Assert.False(overlayHero.IsDirectFlightActive);
        Assert.Equal(1, sourceHero.Opacity);

        // Exit fullscreen: animates overlayHero directly back to sourceHero, zero snapshots.
        overlay.IsOpen = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, flights.Count);
        Assert.Equal(1, host.ActiveFlightCount);
        Assert.Equal(0, host.SnapshotCaptureCount);
        Assert.Empty(overlayLayer.Children);
        Assert.True(overlayHero.IsDirectFlightActive);
        Assert.Equal(1, overlayHero.Opacity);
        Assert.Equal(0, sourceHero.Opacity);

        host.CancelAllFlights();
        Assert.False(overlayHero.IsDirectFlightActive);
        Assert.Equal(1, sourceHero.Opacity);
        window.Close();
    }

    private static Hero CreateSizedHero(string tag, double left, double top, double width, double height, IBrush fill)
    {
        Hero hero = new()
        {
            Tag = tag,
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(12),
            Content = new Border
            {
                Background = fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            },
        };
        Canvas.SetLeft(hero, left);
        Canvas.SetTop(hero, top);
        return hero;
    }

    private static Canvas CreateCanvasWithChild(Control child)
    {
        Canvas canvas = new();
        canvas.Children.Add(child);
        return canvas;
    }
}
