using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aviora.Presentation.Toasts;

namespace Aviora.Controls;

/// <summary>Hosts page content and presents concurrent global toast notifications above it.</summary>
[TemplatePart(TopLeftPartName, typeof(StackPanel))]
[TemplatePart(TopCenterPartName, typeof(StackPanel))]
[TemplatePart(TopRightPartName, typeof(StackPanel))]
[TemplatePart(BottomLeftPartName, typeof(StackPanel))]
[TemplatePart(BottomCenterPartName, typeof(StackPanel))]
[TemplatePart(BottomRightPartName, typeof(StackPanel))]
public class ToastHost : ContentControl, IToastHost
{
    /// <summary>The default identifier used to match service requests to a host.</summary>
    public const string DefaultHostId = ToastHosts.DefaultId;
    internal const string TopLeftPartName = "PART_TopLeftToasts";
    internal const string TopCenterPartName = "PART_TopCenterToasts";
    internal const string TopRightPartName = "PART_TopRightToasts";
    internal const string BottomLeftPartName = "PART_BottomLeftToasts";
    internal const string BottomCenterPartName = "PART_BottomCenterToasts";
    internal const string BottomRightPartName = "PART_BottomRightToasts";

    /// <summary>Defines the <see cref="Service"/> property.</summary>
    public static readonly StyledProperty<IToastHostService?> ServiceProperty =
        AvaloniaProperty.Register<ToastHost, IToastHostService?>(nameof(Service));

