using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Aviora.Controls;

/// <summary>
/// Attached-property helper that adds smooth, customizable mouse-wheel scrolling to any
/// <see cref="ScrollViewer"/> or composite <see cref="Control"/> containing one (such as
/// <see cref="ListBox"/>, <see cref="TreeView"/>, or <see cref="ItemsControl"/>).
/// Supports exponential damping and duration/easing curves, built-in presets, vertical and
/// horizontal axes, Shift-wheel horizontal scrolling, nested scroller handoff, and automatic
/// pass-through for high-precision trackpads.
/// </summary>
public static class SmoothScrollAssist
{
    private const double DefaultStepSize = 96.0;
    private const double DefaultInitialStepFactor = 0.22;
    private const double DefaultDecayRate = 18.0;
    private const double DefaultMinSpeed = 48.0;
    private const double DefaultSettleThreshold = 0.75;
    private const double DefaultTrackpadThreshold = 0.5;
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(220);
    private static readonly Easing DefaultEasing = new CubicEaseOut();

    /// <summary>Defines the <c>IsEnabled</c> attached property.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsEnabled",
            typeof(SmoothScrollAssist),
            defaultValue: false);

    /// <summary>Defines the <c>Preset</c> attached property.</summary>
    public static readonly AttachedProperty<SmoothScrollPreset> PresetProperty =
        AvaloniaProperty.RegisterAttached<Control, SmoothScrollPreset>(
            "Preset",
            typeof(SmoothScrollAssist),
            defaultValue: SmoothScrollPreset.Default);

    /// <summary>Defines the <c>CurveMode</c> attached property.</summary>
    public static readonly AttachedProperty<SmoothScrollCurveMode> CurveModeProperty =
        AvaloniaProperty.RegisterAttached<Control, SmoothScrollCurveMode>(
            "CurveMode",
            typeof(SmoothScrollAssist),
            defaultValue: SmoothScrollCurveMode.Exponential);

    /// <summary>Defines the <c>Orientation</c> attached property.</summary>
    public static readonly AttachedProperty<SmoothScrollOrientation> OrientationProperty =
        AvaloniaProperty.RegisterAttached<Control, SmoothScrollOrientation>(
            "Orientation",
            typeof(SmoothScrollAssist),
            defaultValue: SmoothScrollOrientation.Both);

    /// <summary>Defines the <c>StepSize</c> attached property, in DIPs per unit wheel notch.</summary>
    public static readonly AttachedProperty<double> StepSizeProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "StepSize",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultStepSize,
            validate: static v => double.IsFinite(v) && v > 0);

    /// <summary>Defines the <c>InitialStepFactor</c> attached property, in the range [0, 1].</summary>
    public static readonly AttachedProperty<double> InitialStepFactorProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "InitialStepFactor",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultInitialStepFactor,
            validate: static v => double.IsFinite(v) && v >= 0.0 && v <= 1.0);

    /// <summary>Defines the <c>DecayRate</c> attached property for exponential damping.</summary>
    public static readonly AttachedProperty<double> DecayRateProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "DecayRate",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultDecayRate,
            validate: static v => double.IsFinite(v) && v > 0);

    /// <summary>Defines the <c>MinSpeed</c> attached property, in DIPs per second.</summary>
    public static readonly AttachedProperty<double> MinSpeedProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "MinSpeed",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultMinSpeed,
            validate: static v => double.IsFinite(v) && v >= 0.0);

    /// <summary>Defines the <c>Duration</c> attached property for <see cref="SmoothScrollCurveMode.Easing"/> mode.</summary>
    public static readonly AttachedProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.RegisterAttached<Control, TimeSpan>(
            "Duration",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultDuration,
            validate: static v => v >= TimeSpan.Zero);

    /// <summary>Defines the <c>Easing</c> attached property for <see cref="SmoothScrollCurveMode.Easing"/> mode.</summary>
    public static readonly AttachedProperty<Easing?> EasingProperty =
        AvaloniaProperty.RegisterAttached<Control, Easing?>(
            "Easing",
            typeof(SmoothScrollAssist),
            defaultValue: null);

    /// <summary>Defines the <c>SettleThreshold</c> attached property, in DIPs.</summary>
    public static readonly AttachedProperty<double> SettleThresholdProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "SettleThreshold",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultSettleThreshold,
            validate: static v => double.IsFinite(v) && v > 0);

    /// <summary>Defines the <c>MaxQueueDistance</c> attached property, in DIPs.</summary>
    public static readonly AttachedProperty<double> MaxQueueDistanceProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "MaxQueueDistance",
            typeof(SmoothScrollAssist),
            defaultValue: double.PositiveInfinity,
            validate: static v => (double.IsFinite(v) && v > 0) || double.IsPositiveInfinity(v));

    /// <summary>Defines the <c>TrackpadThreshold</c> attached property for direct pass-through of fine wheel deltas.</summary>
    public static readonly AttachedProperty<double> TrackpadThresholdProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "TrackpadThreshold",
            typeof(SmoothScrollAssist),
            defaultValue: DefaultTrackpadThreshold,
            validate: static v => double.IsFinite(v) && v >= 0.0);

    /// <summary>Defines the <c>IsShiftWheelHorizontalEnabled</c> attached property.</summary>
    public static readonly AttachedProperty<bool> IsShiftWheelHorizontalEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsShiftWheelHorizontalEnabled",
            typeof(SmoothScrollAssist),
            defaultValue: true);

    /// <summary>Defines the <c>AutoHorizontalWhenVerticalDisabled</c> attached property.</summary>
    public static readonly AttachedProperty<bool> AutoHorizontalWhenVerticalDisabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "AutoHorizontalWhenVerticalDisabled",
            typeof(SmoothScrollAssist),
            defaultValue: false);

    /// <summary>Defines the <c>RespectNestedScrollViewers</c> attached property.</summary>
    public static readonly AttachedProperty<bool> RespectNestedScrollViewersProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "RespectNestedScrollViewers",
            typeof(SmoothScrollAssist),
            defaultValue: true);

    private static readonly AttachedProperty<Controller?> ControllerProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, Controller?>(
            "Controller",
            typeof(SmoothScrollAssist));

    private static readonly AttachedProperty<HostBinder?> HostBinderProperty =
        AvaloniaProperty.RegisterAttached<Control, HostBinder?>(
            "HostBinder",
            typeof(SmoothScrollAssist));

    private static readonly ConditionalWeakTable<Control, Func<Action<TimeSpan>, bool>> FrameSchedulers = new();

    static SmoothScrollAssist() => IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);

    /// <summary>Gets whether smooth scrolling is enabled on <paramref name="control"/>.</summary>
    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    /// <summary>Sets whether smooth scrolling is enabled on <paramref name="control"/>.</summary>
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    /// <summary>Gets the motion feel preset on <paramref name="control"/>.</summary>
    public static SmoothScrollPreset GetPreset(Control control) => control.GetValue(PresetProperty);

    /// <summary>Sets the motion feel preset on <paramref name="control"/>.</summary>
    public static void SetPreset(Control control, SmoothScrollPreset value) => control.SetValue(PresetProperty, value);

    /// <summary>Gets the interpolation curve mode on <paramref name="control"/>.</summary>
    public static SmoothScrollCurveMode GetCurveMode(Control control) => control.GetValue(CurveModeProperty);

    /// <summary>Sets the interpolation curve mode on <paramref name="control"/>.</summary>
    public static void SetCurveMode(Control control, SmoothScrollCurveMode value) => control.SetValue(CurveModeProperty, value);

    /// <summary>Gets which scroll axes are interpolated on <paramref name="control"/>.</summary>
    public static SmoothScrollOrientation GetOrientation(Control control) => control.GetValue(OrientationProperty);

    /// <summary>Sets which scroll axes are interpolated on <paramref name="control"/>.</summary>
    public static void SetOrientation(Control control, SmoothScrollOrientation value) => control.SetValue(OrientationProperty, value);

    /// <summary>Gets the scroll step size in DIPs per unit wheel notch on <paramref name="control"/>.</summary>
    public static double GetStepSize(Control control) => control.GetValue(StepSizeProperty);

    /// <summary>Sets the scroll step size in DIPs per unit wheel notch on <paramref name="control"/>.</summary>
    public static void SetStepSize(Control control, double value) => control.SetValue(StepSizeProperty, value);

    /// <summary>Gets the initial step fraction applied immediately on a wheel tick for <paramref name="control"/>.</summary>
    public static double GetInitialStepFactor(Control control) => control.GetValue(InitialStepFactorProperty);

    /// <summary>Sets the initial step fraction applied immediately on a wheel tick for <paramref name="control"/>.</summary>
    public static void SetInitialStepFactor(Control control, double value) => control.SetValue(InitialStepFactorProperty, value);

    /// <summary>Gets the exponential damping decay rate on <paramref name="control"/>.</summary>
    public static double GetDecayRate(Control control) => control.GetValue(DecayRateProperty);

    /// <summary>Sets the exponential damping decay rate on <paramref name="control"/>.</summary>
    public static void SetDecayRate(Control control, double value) => control.SetValue(DecayRateProperty, value);

    /// <summary>Gets the minimum tail settling speed in DIPs per second on <paramref name="control"/>.</summary>
    public static double GetMinSpeed(Control control) => control.GetValue(MinSpeedProperty);

    /// <summary>Sets the minimum tail settling speed in DIPs per second on <paramref name="control"/>.</summary>
    public static void SetMinSpeed(Control control, double value) => control.SetValue(MinSpeedProperty, value);

    /// <summary>Gets the interpolation duration used in <see cref="SmoothScrollCurveMode.Easing"/> mode on <paramref name="control"/>.</summary>
    public static TimeSpan GetDuration(Control control) => control.GetValue(DurationProperty);

    /// <summary>Sets the interpolation duration used in <see cref="SmoothScrollCurveMode.Easing"/> mode on <paramref name="control"/>.</summary>
    public static void SetDuration(Control control, TimeSpan value) => control.SetValue(DurationProperty, value);

    /// <summary>Gets the easing function used in <see cref="SmoothScrollCurveMode.Easing"/> mode on <paramref name="control"/>.</summary>
    public static Easing? GetEasing(Control control) => control.GetValue(EasingProperty);

    /// <summary>Sets the easing function used in <see cref="SmoothScrollCurveMode.Easing"/> mode on <paramref name="control"/>.</summary>
    public static void SetEasing(Control control, Easing? value) => control.SetValue(EasingProperty, value);

    /// <summary>Gets the snap-to-target settle threshold in DIPs on <paramref name="control"/>.</summary>
    public static double GetSettleThreshold(Control control) => control.GetValue(SettleThresholdProperty);

    /// <summary>Sets the snap-to-target settle threshold in DIPs on <paramref name="control"/>.</summary>
    public static void SetSettleThreshold(Control control, double value) => control.SetValue(SettleThresholdProperty, value);

    /// <summary>Gets the maximum queued lead distance in DIPs ahead of the current offset on <paramref name="control"/>.</summary>
    public static double GetMaxQueueDistance(Control control) => control.GetValue(MaxQueueDistanceProperty);

    /// <summary>Sets the maximum queued lead distance in DIPs ahead of the current offset on <paramref name="control"/>.</summary>
    public static void SetMaxQueueDistance(Control control, double value) => control.SetValue(MaxQueueDistanceProperty, value);

    /// <summary>Gets the delta magnitude below which fine trackpad events pass through directly on <paramref name="control"/>.</summary>
    public static double GetTrackpadThreshold(Control control) => control.GetValue(TrackpadThresholdProperty);

    /// <summary>Sets the delta magnitude below which fine trackpad events pass through directly on <paramref name="control"/>.</summary>
    public static void SetTrackpadThreshold(Control control, double value) => control.SetValue(TrackpadThresholdProperty, value);

    /// <summary>Gets whether holding Shift maps vertical wheel ticks to horizontal scrolling on <paramref name="control"/>.</summary>
    public static bool GetIsShiftWheelHorizontalEnabled(Control control) => control.GetValue(IsShiftWheelHorizontalEnabledProperty);

    /// <summary>Sets whether holding Shift maps vertical wheel ticks to horizontal scrolling on <paramref name="control"/>.</summary>
    public static void SetIsShiftWheelHorizontalEnabled(Control control, bool value) => control.SetValue(IsShiftWheelHorizontalEnabledProperty, value);

    /// <summary>Gets whether vertical wheel ticks automatically scroll horizontally when vertical scrolling is unavailable on <paramref name="control"/>.</summary>
    public static bool GetAutoHorizontalWhenVerticalDisabled(Control control) => control.GetValue(AutoHorizontalWhenVerticalDisabledProperty);

    /// <summary>Sets whether vertical wheel ticks automatically scroll horizontally when vertical scrolling is unavailable on <paramref name="control"/>.</summary>
    public static void SetAutoHorizontalWhenVerticalDisabled(Control control, bool value) => control.SetValue(AutoHorizontalWhenVerticalDisabledProperty, value);

    /// <summary>Gets whether nested scrollable <see cref="ScrollViewer"/> descendants take priority before the outer host scrolls.</summary>
    public static bool GetRespectNestedScrollViewers(Control control) => control.GetValue(RespectNestedScrollViewersProperty);

    /// <summary>Sets whether nested scrollable <see cref="ScrollViewer"/> descendants take priority before the outer host scrolls.</summary>
    public static void SetRespectNestedScrollViewers(Control control, bool value) => control.SetValue(RespectNestedScrollViewersProperty, value);

    /// <summary>Overrides frame scheduling for <paramref name="control"/> in tests.</summary>
    internal static void SetFrameScheduler(Control control, Func<Action<TimeSpan>, bool>? scheduler)
    {
        if (scheduler is null)
        {
            FrameSchedulers.Remove(control);
        }
        else
        {
            FrameSchedulers.AddOrUpdate(control, scheduler);
        }
    }

    internal static Func<Action<TimeSpan>, bool>? GetFrameScheduler(Control control) =>
        FrameSchedulers.TryGetValue(control, out var scheduler) ? scheduler : null;

    private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs change)
    {
        var enabled = change.GetNewValue<bool>();

        if (control is ScrollViewer scroller)
        {
            UpdateDirectController(scroller, enabled);
            return;
        }

        if (enabled)
        {
            if (control.GetValue(HostBinderProperty) is null)
            {
                var binder = new HostBinder(control);
                control.SetValue(HostBinderProperty, binder);
                binder.Attach();
            }
        }
        else if (control.GetValue(HostBinderProperty) is { } binder)
        {
            binder.Detach();
            control.SetValue(HostBinderProperty, null);
        }
    }

    private static void UpdateDirectController(ScrollViewer scroller, bool enabled)
    {
        var controller = scroller.GetValue(ControllerProperty);
        if (enabled)
        {
            if (controller is null)
            {
                controller = new Controller(scroller);
                scroller.SetValue(ControllerProperty, controller);
            }

            controller.SetDirectEnabled(true);
        }
        else if (controller is not null)
        {
            controller.SetDirectEnabled(false);
            CleanupControllerIfUnused(scroller, controller);
        }
    }

    private static Controller GetOrCreateController(ScrollViewer scroller)
    {
        var controller = scroller.GetValue(ControllerProperty);
        if (controller is null)
        {
            controller = new Controller(scroller);
            scroller.SetValue(ControllerProperty, controller);
        }

        return controller;
    }

    private static void CleanupControllerIfUnused(ScrollViewer scroller, Controller controller)
    {
        if (!controller.IsActive)
        {
            controller.Detach();
            scroller.SetValue(ControllerProperty, null);
        }
    }

    private static PresetDefaults GetPresetDefaults(SmoothScrollPreset preset) => preset switch
    {
        SmoothScrollPreset.Snappy => new PresetDefaults(96.0, 0.32, 26.0, 72.0, 0.75, TimeSpan.FromMilliseconds(150)),
        SmoothScrollPreset.Silky => new PresetDefaults(112.0, 0.14, 11.5, 32.0, 0.5, TimeSpan.FromMilliseconds(340)),
        _ => new PresetDefaults(DefaultStepSize, DefaultInitialStepFactor, DefaultDecayRate, DefaultMinSpeed, DefaultSettleThreshold, DefaultDuration),
    };

    private static ResolvedOptions ResolveOptions(ScrollViewer scroller, Control? delegatingHost)
    {
        var preset = ResolveValue(scroller, delegatingHost, PresetProperty, SmoothScrollPreset.Default);
        var defaults = GetPresetDefaults(preset);

        return new ResolvedOptions(
            CurveMode: ResolveValue(scroller, delegatingHost, CurveModeProperty, SmoothScrollCurveMode.Exponential),
            Orientation: ResolveValue(scroller, delegatingHost, OrientationProperty, SmoothScrollOrientation.Both),
            StepSize: ResolveValue(scroller, delegatingHost, StepSizeProperty, defaults.StepSize),
            InitialStepFactor: ResolveValue(scroller, delegatingHost, InitialStepFactorProperty, defaults.InitialStepFactor),
            DecayRate: ResolveValue(scroller, delegatingHost, DecayRateProperty, defaults.DecayRate),
            MinSpeed: ResolveValue(scroller, delegatingHost, MinSpeedProperty, defaults.MinSpeed),
            Duration: ResolveValue(scroller, delegatingHost, DurationProperty, defaults.Duration),
            Easing: ResolveValue<Easing?>(scroller, delegatingHost, EasingProperty, null) ?? DefaultEasing,
            SettleThreshold: ResolveValue(scroller, delegatingHost, SettleThresholdProperty, defaults.SettleThreshold),
            MaxQueueDistance: ResolveValue(scroller, delegatingHost, MaxQueueDistanceProperty, double.PositiveInfinity),
            TrackpadThreshold: ResolveValue(scroller, delegatingHost, TrackpadThresholdProperty, DefaultTrackpadThreshold),
            IsShiftWheelHorizontalEnabled: ResolveValue(scroller, delegatingHost, IsShiftWheelHorizontalEnabledProperty, true),
            AutoHorizontalWhenVerticalDisabled: ResolveValue(scroller, delegatingHost, AutoHorizontalWhenVerticalDisabledProperty, false),
            RespectNestedScrollViewers: ResolveValue(scroller, delegatingHost, RespectNestedScrollViewersProperty, true));
    }

    private static T ResolveValue<T>(
        ScrollViewer scroller,
        Control? delegatingHost,
        AttachedProperty<T> property,
        T fallback)
    {
        if (delegatingHost is not null && delegatingHost.IsSet(property))
        {
            return delegatingHost.GetValue(property);
        }

        if (scroller.IsSet(property))
        {
            return scroller.GetValue(property);
        }

        return fallback;
    }

    private readonly record struct PresetDefaults(
        double StepSize,
        double InitialStepFactor,
        double DecayRate,
        double MinSpeed,
        double SettleThreshold,
        TimeSpan Duration);

    private readonly record struct ResolvedOptions(
        SmoothScrollCurveMode CurveMode,
        SmoothScrollOrientation Orientation,
        double StepSize,
        double InitialStepFactor,
        double DecayRate,
        double MinSpeed,
        TimeSpan Duration,
        Easing Easing,
        double SettleThreshold,
        double MaxQueueDistance,
        double TrackpadThreshold,
        bool IsShiftWheelHorizontalEnabled,
        bool AutoHorizontalWhenVerticalDisabled,
        bool RespectNestedScrollViewers);

    private sealed class HostBinder(Control host)
    {
        private bool _attached;
        private ScrollViewer? _boundScroller;

        public void Attach()
        {
            _attached = true;
            host.AttachedToVisualTree += OnAttachedToVisualTree;
            host.DetachedFromVisualTree += OnDetachedFromVisualTree;
            if (host is TemplatedControl templated)
            {
                templated.TemplateApplied += OnTemplateApplied;
            }

            Rebind();
            if (host.IsAttachedToVisualTree())
            {
                PostDeferredRebind();
            }
        }

        public void Detach()
        {
            _attached = false;
            host.AttachedToVisualTree -= OnAttachedToVisualTree;
            host.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            if (host is TemplatedControl templated)
            {
                templated.TemplateApplied -= OnTemplateApplied;
            }

            UnbindCurrent();
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Rebind();
            PostDeferredRebind();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => UnbindCurrent();

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) => Rebind();

        private void PostDeferredRebind() =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_attached && host.IsAttachedToVisualTree())
                {
                    Rebind();
                }
            }, DispatcherPriority.Loaded);

        private void Rebind()
        {
            if (!_attached || !GetIsEnabled(host))
            {
                UnbindCurrent();
                return;
            }

            var target = host.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (ReferenceEquals(target, _boundScroller))
            {
                return;
            }

            UnbindCurrent();
            if (target is null)
            {
                return;
            }

            _boundScroller = target;
            var controller = GetOrCreateController(target);
            controller.SetDelegatingHost(host);
        }

        private void UnbindCurrent()
        {
            if (_boundScroller is not { } previous)
            {
                return;
            }

            _boundScroller = null;
            if (previous.GetValue(ControllerProperty) is { } controller)
            {
                controller.SetDelegatingHost(null);
                CleanupControllerIfUnused(previous, controller);
            }
        }
    }

    private sealed class Controller(ScrollViewer scroller)
    {
        private bool _attached;
        private bool _animating;
        private bool _frameRequested;
        private bool _applying;
        private double _targetX;
        private double _targetY;
        private double _expectedX;
        private double _expectedY;
        private TimeSpan? _lastFrameTime;
        private long? _wheelTimestamp;
        private SmoothScrollCurveMode _activeCurveMode;
        private double _easingStartX;
        private double _easingStartY;
        private TimeSpan _easingElapsed;

        public bool DirectEnabled { get; private set; }

        public Control? DelegatingHost { get; private set; }

        public bool IsActive => DirectEnabled || DelegatingHost is not null;

        public void SetDirectEnabled(bool enabled)
        {
            DirectEnabled = enabled;
            SyncAttachment();
        }

        public void SetDelegatingHost(Control? host)
        {
            DelegatingHost = host;
            SyncAttachment();
        }

        public void Detach()
        {
            if (!_attached)
            {
                return;
            }

            _attached = false;
            scroller.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
            scroller.RemoveHandler(Thumb.DragStartedEvent, OnThumbDragStarted);
            scroller.PropertyChanged -= OnPropertyChanged;
            scroller.ScrollChanged -= OnScrollChanged;
            scroller.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            Cancel();
        }

        private void SyncAttachment()
        {
            if (IsActive)
            {
                Attach();
            }
            else
            {
                Detach();
            }
        }

        private void Attach()
        {
            if (_attached)
            {
                return;
            }

            _attached = true;
            scroller.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
            scroller.AddHandler(Thumb.DragStartedEvent, OnThumbDragStarted, RoutingStrategies.Bubble);
            scroller.PropertyChanged += OnPropertyChanged;
            scroller.ScrollChanged += OnScrollChanged;
            scroller.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => Cancel();

        private void OnThumbDragStarted(object? sender, VectorEventArgs e) => SyncToLiveOffset();

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_applying || !_animating || e.Property != ScrollViewer.OffsetProperty)
            {
                return;
            }

            if (Math.Abs(scroller.Offset.X - _expectedX) > 1e-4 ||
                Math.Abs(scroller.Offset.Y - _expectedY) > 1e-4)
            {
                SyncToLiveOffset();
            }
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (_applying || !_animating)
            {
                return;
            }

            if (Math.Abs(scroller.Offset.X - _expectedX) > 1.0 ||
                Math.Abs(scroller.Offset.Y - _expectedY) > 1.0 ||
                (scroller.Offset.X == 0 && _expectedX > 0) ||
                (scroller.Offset.Y == 0 && _expectedY > 0))
            {
                SyncToLiveOffset();
            }
        }

        private void SyncToLiveOffset()
        {
            Cancel();
            _expectedX = scroller.Offset.X;
            _expectedY = scroller.Offset.Y;
            _targetX = _expectedX;
            _targetY = _expectedY;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            var options = ResolveOptions(scroller, DelegatingHost);
            var isShift = e.KeyModifiers == KeyModifiers.Shift;
            if (e.KeyModifiers != KeyModifiers.None && (!isShift || !options.IsShiftWheelHorizontalEnabled))
            {
                return;
            }

            var rawDx = e.Delta.X;
            var rawDy = e.Delta.Y;
            if (isShift && Math.Abs(rawDx) <= double.Epsilon && Math.Abs(rawDy) > double.Epsilon)
            {
                rawDx = rawDy;
                rawDy = 0.0;
            }

            if (Math.Abs(rawDx) <= double.Epsilon && Math.Abs(rawDy) <= double.Epsilon)
            {
                return;
            }

            if (options.RespectNestedScrollViewers && HasScrollableInnerAncestor(e.Source as Visual, rawDx, rawDy))
            {
                return;
            }

            var (dx, dy) = MapOrientation(rawDx, rawDy, options);
            if (Math.Abs(dx) <= double.Epsilon && Math.Abs(dy) <= double.Epsilon)
            {
                return;
            }

            var maxX = MaxOffsetX(scroller);
            var maxY = MaxOffsetY(scroller);
            var canX = CanScrollAxis(scroller.HorizontalScrollBarVisibility, maxX) && Math.Abs(dx) > double.Epsilon;
            var canY = CanScrollAxis(scroller.VerticalScrollBarVisibility, maxY) && Math.Abs(dy) > double.Epsilon;
            if (!canX && !canY)
            {
                return;
            }

            var currentX = scroller.Offset.X;
            var currentY = scroller.Offset.Y;
            var stepX = canX ? -dx * options.StepSize : 0.0;
            var stepY = canY ? -dy * options.StepSize : 0.0;

            var dominantDelta = Math.Max(canX ? Math.Abs(dx) : 0.0, canY ? Math.Abs(dy) : 0.0);
            if (options.TrackpadThreshold > 0 && dominantDelta < options.TrackpadThreshold)
            {
                ApplyDirectPassThrough(currentX, currentY, stepX, stepY, maxX, maxY, e);
                return;
            }

            var newTargetX = ComputeAxisTarget(canX, currentX, stepX, maxX, _targetX, _animating, options.MaxQueueDistance);
            var newTargetY = ComputeAxisTarget(canY, currentY, stepY, maxY, _targetY, _animating, options.MaxQueueDistance);

            var remX = newTargetX - currentX;
            var remY = newTargetY - currentY;
            if (Math.Abs(remX) <= 0.01 && Math.Abs(remY) <= 0.01)
            {
                _targetX = currentX;
                _targetY = currentY;
                return;
            }

            _targetX = newTargetX;
            _targetY = newTargetY;
            e.Handled = true;

            var remainingDistance = Math.Sqrt((remX * remX) + (remY * remY));
            if (remainingDistance <= options.SettleThreshold ||
                (options.CurveMode == SmoothScrollCurveMode.Easing && options.Duration <= TimeSpan.Zero))
            {
                FinishAtTarget();
                return;
            }

            _animating = true;
            _activeCurveMode = options.CurveMode;
            if (_lastFrameTime is null)
            {
                _wheelTimestamp = Stopwatch.GetTimestamp();
            }

            if (options.CurveMode == SmoothScrollCurveMode.Easing)
            {
                _easingStartX = currentX;
                _easingStartY = currentY;
                _easingElapsed = TimeSpan.Zero;
                RequestFrame();
                return;
            }

            var nextX = currentX + (remX * options.InitialStepFactor);
            var nextY = currentY + (remY * options.InitialStepFactor);
            ApplyOffset(nextX, nextY);
            RequestFrame();
        }

        private void ApplyDirectPassThrough(
            double currentX,
            double currentY,
            double stepX,
            double stepY,
            double maxX,
            double maxY,
            PointerWheelEventArgs e)
        {
            Cancel();
            var directX = Math.Clamp(currentX + stepX, 0, maxX);
            var directY = Math.Clamp(currentY + stepY, 0, maxY);
            if (Math.Abs(directX - currentX) > 1e-4 || Math.Abs(directY - currentY) > 1e-4)
            {
                ApplyOffset(directX, directY);
                e.Handled = true;
            }
        }

        private (double Dx, double Dy) MapOrientation(double dx, double dy, ResolvedOptions options)
        {
            if (options.AutoHorizontalWhenVerticalDisabled &&
                Math.Abs(dx) <= double.Epsilon &&
                Math.Abs(dy) > double.Epsilon &&
                !CanScrollAxis(scroller.VerticalScrollBarVisibility, MaxOffsetY(scroller)) &&
                CanScrollAxis(scroller.HorizontalScrollBarVisibility, MaxOffsetX(scroller)))
            {
                dx = dy;
                dy = 0.0;
            }

            return options.Orientation switch
            {
                SmoothScrollOrientation.Vertical => (0.0, dy),
                SmoothScrollOrientation.Horizontal => (dx, 0.0),
                _ => (dx, dy),
            };
        }

        private bool HasScrollableInnerAncestor(Visual? source, double dx, double dy)
        {
            var candidate = source?.FindAncestorOfType<ScrollViewer>(includeSelf: true);
            while (candidate is not null && !ReferenceEquals(candidate, scroller))
            {
                if (scroller.IsVisualAncestorOf(candidate) && CanScrollInDelta(candidate, dx, dy))
                {
                    return true;
                }

                candidate = (candidate.GetVisualParent() as Visual)?.FindAncestorOfType<ScrollViewer>(includeSelf: true);
            }

            return false;
        }

        private Func<Action<TimeSpan>, bool>? GetActiveScheduler() =>
            GetFrameScheduler(scroller) ?? (DelegatingHost is not null ? GetFrameScheduler(DelegatingHost) : null);

        private void RequestFrame()
        {
            if (_frameRequested)
            {
                return;
            }

            var scheduler = GetActiveScheduler() ?? DefaultSchedule;
            _frameRequested = true;
            if (!scheduler(OnFrame))
            {
                _frameRequested = false;
                FinishAtTarget();
            }
        }

        private bool DefaultSchedule(Action<TimeSpan> callback)
        {
            if (TopLevel.GetTopLevel(scroller) is not { } topLevel)
            {
                return false;
            }

            topLevel.RequestAnimationFrame(callback);
            return true;
        }

        private void OnFrame(TimeSpan now)
        {
            _frameRequested = false;
            if (!_animating)
            {
                _lastFrameTime = null;
                return;
            }

            var options = ResolveOptions(scroller, DelegatingHost);
            _targetX = Math.Clamp(_targetX, 0, MaxOffsetX(scroller));
            _targetY = Math.Clamp(_targetY, 0, MaxOffsetY(scroller));

            var dt = ComputeFrameDeltaSeconds(now, _activeCurveMode);
            _lastFrameTime = now;
            _wheelTimestamp = null;

            if (_activeCurveMode == SmoothScrollCurveMode.Easing)
            {
                AdvanceEasingFrame(dt, options);
                return;
            }

            AdvanceExponentialFrame(dt, options);
        }

        private double ComputeFrameDeltaSeconds(TimeSpan now, SmoothScrollCurveMode mode)
        {
            if (_lastFrameTime is { } last)
            {
                var elapsed = (now - last).TotalSeconds;
                return mode == SmoothScrollCurveMode.Easing
                    ? Math.Max(0.0, elapsed)
                    : Math.Clamp(elapsed, 0.001, 0.05);
            }

            if (GetActiveScheduler() is not null && now == TimeSpan.Zero)
            {
                return 0.0;
            }

            if (GetActiveScheduler() is null && _wheelTimestamp is { } wheelStart)
            {
                return Math.Clamp(Stopwatch.GetElapsedTime(wheelStart).TotalSeconds, 0.001, 1.0 / 60.0);
            }

            return 1.0 / 60.0;
        }

        private void AdvanceEasingFrame(double dt, ResolvedOptions options)
        {
            var duration = options.Duration;
            if (duration <= TimeSpan.Zero)
            {
                FinishAtTarget();
                return;
            }

            _easingElapsed += TimeSpan.FromSeconds(dt);
            var progress = Math.Clamp(_easingElapsed.TotalMilliseconds / duration.TotalMilliseconds, 0.0, 1.0);
            var eased = options.Easing.Ease(progress);
            var nextX = _easingStartX + ((_targetX - _easingStartX) * eased);
            var nextY = _easingStartY + ((_targetY - _easingStartY) * eased);
            ApplyOffset(nextX, nextY);

            if (progress >= 1.0)
            {
                FinishAtTarget();
                return;
            }

            RequestFrame();
        }

        private void AdvanceExponentialFrame(double dt, ResolvedOptions options)
        {
            var currentX = scroller.Offset.X;
            var currentY = scroller.Offset.Y;
            var remX = _targetX - currentX;
            var remY = _targetY - currentY;
            var remaining = Math.Sqrt((remX * remX) + (remY * remY));

            if (remaining <= options.SettleThreshold)
            {
                FinishAtTarget();
                return;
            }

            var safeDt = Math.Max(dt, 0.001);
            var factor = 1.0 - Math.Exp(-options.DecayRate * safeDt);
            var move = Math.Min(remaining, Math.Max(remaining * factor, options.MinSpeed * safeDt));
            var ratio = move / remaining;
            ApplyOffset(currentX + (remX * ratio), currentY + (remY * ratio));

            var afterX = _targetX - scroller.Offset.X;
            var afterY = _targetY - scroller.Offset.Y;
            if (Math.Sqrt((afterX * afterX) + (afterY * afterY)) <= 0.01)
            {
                _animating = false;
                _lastFrameTime = null;
                _wheelTimestamp = null;
                return;
            }

            RequestFrame();
        }

        private void FinishAtTarget()
        {
            _animating = false;
            _lastFrameTime = null;
            _wheelTimestamp = null;
            ApplyOffset(_targetX, _targetY);
        }

        private void ApplyOffset(double x, double y)
        {
            var clampedX = Math.Clamp(x, 0, MaxOffsetX(scroller));
            var clampedY = Math.Clamp(y, 0, MaxOffsetY(scroller));
            _applying = true;
            try
            {
                scroller.Offset = new Vector(clampedX, clampedY);
                _expectedX = scroller.Offset.X;
                _expectedY = scroller.Offset.Y;
            }
            finally
            {
                _applying = false;
            }
        }

        private void Cancel()
        {
            _animating = false;
            _lastFrameTime = null;
            _wheelTimestamp = null;
            _easingElapsed = TimeSpan.Zero;
        }

        private static double ComputeAxisTarget(
            bool axisActive,
            double current,
            double step,
            double maxOffset,
            double currentTarget,
            bool animating,
            double maxQueueDistance)
        {
            if (!axisActive)
            {
                return animating ? Math.Clamp(currentTarget, 0, maxOffset) : current;
            }

            var baseTarget = animating ? currentTarget : current;
            if (animating && (currentTarget - current) * step < 0)
            {
                baseTarget = current;
            }

            var rawTarget = baseTarget + step;
            if (double.IsFinite(maxQueueDistance))
            {
                rawTarget = Math.Clamp(rawTarget, current - maxQueueDistance, current + maxQueueDistance);
            }

            return Math.Clamp(rawTarget, 0, maxOffset);
        }

        private static bool CanScrollInDelta(ScrollViewer candidate, double dx, double dy)
        {
            if (Math.Abs(dy) > double.Epsilon &&
                CanScrollAxis(candidate.VerticalScrollBarVisibility, MaxOffsetY(candidate)))
            {
                var maxY = MaxOffsetY(candidate);
                if (dy > 0 ? candidate.Offset.Y > 0.5 : candidate.Offset.Y < maxY - 0.5)
                {
                    return true;
                }
            }

            if (Math.Abs(dx) > double.Epsilon &&
                CanScrollAxis(candidate.HorizontalScrollBarVisibility, MaxOffsetX(candidate)))
            {
                var maxX = MaxOffsetX(candidate);
                if (dx > 0 ? candidate.Offset.X > 0.5 : candidate.Offset.X < maxX - 0.5)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanScrollAxis(ScrollBarVisibility visibility, double maxOffset) =>
            visibility != ScrollBarVisibility.Disabled && maxOffset > 0.5;

        private static double MaxOffsetX(ScrollViewer target) =>
            Math.Max(0, target.Extent.Width - target.Viewport.Width);

        private static double MaxOffsetY(ScrollViewer target) =>
            Math.Max(0, target.Extent.Height - target.Viewport.Height);
    }
}
