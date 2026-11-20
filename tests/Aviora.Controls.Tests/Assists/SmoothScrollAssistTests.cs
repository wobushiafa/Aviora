using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Aviora.Controls.Tests;

public class SmoothScrollAssistTests
{
    [Fact]
    public void Attached_properties_have_expected_defaults_and_validate_ranges()
    {
        var scroller = new ScrollViewer();

        Assert.False(SmoothScrollAssist.GetIsEnabled(scroller));
        Assert.Equal(SmoothScrollPreset.Default, SmoothScrollAssist.GetPreset(scroller));
        Assert.Equal(SmoothScrollCurveMode.Exponential, SmoothScrollAssist.GetCurveMode(scroller));
        Assert.Equal(SmoothScrollOrientation.Both, SmoothScrollAssist.GetOrientation(scroller));
        Assert.Equal(96.0, SmoothScrollAssist.GetStepSize(scroller));
        Assert.Equal(0.22, SmoothScrollAssist.GetInitialStepFactor(scroller));
        Assert.Equal(18.0, SmoothScrollAssist.GetDecayRate(scroller));
        Assert.Equal(48.0, SmoothScrollAssist.GetMinSpeed(scroller));
        Assert.Equal(TimeSpan.FromMilliseconds(220), SmoothScrollAssist.GetDuration(scroller));
        Assert.Null(SmoothScrollAssist.GetEasing(scroller));
        Assert.Equal(0.75, SmoothScrollAssist.GetSettleThreshold(scroller));
        Assert.True(double.IsPositiveInfinity(SmoothScrollAssist.GetMaxQueueDistance(scroller)));
        Assert.Equal(0.5, SmoothScrollAssist.GetTrackpadThreshold(scroller));
        Assert.True(SmoothScrollAssist.GetIsShiftWheelHorizontalEnabled(scroller));
        Assert.False(SmoothScrollAssist.GetAutoHorizontalWhenVerticalDisabled(scroller));
        Assert.True(SmoothScrollAssist.GetRespectNestedScrollViewers(scroller));

        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetStepSize(scroller, 0));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetStepSize(scroller, double.NaN));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetInitialStepFactor(scroller, -0.01));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetInitialStepFactor(scroller, 1.01));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetDecayRate(scroller, 0));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetMinSpeed(scroller, -1));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetDuration(scroller, TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetSettleThreshold(scroller, 0));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetMaxQueueDistance(scroller, 0));
        Assert.Throws<ArgumentException>(() => SmoothScrollAssist.SetTrackpadThreshold(scroller, -0.1));
    }

    [AvaloniaFact]
    public async Task Exponential_mode_interpolates_to_custom_step_size_and_supports_zero_initial_step()
    {
        Action<TimeSpan>? pendingFrame = null;
        var scroller = CreateScrollViewer(callback =>
        {
            pendingFrame = callback;
            return true;
        });
        SmoothScrollAssist.SetStepSize(scroller, 140);
        SmoothScrollAssist.SetInitialStepFactor(scroller, 0);

        await RunInWindowAsync(scroller, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);

            Assert.Equal(0.0, scroller.Offset.Y, 4);
            Assert.NotNull(pendingFrame);

            AdvanceExponentialFrames(ref pendingFrame);

            Assert.Null(pendingFrame);
            Assert.Equal(140.0, scroller.Offset.Y, 2);
            await Task.CompletedTask;
        });
    }

    [AvaloniaFact]
    public async Task Preset_sets_baseline_physics_while_explicit_property_overrides_preset()
    {
        Action<TimeSpan>? snappyFrame = null;
        var snappy = CreateScrollViewer(callback =>
        {
            snappyFrame = callback;
            return true;
        });
        SmoothScrollAssist.SetPreset(snappy, SmoothScrollPreset.Snappy);

        double snappyInitial = 0;
        await RunInWindowAsync(snappy, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);
            snappyInitial = snappy.Offset.Y;
            Assert.NotNull(snappyFrame);
            await Task.CompletedTask;
        });

        Action<TimeSpan>? silkyFrame = null;
        var silkyOverride = CreateScrollViewer(callback =>
        {
            silkyFrame = callback;
            return true;
        });
        SmoothScrollAssist.SetPreset(silkyOverride, SmoothScrollPreset.Silky);
        SmoothScrollAssist.SetStepSize(silkyOverride, 160);

        await RunInWindowAsync(silkyOverride, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);

            Assert.True(snappyInitial > silkyOverride.Offset.Y);
            Assert.Equal(160.0 * 0.14, silkyOverride.Offset.Y, 2);

            AdvanceExponentialFrames(ref silkyFrame);
            Assert.Equal(160.0, silkyOverride.Offset.Y, 2);
            await Task.CompletedTask;
        });
    }

    [AvaloniaFact]
    public async Task Easing_mode_interpolates_according_to_duration_and_easing_function()
    {
        Action<TimeSpan>? pendingFrame = null;
        var scroller = CreateScrollViewer(callback =>
        {
            pendingFrame = callback;
            return true;
        });
        SmoothScrollAssist.SetCurveMode(scroller, SmoothScrollCurveMode.Easing);
        SmoothScrollAssist.SetDuration(scroller, TimeSpan.FromMilliseconds(200));
        SmoothScrollAssist.SetEasing(scroller, new LinearEasing());

        await RunInWindowAsync(scroller, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);

            Assert.Equal(0.0, scroller.Offset.Y, 4);
            Assert.NotNull(pendingFrame);

            InvokeFrame(ref pendingFrame, TimeSpan.Zero);
            Assert.Equal(0.0, scroller.Offset.Y, 4);
            Assert.NotNull(pendingFrame);

            InvokeFrame(ref pendingFrame, TimeSpan.FromMilliseconds(100));
            Assert.Equal(48.0, scroller.Offset.Y, 2);
            Assert.NotNull(pendingFrame);

            InvokeFrame(ref pendingFrame, TimeSpan.FromMilliseconds(200));
            Assert.Equal(96.0, scroller.Offset.Y, 2);
            Assert.Null(pendingFrame);
            await Task.CompletedTask;
        });
    }

    [AvaloniaFact]
    public async Task Shift_wheel_and_auto_horizontal_and_orientation_filter_work_as_configured()
    {
        Action<TimeSpan>? shiftFrame = null;
        var horizontal = CreateScrollViewer(
            callback =>
            {
                shiftFrame = callback;
                return true;
            },
            contentWidth: 500,
            contentHeight: 500,
            canHorizontallyScroll: true,
            canVerticallyScroll: true,
            horizontalVisibility: ScrollBarVisibility.Auto,
            verticalVisibility: ScrollBarVisibility.Auto);

        await RunInWindowAsync(horizontal, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.Shift);

            Assert.InRange(horizontal.Offset.X, 1.0, 95.0);
            Assert.Equal(0.0, horizontal.Offset.Y, 4);
            AdvanceExponentialFrames(ref shiftFrame);
            Assert.Equal(96.0, horizontal.Offset.X, 2);
            await Task.CompletedTask;
        });

        Action<TimeSpan>? autoFrame = null;
        var gallery = CreateScrollViewer(
            callback =>
            {
                autoFrame = callback;
                return true;
            },
            contentWidth: 500,
            contentHeight: 100,
            canHorizontallyScroll: true,
            canVerticallyScroll: false,
            horizontalVisibility: ScrollBarVisibility.Auto,
            verticalVisibility: ScrollBarVisibility.Disabled);
        SmoothScrollAssist.SetAutoHorizontalWhenVerticalDisabled(gallery, true);

        await RunInWindowAsync(gallery, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);

            Assert.InRange(gallery.Offset.X, 1.0, 95.0);
            AdvanceExponentialFrames(ref autoFrame);
            Assert.Equal(96.0, gallery.Offset.X, 2);
            await Task.CompletedTask;
        });

        Action<TimeSpan>? filteredFrame = null;
        var verticalOnly = CreateScrollViewer(
            callback =>
            {
                filteredFrame = callback;
                return true;
            },
            contentWidth: 500,
            contentHeight: 500,
            canHorizontallyScroll: true,
            canVerticallyScroll: true,
            horizontalVisibility: ScrollBarVisibility.Auto,
            verticalVisibility: ScrollBarVisibility.Auto);
        SmoothScrollAssist.SetOrientation(verticalOnly, SmoothScrollOrientation.Vertical);

        await RunInWindowAsync(verticalOnly, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(-1, 0), RawInputModifiers.None);

            Assert.Null(filteredFrame);
            await Task.CompletedTask;
        });
    }

    [AvaloniaFact]
    public async Task Queue_limit_reversal_and_trackpad_passthrough_are_enforced()
    {
        Action<TimeSpan>? pendingFrame = null;
        var scroller = CreateScrollViewer(
            callback =>
            {
                pendingFrame = callback;
                return true;
            },
            contentHeight: 1200);
        SmoothScrollAssist.SetMaxQueueDistance(scroller, 120);
        SmoothScrollAssist.SetTrackpadThreshold(scroller, 0.5);

        await RunInWindowAsync(scroller, async window =>
        {
            window.MouseMove(new Point(50, 50), RawInputModifiers.None);

            // Rapidly queue multiple notches; total lead should remain capped by MaxQueueDistance.
            for (var i = 0; i < 5; i++)
            {
                window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);
            }

            Assert.NotNull(pendingFrame);
            AdvanceExponentialFrames(ref pendingFrame);
            Assert.InRange(scroller.Offset.Y, 96.0, 260.0);

            // Reverse direction immediately from live position.
            var beforeReverse = scroller.Offset.Y;
            window.MouseWheel(new Point(50, 50), new Vector(0, 1), RawInputModifiers.None);
            Assert.True(scroller.Offset.Y < beforeReverse);
            AdvanceExponentialFrames(ref pendingFrame);
            Assert.Equal(Math.Max(0, beforeReverse - 96.0), scroller.Offset.Y, 2);

            // Fine trackpad delta below TrackpadThreshold passes through without scheduling frames.
            var beforeTrackpad = scroller.Offset.Y;
            window.MouseWheel(new Point(50, 50), new Vector(0, -0.25), RawInputModifiers.None);
            Assert.Null(pendingFrame);
            Assert.Equal(beforeTrackpad + 24.0, scroller.Offset.Y, 2);
            await Task.CompletedTask;
        });
    }

    [AvaloniaFact]
    public async Task Composite_listbox_host_resolves_inner_scroll_viewer_and_cleans_up_on_disable()
    {
        Action<TimeSpan>? pendingFrame = null;
        var listBox = new ListBox
        {
            Width = 200,
            Height = 100,
            ItemsSource = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToArray(),
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new Border
            {
                Height = 30,
                Background = Brushes.Transparent,
            }),
            Template = new FuncControlTemplate<ListBox>((_, scope) =>
            {
                var itemsPresenter = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                };
                scope.Register("PART_ItemsPresenter", itemsPresenter);

                var innerScroller = new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Template = CreateScrollerTemplate(canHorizontallyScroll: false, canVerticallyScroll: true),
                    Content = itemsPresenter,
                };
                scope.Register("PART_ScrollViewer", innerScroller);
                return innerScroller;
            }),
        };

        SmoothScrollAssist.SetIsEnabled(listBox, true);
        SmoothScrollAssist.SetStepSize(listBox, 128);
        SmoothScrollAssist.SetFrameScheduler(listBox, callback =>
        {
            pendingFrame = callback;
            return true;
        });

        await RunInWindowAsync(listBox, async window =>
        {
            var innerScroller = listBox.GetVisualDescendants().OfType<ScrollViewer>().Single();

            window.MouseMove(new Point(50, 50), RawInputModifiers.None);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);

            Assert.InRange(innerScroller.Offset.Y, 1.0, 127.0);
            Assert.NotNull(pendingFrame);

            AdvanceExponentialFrames(ref pendingFrame);
            Assert.Equal(128.0, innerScroller.Offset.Y, 2);

            SmoothScrollAssist.SetIsEnabled(listBox, false);
            window.MouseWheel(new Point(50, 50), new Vector(0, -1), RawInputModifiers.None);
            Assert.Null(pendingFrame);
            await Task.CompletedTask;
        });
    }

    [AvaloniaFact]
    public async Task Nested_scroll_viewers_yield_until_inner_reaches_boundary_and_external_offset_cancels()
    {
        Action<TimeSpan>? outerFrame = null;
        Action<TimeSpan>? innerFrame = null;

        var inner = CreateScrollViewer(
            callback =>
            {
                innerFrame = callback;
                return true;
            },
            width: 160,
            height: 80,
            contentWidth: 160,
            contentHeight: 160);

        var outer = new ScrollViewer
        {
            Width = 200,
            Height = 120,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Template = CreateScrollerTemplate(canHorizontallyScroll: false, canVerticallyScroll: true),
            Content = new StackPanel
            {
                Width = 200,
                Children =
                {
                    inner,
                    new Border { Width = 200, Height = 400, Background = Brushes.Transparent },
                },
            },
        };

        SmoothScrollAssist.SetIsEnabled(outer, true);
        SmoothScrollAssist.SetFrameScheduler(outer, callback =>
        {
            outerFrame = callback;
            return true;
        });

        await RunInWindowAsync(outer, async window =>
        {
            window.MouseMove(new Point(40, 40), RawInputModifiers.None);
            window.MouseWheel(new Point(40, 40), new Vector(0, -1), RawInputModifiers.None);

            Assert.NotNull(innerFrame);
            Assert.Null(outerFrame);

            AdvanceExponentialFrames(ref innerFrame);
            Assert.Equal(80.0, inner.Offset.Y, 2);

            // Inner is now at bottom; next wheel event hands off smoothly to outer.
            window.MouseWheel(new Point(40, 40), new Vector(0, -1), RawInputModifiers.None);
            Assert.NotNull(outerFrame);

            // External offset assignment cancels active interpolation.
            outer.Offset = new Vector(0, 180);
            InvokeFrame(ref outerFrame, TimeSpan.FromMilliseconds(16));
            Assert.Null(outerFrame);
            Assert.Equal(180.0, outer.Offset.Y, 2);
            await Task.CompletedTask;
        });
    }

    private static ScrollViewer CreateScrollViewer(
        Func<Action<TimeSpan>, bool>? scheduler,
        double width = 200,
        double height = 100,
        double contentWidth = 200,
        double contentHeight = 500,
        bool canHorizontallyScroll = false,
        bool canVerticallyScroll = true,
        ScrollBarVisibility horizontalVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalVisibility = ScrollBarVisibility.Auto)
    {
        var scroller = new ScrollViewer
        {
            Width = width,
            Height = height,
            HorizontalScrollBarVisibility = horizontalVisibility,
            VerticalScrollBarVisibility = verticalVisibility,
            Template = CreateScrollerTemplate(canHorizontallyScroll, canVerticallyScroll),
            Content = new Border
            {
                Width = contentWidth,
                Height = contentHeight,
                Background = Brushes.Transparent,
            },
        };

        SmoothScrollAssist.SetIsEnabled(scroller, true);
        if (scheduler is not null)
        {
            SmoothScrollAssist.SetFrameScheduler(scroller, scheduler);
        }

        return scroller;
    }

    private static FuncControlTemplate<ScrollViewer> CreateScrollerTemplate(
        bool canHorizontallyScroll,
        bool canVerticallyScroll) =>
        new((_, scope) =>
        {
            var presenter = new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                Background = Brushes.Transparent,
                CanHorizontallyScroll = canHorizontallyScroll,
                CanVerticallyScroll = canVerticallyScroll,
            };
            presenter.Bind(ContentPresenter.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
            scope.Register("PART_ContentPresenter", presenter);
            return presenter;
        });

    private static async Task RunInWindowAsync(Control content, Func<Window, Task> body)
    {
        var window = new Window
        {
            Width = 220,
            Height = 140,
            Content = content,
        };

        try
        {
            window.Show();
            await FlushDispatcherAsync();
            await body(window);
        }
        finally
        {
            SmoothScrollAssist.SetFrameScheduler(content, null);
            window.Close();
        }
    }

    private static void AdvanceExponentialFrames(ref Action<TimeSpan>? pendingFrame)
    {
        var frameTime = TimeSpan.Zero;
        for (var i = 0; i < 160 && pendingFrame is not null; i++)
        {
            frameTime += TimeSpan.FromMilliseconds(16);
            InvokeFrame(ref pendingFrame, frameTime);
        }
    }

    private static void InvokeFrame(ref Action<TimeSpan>? pendingFrame, TimeSpan timestamp)
    {
        var callback = pendingFrame;
        pendingFrame = null;
        callback?.Invoke(timestamp);
    }

    private static async Task FlushDispatcherAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
    }
}