    /// <summary>Defines the <see cref="HostId"/> property.</summary>
    public static readonly StyledProperty<string> HostIdProperty =
        AvaloniaProperty.Register<ToastHost, string>(
            nameof(HostId),
            DefaultHostId,
            validate: value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Defines the <see cref="Placement"/> property.</summary>
    public static readonly StyledProperty<ToastPlacement> PlacementProperty =
        AvaloniaProperty.Register<ToastHost, ToastPlacement>(nameof(Placement), ToastPlacement.TopRight);

    /// <summary>Defines the <see cref="DefaultDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> DefaultDurationProperty =
        AvaloniaProperty.Register<ToastHost, TimeSpan>(
            nameof(DefaultDuration),
            TimeSpan.FromSeconds(4),
            validate: IsValidDuration);

    /// <summary>Defines the <see cref="MaxVisible"/> property.</summary>
    public static readonly StyledProperty<int> MaxVisibleProperty =
        AvaloniaProperty.Register<ToastHost, int>(nameof(MaxVisible), 0, validate: value => value >= 0);

    /// <summary>Defines the <see cref="IsDismissible"/> property.</summary>
    public static readonly StyledProperty<bool> IsDismissibleProperty =
        AvaloniaProperty.Register<ToastHost, bool>(nameof(IsDismissible), true);

    /// <summary>Defines the <see cref="IsClickDismissEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsClickDismissEnabledProperty =
        AvaloniaProperty.Register<ToastHost, bool>(nameof(IsClickDismissEnabled), true);

    /// <summary>Defines the <see cref="PauseOnPointerOver"/> property.</summary>
    public static readonly StyledProperty<bool> PauseOnPointerOverProperty =
        AvaloniaProperty.Register<ToastHost, bool>(nameof(PauseOnPointerOver), true);

    /// <summary>Defines the <see cref="IsAnimationEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<ToastHost, bool>(nameof(IsAnimationEnabled), true);

    /// <summary>Defines the <see cref="EntryAnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> EntryAnimationDurationProperty =
        AvaloniaProperty.Register<ToastHost, TimeSpan>(
            nameof(EntryAnimationDuration),
            TimeSpan.FromMilliseconds(220),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Provides compatibility for the former <c>AnimationDuration</c> styled property.</summary>
    [Obsolete("Use EntryAnimationDurationProperty instead.")]
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty = EntryAnimationDurationProperty;

    /// <summary>Defines the <see cref="ExitAnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> ExitAnimationDurationProperty =
        AvaloniaProperty.Register<ToastHost, TimeSpan>(
            nameof(ExitAnimationDuration),
            TimeSpan.FromMilliseconds(150),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="ReflowAnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> ReflowAnimationDurationProperty =
        AvaloniaProperty.Register<ToastHost, TimeSpan>(
            nameof(ReflowAnimationDuration),
            TimeSpan.FromMilliseconds(180),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="EntryAnimationEasing"/> property.</summary>
    public static readonly StyledProperty<Easing> EntryAnimationEasingProperty =
        AvaloniaProperty.Register<ToastHost, Easing>(nameof(EntryAnimationEasing), new SineEaseOut());

    /// <summary>Provides compatibility for the former <c>EntryEasing</c> styled property.</summary>
    [Obsolete("Use EntryAnimationEasingProperty instead.")]
    public static readonly StyledProperty<Easing> EntryEasingProperty = EntryAnimationEasingProperty;

    /// <summary>Defines the <see cref="ExitAnimationEasing"/> property.</summary>
    public static readonly StyledProperty<Easing> ExitAnimationEasingProperty =
        AvaloniaProperty.Register<ToastHost, Easing>(nameof(ExitAnimationEasing), new SineEaseIn());

    /// <summary>Provides compatibility for the former <c>ExitEasing</c> styled property.</summary>
    [Obsolete("Use ExitAnimationEasingProperty instead.")]
    public static readonly StyledProperty<Easing> ExitEasingProperty = ExitAnimationEasingProperty;

    /// <summary>Defines the <see cref="SlideDistance"/> property.</summary>
    public static readonly StyledProperty<double> SlideDistanceProperty =
        AvaloniaProperty.Register<ToastHost, double>(nameof(SlideDistance), 12, validate: value => value >= 0);

    /// <summary>Defines the <see cref="ToastSpacing"/> property.</summary>
    public static readonly StyledProperty<double> ToastSpacingProperty =
        AvaloniaProperty.Register<ToastHost, double>(nameof(ToastSpacing), 8, validate: value => value >= 0);

    /// <summary>Defines the <see cref="ToastMargin"/> property.</summary>
    public static readonly StyledProperty<Thickness> ToastMarginProperty =
        AvaloniaProperty.Register<ToastHost, Thickness>(nameof(ToastMargin), new Thickness(16));

    /// <summary>Defines the <see cref="ToastTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ToastTemplateProperty =
        AvaloniaProperty.Register<ToastHost, IDataTemplate?>(nameof(ToastTemplate));

    /// <summary>Defines the <see cref="ToastTheme"/> property.</summary>
    public static readonly StyledProperty<ControlTheme?> ToastThemeProperty =
        AvaloniaProperty.Register<ToastHost, ControlTheme?>(nameof(ToastTheme));

    /// <summary>Defines the read-only <see cref="ActiveCount"/> property.</summary>
    public static readonly DirectProperty<ToastHost, int> ActiveCountProperty =
        AvaloniaProperty.RegisterDirect<ToastHost, int>(nameof(ActiveCount), host => host.ActiveCount);

    private readonly List<ToastEntry> _activeEntries = [];
    private readonly List<ToastPresentation> _waiting = [];
    private StackPanel? _topLeft;
    private StackPanel? _topCenter;
    private StackPanel? _topRight;
    private StackPanel? _bottomLeft;
    private StackPanel? _bottomCenter;
    private StackPanel? _bottomRight;
    private IToastHostService? _attachedService;
    private string? _attachedHostId;
    private int _activeCount;

    static ToastHost()
    {
        ServiceProperty.Changed.AddClassHandler<ToastHost>((host, _) => host.UpdateServiceRegistration());
        HostIdProperty.Changed.AddClassHandler<ToastHost>((host, _) => host.UpdateServiceRegistration());
        MaxVisibleProperty.Changed.AddClassHandler<ToastHost>((host, _) => host.PromoteWaiting());
        ToastSpacingProperty.Changed.AddClassHandler<ToastHost>((host, _) => host.ApplyPanelSpacing());
        ToastTemplateProperty.Changed.AddClassHandler<ToastHost>((host, _) => host.UpdateActiveVisuals());
        ToastThemeProperty.Changed.AddClassHandler<ToastHost>((host, _) => host.UpdateActiveVisuals());
    }

    /// <summary>Gets or sets the service whose requests this host presents.</summary>
    public IToastHostService? Service { get => GetValue(ServiceProperty); set => SetValue(ServiceProperty, value); }

    /// <summary>Gets or sets the identifier used to route toast requests.</summary>
    public string HostId { get => GetValue(HostIdProperty); set => SetValue(HostIdProperty, value); }

    /// <summary>Gets or sets the default placement for requests without an override.</summary>
    public ToastPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Gets or sets the default display duration. Use <see cref="Timeout.InfiniteTimeSpan"/> for persistent toasts.</summary>
    public TimeSpan DefaultDuration { get => GetValue(DefaultDurationProperty); set => SetValue(DefaultDurationProperty, value); }

    /// <summary>Gets or sets the maximum number of visible notifications. Use zero for no limit.</summary>
    public int MaxVisible { get => GetValue(MaxVisibleProperty); set => SetValue(MaxVisibleProperty, value); }

    /// <summary>Gets or sets whether notifications are user-dismissible by default.</summary>
    public bool IsDismissible { get => GetValue(IsDismissibleProperty); set => SetValue(IsDismissibleProperty, value); }

    /// <summary>Gets or sets whether clicking non-interactive notification content dismisses it by default.</summary>
    public bool IsClickDismissEnabled
    {
        get => GetValue(IsClickDismissEnabledProperty);
        set => SetValue(IsClickDismissEnabledProperty, value);
    }

    /// <summary>Gets or sets whether the timeout pauses while the pointer is over a toast.</summary>
    public bool PauseOnPointerOver
    {
        get => GetValue(PauseOnPointerOverProperty);
        set => SetValue(PauseOnPointerOverProperty, value);
    }

    /// <summary>Gets or sets whether entry and exit transitions are enabled.</summary>
    public bool IsAnimationEnabled
    {
        get => GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>Gets or sets the entry transition duration.</summary>
    public TimeSpan EntryAnimationDuration
    {
        get => GetValue(EntryAnimationDurationProperty);
        set => SetValue(EntryAnimationDurationProperty, value);
    }

    /// <summary>Gets or sets the entry transition duration.</summary>
    [Obsolete("Use EntryAnimationDuration instead.")]
    public TimeSpan AnimationDuration
    {
        get => EntryAnimationDuration;
        set => EntryAnimationDuration = value;
    }

    /// <summary>Gets or sets the exit transition duration.</summary>
    public TimeSpan ExitAnimationDuration
    {
        get => GetValue(ExitAnimationDurationProperty);
        set => SetValue(ExitAnimationDurationProperty, value);
    }

    /// <summary>Gets or sets the duration used when remaining notifications move into their new positions.</summary>
    public TimeSpan ReflowAnimationDuration
    {
        get => GetValue(ReflowAnimationDurationProperty);
        set => SetValue(ReflowAnimationDurationProperty, value);
    }

    /// <summary>Gets or sets the easing used when a toast enters.</summary>
    public Easing EntryAnimationEasing { get => GetValue(EntryAnimationEasingProperty); set => SetValue(EntryAnimationEasingProperty, value); }

    /// <summary>Gets or sets the easing used when a toast enters.</summary>
    [Obsolete("Use EntryAnimationEasing instead.")]
    public Easing EntryEasing { get => EntryAnimationEasing; set => EntryAnimationEasing = value; }

    /// <summary>Gets or sets the easing used when a toast exits.</summary>
    public Easing ExitAnimationEasing { get => GetValue(ExitAnimationEasingProperty); set => SetValue(ExitAnimationEasingProperty, value); }

    /// <summary>Gets or sets the easing used when a toast exits.</summary>
    [Obsolete("Use ExitAnimationEasing instead.")]
    public Easing ExitEasing { get => ExitAnimationEasing; set => ExitAnimationEasing = value; }

    /// <summary>Gets or sets the entry and exit translation distance.</summary>
    public double SlideDistance { get => GetValue(SlideDistanceProperty); set => SetValue(SlideDistanceProperty, value); }

    /// <summary>Gets or sets the spacing between visible notifications.</summary>
    public double ToastSpacing { get => GetValue(ToastSpacingProperty); set => SetValue(ToastSpacingProperty, value); }

    /// <summary>Gets or sets the inset from the host edges.</summary>
    public Thickness ToastMargin { get => GetValue(ToastMarginProperty); set => SetValue(ToastMarginProperty, value); }

    /// <summary>Gets or sets the content template applied to generated toast controls.</summary>
    public IDataTemplate? ToastTemplate
    {
        get => GetValue(ToastTemplateProperty);
        set => SetValue(ToastTemplateProperty, value);
    }

    /// <summary>Gets or sets the control theme applied to generated toast controls.</summary>
    public ControlTheme? ToastTheme { get => GetValue(ToastThemeProperty); set => SetValue(ToastThemeProperty, value); }

    /// <summary>Gets the number of currently visible notifications.</summary>
    public int ActiveCount
    {
        get => _activeCount;
        private set => SetAndRaise(ActiveCountProperty, ref _activeCount, value);
    }

    /// <summary>Occurs after a toast is added to the visual tree.</summary>
    public event EventHandler<ToastOpenedEventArgs>? ToastOpened;

    /// <summary>Occurs after a toast is removed from the visual tree.</summary>
    public event EventHandler<ToastClosedEventArgs>? ToastClosed;

    internal IReadOnlyList<Toast> ActiveToasts => _activeEntries.Select(entry => entry.Control).ToArray();

    internal int WaitingCount => _waiting.Count;

    /// <inheritdoc />
    public void Present(ToastPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        if (_activeEntries.Any(entry => entry.Presentation.Id == presentation.Id) ||
            _waiting.Any(candidate => candidate.Id == presentation.Id))
        {
            return;
        }

        _waiting.Add(presentation);
        PromoteWaiting();
    }

    /// <inheritdoc />
    public bool Dismiss(Guid id, ToastDismissReason reason)
    {
        ToastEntry? active = _activeEntries.FirstOrDefault(entry => entry.Presentation.Id == id);
        if (active is not null)
        {
            if (!active.IsClosing)
            {
                BeginDismiss(active, reason);
            }

            return true;
        }

        int waitingIndex = _waiting.FindIndex(presentation => presentation.Id == id);
        if (waitingIndex < 0)
        {
            return false;
        }

        ToastPresentation waiting = _waiting[waitingIndex];
        _waiting.RemoveAt(waitingIndex);
        _attachedService?.Complete(this, HostId, waiting.Id, reason);
        ToastClosed?.Invoke(this, new ToastClosedEventArgs(waiting.Id, waiting.Request, reason));
        return true;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ClearPanelChildren();
        base.OnApplyTemplate(e);
        _topLeft = e.NameScope.Find<StackPanel>(TopLeftPartName);
        _topCenter = e.NameScope.Find<StackPanel>(TopCenterPartName);
        _topRight = e.NameScope.Find<StackPanel>(TopRightPartName);
        _bottomLeft = e.NameScope.Find<StackPanel>(BottomLeftPartName);
        _bottomCenter = e.NameScope.Find<StackPanel>(BottomCenterPartName);
        _bottomRight = e.NameScope.Find<StackPanel>(BottomRightPartName);
        ApplyPanelSpacing();

        foreach (ToastEntry entry in _activeEntries)
        {
            AddToPanel(entry);
        }

        PromoteWaiting();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateServiceRegistration();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Detach(this, _attachedHostId);
        }

        _attachedService = null;
        _attachedHostId = null;
        ResetPresentations();
        base.OnDetachedFromVisualTree(e);
    }

    private static bool IsValidDuration(TimeSpan value) =>
        value == Timeout.InfiniteTimeSpan || value > TimeSpan.Zero;

    private void PromoteWaiting()
    {
        while ((MaxVisible == 0 || _activeEntries.Count < MaxVisible) && _waiting.Count > 0)
        {
            ToastPresentation presentation = _waiting[0];
            ToastPlacement placement = presentation.Request.Placement ?? Placement;
            if (GetPanel(placement) is null)
            {
                return;
            }

            _waiting.RemoveAt(0);
            ShowPresentation(presentation, placement);
        }
    }

    private void ShowPresentation(ToastPresentation presentation, ToastPlacement placement)
    {
        ICommand? actionCommand = presentation.Request.ActionCommand is null
            ? null
            : new ToastActionCommand(
                presentation.Request.ActionCommand,
                presentation.Request.ActionCommandParameter,
                () =>
                {
                    if (presentation.Request.DismissOnAction)
                    {
                        RequestDismiss(presentation.Id, ToastDismissReason.Action);
                    }
                });
        var control = new Toast
        {
            Title = presentation.Request.Title,
            Severity = presentation.Request.Severity,
            Placement = placement,
            Content = presentation.Content,
            ContentTemplate = ToastTemplate,
            Theme = ToastTheme,
            HorizontalAlignment = GetToastHorizontalAlignment(placement),
            IsDismissible = presentation.Request.IsDismissible ?? IsDismissible,
            IsClickDismissEnabled = presentation.Request.IsClickDismissEnabled ?? IsClickDismissEnabled,
            ActionText = presentation.Request.ActionText,
            ActionCommand = actionCommand,
            ActionCommandParameter = presentation.Request.ActionCommandParameter,
            Opacity = 0,
        };
        var reflowTransform = new TranslateTransform();
        var transform = new TranslateTransform();
        control.RenderTransform = new TransformGroup
        {
            Children = { reflowTransform, transform },
        };
        control.RenderTransformOrigin = RelativePoint.Center;

        TimeSpan duration = presentation.Request.Duration ?? DefaultDuration;
        var entry = new ToastEntry(presentation, control, transform, reflowTransform, placement, duration);
        control.DismissRequested += OnToastDismissRequested;
        control.PointerEntered += OnToastPointerEntered;
        control.PointerExited += OnToastPointerExited;
        ConfigureTransitions(entry, EntryAnimationEasing, GetEntryAnimationDuration());
        ConfigureReflowTransition(entry);
        ApplyOffset(entry);
        Dictionary<ToastEntry, double> previousPositions = CapturePanelPositions(placement);
        _activeEntries.Add(entry);
        AddToPanel(entry);
        AnimateReflow(previousPositions);
        ActiveCount = _activeEntries.Count;
        ToastOpened?.Invoke(this, new ToastOpenedEventArgs(presentation.Id, presentation.Request, control));

        DispatcherTimer.RunOnce(() =>
        {
            if (entry.IsClosing || !_activeEntries.Contains(entry))
            {
                return;
            }

            control.Opacity = 1;
            transform.X = 0;
            transform.Y = 0;
            DispatcherTimer.RunOnce(
                () => StartLifetime(entry),
                GetEntryAnimationDuration(),
                DispatcherPriority.Normal);
        }, TimeSpan.Zero, DispatcherPriority.Render);
    }

    private void RequestDismiss(Guid id, ToastDismissReason reason)
    {
        if (_attachedService is null || !_attachedService.RequestDismiss(this, HostId, id, reason))
        {
            Dismiss(id, reason);
        }
    }

    private void BeginDismiss(ToastEntry entry, ToastDismissReason reason)
    {
        entry.IsClosing = true;
        StopLifetime(entry, preserveRemaining: false);
        TimeSpan duration = GetExitAnimationDuration();
        ConfigureTransitions(entry, ExitAnimationEasing, duration);
        entry.Control.Opacity = 0;
        ApplyOffset(entry);
        if (duration == TimeSpan.Zero)
        {
            FinishDismiss(entry, reason);
        }
        else
        {
            DispatcherTimer.RunOnce(
                () => FinishDismiss(entry, reason),
                duration,
                DispatcherPriority.Normal);
        }
    }

    private void FinishDismiss(ToastEntry entry, ToastDismissReason reason)
    {
        if (!_activeEntries.Remove(entry))
        {
            return;
        }

        Dictionary<ToastEntry, double> previousPositions = CapturePanelPositions(entry.Placement);
        DetachControl(entry);
        AnimateReflow(previousPositions);
        ActiveCount = _activeEntries.Count;
        _attachedService?.Complete(this, HostId, entry.Presentation.Id, reason);
        ToastClosed?.Invoke(
            this,
            new ToastClosedEventArgs(entry.Presentation.Id, entry.Presentation.Request, reason));
        PromoteWaiting();
    }

    private void StartLifetime(ToastEntry entry)
    {
        if (entry.IsClosing || !_activeEntries.Contains(entry) || entry.Remaining == Timeout.InfiniteTimeSpan)
        {
            return;
        }

        if (entry.Remaining <= TimeSpan.Zero)
        {
            RequestDismiss(entry.Presentation.Id, ToastDismissReason.Timeout);
            return;
        }

        var timer = new DispatcherTimer { Interval = entry.Remaining };
        entry.Timer = timer;
        entry.StartedTimestamp = Stopwatch.GetTimestamp();
        timer.Tick += OnLifetimeElapsed;
        timer.Start();
    }

    private void StopLifetime(ToastEntry entry, bool preserveRemaining)
    {
        if (entry.Timer is null)
        {
            return;
        }

        entry.Timer.Stop();
        entry.Timer.Tick -= OnLifetimeElapsed;
        entry.Timer = null;
        if (preserveRemaining)
        {
            entry.Remaining -= Stopwatch.GetElapsedTime(entry.StartedTimestamp);
            if (entry.Remaining < TimeSpan.Zero)
            {
                entry.Remaining = TimeSpan.Zero;
            }
        }
    }

    private void OnLifetimeElapsed(object? sender, EventArgs e)
    {
        ToastEntry? entry = _activeEntries.FirstOrDefault(candidate => ReferenceEquals(candidate.Timer, sender));
        if (entry is null)
        {
            return;
        }

        StopLifetime(entry, preserveRemaining: false);
        RequestDismiss(entry.Presentation.Id, ToastDismissReason.Timeout);
    }

    private void OnToastDismissRequested(object? sender, EventArgs e)
    {
        ToastEntry? entry = FindEntry(sender);
        if (entry is not null)
        {
            RequestDismiss(entry.Presentation.Id, ToastDismissReason.User);
        }
    }

    private void OnToastPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!PauseOnPointerOver)
        {
            return;
        }

        ToastEntry? entry = FindEntry(sender);
        if (entry is { IsClosing: false })
        {
            StopLifetime(entry, preserveRemaining: true);
        }
    }

    private void OnToastPointerExited(object? sender, PointerEventArgs e)
    {
        if (!PauseOnPointerOver)
        {
            return;
        }

        ToastEntry? entry = FindEntry(sender);
        if (entry is { IsClosing: false, Timer: null })
        {
            StartLifetime(entry);
        }
    }

    private ToastEntry? FindEntry(object? sender) =>
        _activeEntries.FirstOrDefault(entry => ReferenceEquals(entry.Control, sender));

    private void ConfigureTransitions(ToastEntry entry, Easing easing, TimeSpan duration)
    {
        entry.Control.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = easing,
            },
        };
        entry.Transform.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = TranslateTransform.XProperty,
                Duration = duration,
                Easing = easing,
            },
            new DoubleTransition
            {
                Property = TranslateTransform.YProperty,
                Duration = duration,
                Easing = easing,
            },
        };
    }

    private void ConfigureReflowTransition(ToastEntry entry)
    {
        entry.ReflowTransform.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = TranslateTransform.YProperty,
                Duration = GetReflowAnimationDuration(),
                Easing = EntryAnimationEasing,
            },
        };
    }

    private void ApplyOffset(ToastEntry entry)
    {
        entry.Transform.X = entry.Placement switch
        {
            ToastPlacement.TopLeft or ToastPlacement.BottomLeft => -SlideDistance,
            ToastPlacement.TopRight or ToastPlacement.BottomRight => SlideDistance,
            _ => 0,
        };
        entry.Transform.Y = entry.Placement switch
        {
            ToastPlacement.TopCenter => -SlideDistance,
            ToastPlacement.BottomCenter => SlideDistance,
            _ => 0,
        };
    }

    private TimeSpan GetEntryAnimationDuration() =>
        IsAnimationEnabled ? EntryAnimationDuration : TimeSpan.Zero;

    private TimeSpan GetExitAnimationDuration() =>
        IsAnimationEnabled ? ExitAnimationDuration : TimeSpan.Zero;

    private TimeSpan GetReflowAnimationDuration() =>
        IsAnimationEnabled ? ReflowAnimationDuration : TimeSpan.Zero;

    private Dictionary<ToastEntry, double> CapturePanelPositions(ToastPlacement placement) =>
        _activeEntries
            .Where(entry => entry.Placement == placement)
            .ToDictionary(entry => entry, entry => entry.Control.Bounds.Position.Y);

    private void AnimateReflow(IReadOnlyDictionary<ToastEntry, double> previousPositions)
    {
        if (previousPositions.Count == 0 || GetReflowAnimationDuration() == TimeSpan.Zero)
        {
            return;
        }

        UpdateLayout();
        ToastEntry[] movedEntries = previousPositions
            .Where(pair => _activeEntries.Contains(pair.Key))
            .Where(pair => Math.Abs(pair.Value - pair.Key.Control.Bounds.Position.Y) > 0.01)
            .Select(pair =>
            {
                pair.Key.ReflowTransform.Y = pair.Value - pair.Key.Control.Bounds.Position.Y;
                return pair.Key;
            })
            .ToArray();
        if (movedEntries.Length == 0)
        {
            return;
        }

        DispatcherTimer.RunOnce(
            () =>
            {
                foreach (ToastEntry movedEntry in movedEntries)
                {
                    if (_activeEntries.Contains(movedEntry))
                    {
                        movedEntry.ReflowTransform.Y = 0;
                    }
                }
            },
            TimeSpan.Zero,
            DispatcherPriority.Render);
    }

    private void AddToPanel(ToastEntry entry)
    {
        StackPanel? panel = GetPanel(entry.Placement);
        if (panel is null || panel.Children.Contains(entry.Control))
        {
            return;
        }

        if (IsTopPlacement(entry.Placement))
        {
            panel.Children.Insert(0, entry.Control);
        }
        else
        {
            panel.Children.Add(entry.Control);
        }
    }

    private StackPanel? GetPanel(ToastPlacement placement) => placement switch
    {
        ToastPlacement.TopLeft => _topLeft,
        ToastPlacement.TopCenter => _topCenter,
        ToastPlacement.TopRight => _topRight,
        ToastPlacement.BottomLeft => _bottomLeft,
        ToastPlacement.BottomCenter => _bottomCenter,
        ToastPlacement.BottomRight => _bottomRight,
        _ => _topRight,
    };

    private static bool IsTopPlacement(ToastPlacement placement) =>
        placement is ToastPlacement.TopLeft or ToastPlacement.TopCenter or ToastPlacement.TopRight;

    private static HorizontalAlignment GetToastHorizontalAlignment(ToastPlacement placement) => placement switch
    {
        ToastPlacement.TopLeft or ToastPlacement.BottomLeft => HorizontalAlignment.Left,
        ToastPlacement.TopCenter or ToastPlacement.BottomCenter => HorizontalAlignment.Center,
        _ => HorizontalAlignment.Right,
    };

    private void ApplyPanelSpacing()
    {
        foreach (StackPanel? panel in GetPanels())
        {
            if (panel is not null)
            {
                panel.Spacing = ToastSpacing;
            }
        }
    }

    private void UpdateActiveVisuals()
    {
        foreach (ToastEntry entry in _activeEntries)
        {
            entry.Control.ContentTemplate = ToastTemplate;
            entry.Control.Theme = ToastTheme;
        }
    }

    private void UpdateServiceRegistration()
    {
        if (VisualRoot is null)
        {
            return;
        }

        IToastHostService? next = Service;
        string hostId = HostId;
        if (ReferenceEquals(next, _attachedService) &&
            string.Equals(hostId, _attachedHostId, StringComparison.Ordinal))
        {
            return;
        }

        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Detach(this, _attachedHostId);
            ResetPresentations();
        }

        _attachedService = next;
        _attachedHostId = next is null ? null : hostId;
        next?.Attach(this, hostId);
    }

    private void ResetPresentations()
    {
        foreach (ToastEntry entry in _activeEntries.ToArray())
        {
            DetachControl(entry);
        }

        _activeEntries.Clear();
        _waiting.Clear();
        ActiveCount = 0;
        ClearPanelChildren();
    }

    private void DetachControl(ToastEntry entry)
    {
        StopLifetime(entry, preserveRemaining: false);
        entry.Control.DismissRequested -= OnToastDismissRequested;
        entry.Control.PointerEntered -= OnToastPointerEntered;
        entry.Control.PointerExited -= OnToastPointerExited;
        GetPanel(entry.Placement)?.Children.Remove(entry.Control);
        entry.Control.Transitions = null;
        entry.Transform.Transitions = null;
        entry.ReflowTransform.Transitions = null;
    }

    private void ClearPanelChildren()
    {
        foreach (StackPanel? panel in GetPanels())
        {
            panel?.Children.Clear();
        }
    }

    private IEnumerable<StackPanel?> GetPanels()
    {
        yield return _topLeft;
        yield return _topCenter;
        yield return _topRight;
        yield return _bottomLeft;
        yield return _bottomCenter;
        yield return _bottomRight;
    }

    private sealed class ToastEntry(
        ToastPresentation presentation,
        Toast control,
        TranslateTransform transform,
        TranslateTransform reflowTransform,
        ToastPlacement placement,
        TimeSpan duration)
    {
        public ToastPresentation Presentation { get; } = presentation;

        public Toast Control { get; } = control;

        public TranslateTransform Transform { get; } = transform;

        public TranslateTransform ReflowTransform { get; } = reflowTransform;

        public ToastPlacement Placement { get; } = placement;

        public TimeSpan Remaining { get; set; } = duration;

        public DispatcherTimer? Timer { get; set; }

        public long StartedTimestamp { get; set; }

        public bool IsClosing { get; set; }
    }

    private sealed class ToastActionCommand(ICommand inner, object? parameter, Action executed) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => inner.CanExecuteChanged += value;
            remove => inner.CanExecuteChanged -= value;
        }

        public bool CanExecute(object? ignored) => inner.CanExecute(parameter);

        public void Execute(object? ignored)
        {
            if (!CanExecute(ignored))
            {
                return;
            }

            inner.Execute(parameter);
            executed();
        }
    }
}
