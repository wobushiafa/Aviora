using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aviora.Presentation.Drawers;
using DrawerPlacement = Aviora.Presentation.Drawers.DrawerPlacement;

namespace Aviora.Controls;

/// <summary>
/// Hosts primary content and presents secondary content from any edge.
/// </summary>
[TemplatePart(OverlayPartName, typeof(Border))]
[TemplatePart(PanePartName, typeof(ContentControl))]
public class Drawer : ContentControl, IDrawerHost
{
    /// <summary>The default identifier used to match service requests to a host.</summary>
    public const string DefaultHostId = DrawerHost.DefaultId;
    internal const string OverlayPartName = "PART_Overlay";
    internal const string PanePartName = "PART_Pane";

    /// <summary>Defines the <see cref="DrawerContent"/> property.</summary>
    public static readonly StyledProperty<object?> DrawerContentProperty =
        AvaloniaProperty.Register<Drawer, object?>(nameof(DrawerContent));

    /// <summary>Defines the <see cref="DrawerContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> DrawerContentTemplateProperty =
        AvaloniaProperty.Register<Drawer, IDataTemplate?>(nameof(DrawerContentTemplate));

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Drawer, bool>(nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="Placement"/> property.</summary>
    public static readonly StyledProperty<DrawerPlacement> PlacementProperty =
        AvaloniaProperty.Register<Drawer, DrawerPlacement>(nameof(Placement), DrawerPlacement.Right);

    /// <summary>Defines the <see cref="DisplayMode"/> property.</summary>
    public static readonly StyledProperty<DrawerDisplayMode> DisplayModeProperty =
        AvaloniaProperty.Register<Drawer, DrawerDisplayMode>(nameof(DisplayMode));

    /// <summary>Defines the <see cref="DrawerSize"/> property.</summary>
    public static readonly StyledProperty<double> DrawerSizeProperty =
        AvaloniaProperty.Register<Drawer, double>(nameof(DrawerSize), double.NaN, validate: value => double.IsNaN(value) || value >= 0);

    /// <summary>Defines the <see cref="MinDrawerSize"/> property.</summary>
    public static readonly StyledProperty<double> MinDrawerSizeProperty =
        AvaloniaProperty.Register<Drawer, double>(nameof(MinDrawerSize), 0, validate: value => value >= 0);

    /// <summary>Defines the <see cref="MaxDrawerSize"/> property.</summary>
    public static readonly StyledProperty<double> MaxDrawerSizeProperty =
        AvaloniaProperty.Register<Drawer, double>(nameof(MaxDrawerSize), double.PositiveInfinity, validate: value => value >= 0);

    /// <summary>Defines the <see cref="IsLightDismissEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
        AvaloniaProperty.Register<Drawer, bool>(nameof(IsLightDismissEnabled), true);

    /// <summary>Defines the <see cref="IsEscapeKeyEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsEscapeKeyEnabledProperty =
        AvaloniaProperty.Register<Drawer, bool>(nameof(IsEscapeKeyEnabled), true);

    /// <summary>Defines the <see cref="IsOverlayVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsOverlayVisibleProperty =
        AvaloniaProperty.Register<Drawer, bool>(nameof(IsOverlayVisible), true);

    /// <summary>Defines the <see cref="OverlayBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<Drawer, IBrush?>(nameof(OverlayBrush), new ImmutableSolidColorBrush(Color.FromArgb(112, 0, 0, 0)));

    /// <summary>Defines the <see cref="Service"/> property.</summary>
    public static readonly StyledProperty<IDrawerHostService?> ServiceProperty =
        AvaloniaProperty.Register<Drawer, IDrawerHostService?>(nameof(Service));

    /// <summary>Defines the <see cref="HostId"/> property.</summary>
    public static readonly StyledProperty<string> HostIdProperty =
        AvaloniaProperty.Register<Drawer, string>(nameof(HostId), DefaultHostId, validate: value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Defines the <see cref="IsAnimationEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<Drawer, bool>(nameof(IsAnimationEnabled), true);

    /// <summary>Defines the <see cref="PaneAnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> PaneAnimationDurationProperty =
        AvaloniaProperty.Register<Drawer, TimeSpan>(nameof(PaneAnimationDuration), TimeSpan.FromMilliseconds(260),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="OverlayAnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> OverlayAnimationDurationProperty =
        AvaloniaProperty.Register<Drawer, TimeSpan>(nameof(OverlayAnimationDuration), TimeSpan.FromMilliseconds(180),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="PaneEasing"/> property.</summary>
    public static readonly StyledProperty<Easing> PaneEasingProperty =
        AvaloniaProperty.Register<Drawer, Easing>(nameof(PaneEasing), new CubicEaseOut());

    /// <summary>Defines the <see cref="OverlayEasing"/> property.</summary>
    public static readonly StyledProperty<Easing> OverlayEasingProperty =
        AvaloniaProperty.Register<Drawer, Easing>(nameof(OverlayEasing), new CubicEaseOut());

    /// <summary>Defines the <see cref="PaneCornerRadius"/> property.</summary>
    public static readonly StyledProperty<CornerRadius> PaneCornerRadiusProperty =
        AvaloniaProperty.Register<Drawer, CornerRadius>(nameof(PaneCornerRadius));

    /// <summary>Defines the <see cref="PaneBoxShadow"/> property.</summary>
    public static readonly StyledProperty<BoxShadows> PaneBoxShadowProperty =
        AvaloniaProperty.Register<Drawer, BoxShadows>(nameof(PaneBoxShadow), new BoxShadows(new BoxShadow
        {
            Blur = 24,
            OffsetX = 0,
            OffsetY = 8,
            Color = Color.FromArgb(48, 0, 0, 0),
        }));

    private Border? _overlay;
    private Grid? _root;
    private IInputElement? _previousFocus;
    private DrawerRequest? _activeRequest;
    private HostOptions? _hostOptions;
    private IDrawerHostService? _attachedService;
    private string? _attachedHostId;
    private Border? _paneSurface;
    private TranslateTransform? _paneTransform;
    private readonly CloseDrawerCommand _closeCommand;
    private bool _internalChange;
    private bool _isPresented;
    private bool _isVisualOpen;
    private int _transitionVersion;

    static Drawer()
    {
        IsOpenProperty.Changed.AddClassHandler<Drawer>((drawer, args) => drawer.OnIsOpenChanged(args));
        PlacementProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdatePseudoClasses());
        PlacementProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdatePaneOffset(drawer._isVisualOpen));
        DisplayModeProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdatePseudoClasses());
        DisplayModeProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdateLayoutTracks());
        PlacementProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdateLayoutTracks());
        IsOverlayVisibleProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdatePseudoClasses());
        IsOverlayVisibleProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ApplyOverlayState(drawer._isVisualOpen));
        IsLightDismissEnabledProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ApplyOverlayState(drawer._isVisualOpen));
        IsAnimationEnabledProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ConfigureTransitions());
        PaneAnimationDurationProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ConfigureTransitions());
        OverlayAnimationDurationProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ConfigureTransitions());
        PaneEasingProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ConfigureTransitions());
        OverlayEasingProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.ConfigureTransitions());
        DrawerSizeProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdatePaneOffset(drawer._isVisualOpen));
        BoundsProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdatePaneOffset(drawer._isVisualOpen));
        ServiceProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdateServiceRegistration());
        HostIdProperty.Changed.AddClassHandler<Drawer>((drawer, _) => drawer.UpdateServiceRegistration());
    }

    /// <summary>Gets or sets the content displayed in the drawer pane.</summary>
    public object? DrawerContent { get => GetValue(DrawerContentProperty); set => SetValue(DrawerContentProperty, value); }

    /// <summary>Gets or sets the template used to render <see cref="DrawerContent"/>.</summary>
    public IDataTemplate? DrawerContentTemplate { get => GetValue(DrawerContentTemplateProperty); set => SetValue(DrawerContentTemplateProperty, value); }

    /// <summary>Gets or sets whether the drawer is open.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Gets or sets the edge from which the drawer opens.</summary>
    public DrawerPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Gets or sets how the drawer is composed with primary content.</summary>
    public DrawerDisplayMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }

    /// <summary>
    /// Gets or sets the pane width for horizontal placement or height for vertical placement.
    /// Set to NaN to measure the pane from its content.
    /// </summary>
    public double DrawerSize { get => GetValue(DrawerSizeProperty); set => SetValue(DrawerSizeProperty, value); }

    /// <summary>Gets or sets the minimum pane size.</summary>
    public double MinDrawerSize { get => GetValue(MinDrawerSizeProperty); set => SetValue(MinDrawerSizeProperty, value); }

    /// <summary>Gets or sets the maximum pane size.</summary>
    public double MaxDrawerSize { get => GetValue(MaxDrawerSizeProperty); set => SetValue(MaxDrawerSizeProperty, value); }

    /// <summary>Gets or sets whether interacting with the overlay closes the drawer.</summary>
    public bool IsLightDismissEnabled { get => GetValue(IsLightDismissEnabledProperty); set => SetValue(IsLightDismissEnabledProperty, value); }

    /// <summary>Gets or sets whether the Escape key closes the drawer.</summary>
    public bool IsEscapeKeyEnabled { get => GetValue(IsEscapeKeyEnabledProperty); set => SetValue(IsEscapeKeyEnabledProperty, value); }

    /// <summary>Gets or sets whether the modal overlay is visible.</summary>
    public bool IsOverlayVisible { get => GetValue(IsOverlayVisibleProperty); set => SetValue(IsOverlayVisibleProperty, value); }

    /// <summary>Gets or sets the brush used to dim primary content.</summary>
    public IBrush? OverlayBrush { get => GetValue(OverlayBrushProperty); set => SetValue(OverlayBrushProperty, value); }

    /// <summary>Gets or sets the service whose requests this drawer hosts.</summary>
    public IDrawerHostService? Service { get => GetValue(ServiceProperty); set => SetValue(ServiceProperty, value); }

    /// <summary>Gets or sets the identifier used to route service requests.</summary>
    public string HostId { get => GetValue(HostIdProperty); set => SetValue(HostIdProperty, value); }

    /// <summary>Gets or sets whether the pane and overlay transitions are enabled.</summary>
    public bool IsAnimationEnabled { get => GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    /// <summary>Gets or sets the duration of the pane slide transition.</summary>
    public TimeSpan PaneAnimationDuration { get => GetValue(PaneAnimationDurationProperty); set => SetValue(PaneAnimationDurationProperty, value); }

    /// <summary>Gets or sets the duration of the overlay fade transition.</summary>
    public TimeSpan OverlayAnimationDuration { get => GetValue(OverlayAnimationDurationProperty); set => SetValue(OverlayAnimationDurationProperty, value); }

    /// <summary>Gets or sets the easing function used by the pane transition.</summary>
    public Easing PaneEasing { get => GetValue(PaneEasingProperty); set => SetValue(PaneEasingProperty, value); }

    /// <summary>Gets or sets the easing function used by the overlay transition.</summary>
    public Easing OverlayEasing { get => GetValue(OverlayEasingProperty); set => SetValue(OverlayEasingProperty, value); }

    /// <summary>Gets or sets the pane's corner radius.</summary>
    public CornerRadius PaneCornerRadius { get => GetValue(PaneCornerRadiusProperty); set => SetValue(PaneCornerRadiusProperty, value); }

    /// <summary>Gets or sets the pane's shadow.</summary>
    public BoxShadows PaneBoxShadow { get => GetValue(PaneBoxShadowProperty); set => SetValue(PaneBoxShadowProperty, value); }

    /// <summary>
    /// Gets a command that closes the drawer and uses its parameter as the result.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>Occurs before the drawer opens.</summary>
    public event EventHandler<DrawerOpeningEventArgs>? Opening;

    /// <summary>Occurs after the drawer enters the open state.</summary>
    public event EventHandler? Opened;

    /// <summary>Occurs before the drawer closes and allows the operation to be canceled.</summary>
    public event EventHandler<DrawerClosingEventArgs>? Closing;

    /// <summary>Occurs after the drawer closes.</summary>
    public event EventHandler<DrawerClosedEventArgs>? Closed;

    /// <summary>Initializes a new drawer.</summary>
    public Drawer()
    {
        _closeCommand = new CloseDrawerCommand(this);
        CloseCommand = _closeCommand;
    }

    /// <summary>Attempts to close the drawer with an optional result.</summary>
    public bool TryClose(DrawerCloseReason reason = DrawerCloseReason.Programmatic, object? result = null)
    {
        if (!IsOpen)
        {
            return false;
        }

        var args = new DrawerClosingEventArgs(reason, result);
        Closing?.Invoke(this, args);
        if (args.Cancel)
        {
            return false;
        }

        _internalChange = true;
        SetCurrentValue(IsOpenProperty, false);
        _internalChange = false;
        BeginClose(reason, result);
        return true;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_overlay is not null)
        {
            _overlay.PointerReleased -= OnOverlayPointerReleased;
        }

        base.OnApplyTemplate(e);
        _overlay = e.NameScope.Find<Border>(OverlayPartName);
        _paneSurface = e.NameScope.Find<Border>("PART_PaneSurface");
        _root = e.NameScope.Find<Grid>("PART_Root");
        if (_overlay is not null)
        {
            _overlay.PointerReleased += OnOverlayPointerReleased;
        }

        _paneTransform = new TranslateTransform();
        if (_paneSurface is not null)
        {
            _paneSurface.RenderTransform = _paneTransform;
        }

        UpdatePseudoClasses();
        UpdateLayoutTracks();
        UpdatePaneOffset(_isVisualOpen);
        ApplyOverlayState(_isVisualOpen);
        ConfigureTransitions();
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
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Escape && IsOpen && IsEscapeKeyEnabled)
        {
            e.Handled = TryClose(DrawerCloseReason.EscapeKey);
        }
    }

    internal void Present(DrawerRequest request, object? content)
    {
        _hostOptions = new HostOptions(
            Placement,
            DisplayMode,
            DrawerSize,
            IsLightDismissEnabled,
            IsEscapeKeyEnabled,
            IsOverlayVisible,
            IsAnimationEnabled,
            PaneAnimationDuration,
            OverlayAnimationDuration);
        _activeRequest = request;
        DrawerContent = content;
        if (request.Placement is { } placement)
        {
            Placement = placement;
        }

        if (request.DisplayMode is { } displayMode)
        {
            DisplayMode = displayMode;
        }

        if (request.Size is { } size)
        {
            DrawerSize = size;
        }

        if (request.IsLightDismissEnabled is { } lightDismiss)
        {
            IsLightDismissEnabled = lightDismiss;
        }

        if (request.IsEscapeKeyEnabled is { } escapeKey)
        {
            IsEscapeKeyEnabled = escapeKey;
        }

        if (request.IsOverlayVisible is { } overlayVisible)
        {
            IsOverlayVisible = overlayVisible;
        }

        if (request.IsAnimationEnabled is { } animationEnabled)
        {
            IsAnimationEnabled = animationEnabled;
        }

        if (request.PaneAnimationDuration is { } paneAnimationDuration)
        {
            PaneAnimationDuration = paneAnimationDuration;
        }

        if (request.OverlayAnimationDuration is { } overlayAnimationDuration)
        {
            OverlayAnimationDuration = overlayAnimationDuration;
        }

        if (!IsOpen)
        {
            IsOpen = true;
        }
    }

    void IDrawerHost.Present(DrawerRequest request, object? content) => Present(request, content);

    private void OnIsOpenChanged(AvaloniaPropertyChangedEventArgs args)
    {
        var isOpen = args.GetNewValue<bool>();
        UpdatePseudoClasses();
        UpdateLayoutTracks();
        _closeCommand.RaiseCanExecuteChanged();
        if (_internalChange)
        {
            return;
        }

        if (isOpen)
        {
            BeginOpen();
            _previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            Opening?.Invoke(this, new DrawerOpeningEventArgs(_activeRequest));
            FocusDrawerContent();
        }
        else
        {
            var closing = new DrawerClosingEventArgs(DrawerCloseReason.Programmatic, null);
            Closing?.Invoke(this, closing);
            if (closing.Cancel)
            {
                _internalChange = true;
                SetCurrentValue(IsOpenProperty, true);
                _internalChange = false;
                return;
            }

            BeginClose(DrawerCloseReason.Programmatic, null);
        }
    }

    private void BeginOpen()
    {
        _transitionVersion++;
        var version = _transitionVersion;
        ClearTransitions();
        UpdatePaneOffset();
        ApplyOverlayState(false);
        _isPresented = true;
        _isVisualOpen = false;
        UpdatePseudoClasses();
        UpdateLayoutTracks();
        ApplyOverlayState(false);
        ConfigureTransitions();
        DispatcherTimer.RunOnce(() =>
        {
            if (!IsOpen || version != _transitionVersion)
            {
                return;
            }

            _isVisualOpen = true;
            UpdatePseudoClasses();
            ApplyOverlayState(true);
            UpdatePaneOffset(open: true);
            DispatcherTimer.RunOnce(() =>
            {
                if (version == _transitionVersion && IsOpen)
                {
                    Opened?.Invoke(this, EventArgs.Empty);
                }
            }, IsAnimationEnabled ? Max(PaneAnimationDuration, OverlayAnimationDuration) : TimeSpan.Zero, DispatcherPriority.Render);
        }, TimeSpan.Zero, DispatcherPriority.Render);
    }

    private void BeginClose(DrawerCloseReason reason, object? result)
    {
        _transitionVersion++;
        _isVisualOpen = false;
        UpdatePseudoClasses();
        ApplyOverlayState(false);
        UpdatePaneOffset();
        var version = _transitionVersion;
        var closeDuration = _isPresented && _paneSurface is not null && IsAnimationEnabled
            ? Max(PaneAnimationDuration, OverlayAnimationDuration)
            : TimeSpan.Zero;
        if (closeDuration <= TimeSpan.Zero)
        {
            FinishClose(version, reason, result);
            return;
        }

        DispatcherTimer.RunOnce(() =>
        {
            FinishClose(version, reason, result);
        }, closeDuration, DispatcherPriority.Render);
    }

    private void FinishClose(int version, DrawerCloseReason reason, object? result)
    {
        if (version != _transitionVersion)
        {
            return;
        }

        _isPresented = false;
        UpdatePseudoClasses();
        UpdateLayoutTracks();
        ApplyOverlayState(false);
        CompleteClose(reason, result);
    }

    private static TimeSpan Max(TimeSpan first, TimeSpan second) => first >= second ? first : second;

    private void CompleteClose(DrawerCloseReason reason, object? result)
    {
        Closed?.Invoke(this, new DrawerClosedEventArgs(reason, result));
        RestoreHostOptions();
        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Complete(this, _attachedHostId, result, reason);
        }

        _activeRequest = null;

        if (_previousFocus is { Focusable: true } previous)
        {
            previous.Focus();
        }

        _previousFocus = null;
    }

    private void FocusDrawerContent()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var pane = this.FindDescendantOfType<ContentControl>(true, control => control.Name == PanePartName);
            var focusable = pane?.GetVisualDescendants().OfType<Control>().FirstOrDefault(control => control.Focusable && control.IsEffectivelyEnabled);
            (focusable ?? pane)?.Focus();
        });
    }

    private void OnOverlayPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (IsLightDismissEnabled)
        {
            e.Handled = TryClose(DrawerCloseReason.LightDismiss);
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":open", _isVisualOpen);
        PseudoClasses.Set(":closed", !_isVisualOpen);
        PseudoClasses.Set(":presented", _isPresented);
        PseudoClasses.Set(":left", Placement == DrawerPlacement.Left);
        PseudoClasses.Set(":top", Placement == DrawerPlacement.Top);
        PseudoClasses.Set(":right", Placement == DrawerPlacement.Right);
        PseudoClasses.Set(":bottom", Placement == DrawerPlacement.Bottom);
        PseudoClasses.Set(":overlay", DisplayMode == DrawerDisplayMode.Overlay);
        PseudoClasses.Set(":push", DisplayMode == DrawerDisplayMode.Push);
        PseudoClasses.Set(":overlay-visible", IsOverlayVisible);
    }

    private void UpdateLayoutTracks()
    {
        if (_root is null)
        {
            return;
        }

        var horizontalPush = _isPresented && DisplayMode == DrawerDisplayMode.Push &&
            Placement is DrawerPlacement.Left or DrawerPlacement.Right;
        var verticalPush = _isPresented && DisplayMode == DrawerDisplayMode.Push &&
            Placement is DrawerPlacement.Top or DrawerPlacement.Bottom;

        _root.ColumnDefinitions = horizontalPush
            ? Placement == DrawerPlacement.Left
                ? new ColumnDefinitions("Auto,*")
                : new ColumnDefinitions("*,Auto")
            : new ColumnDefinitions("*,Auto");
        _root.RowDefinitions = verticalPush
            ? Placement == DrawerPlacement.Top
                ? new RowDefinitions("Auto,*")
                : new RowDefinitions("*,Auto")
            : new RowDefinitions("Auto,*");
    }

    private void UpdatePaneOffset(bool open = false)
    {
        if (_paneTransform is null)
        {
            return;
        }

        var distance = Placement is DrawerPlacement.Left or DrawerPlacement.Right
            ? _paneSurface?.Bounds.Width > 0 ? _paneSurface.Bounds.Width : DrawerSize
            : _paneSurface?.Bounds.Height > 0 ? _paneSurface.Bounds.Height : DrawerSize;
        if (double.IsNaN(distance) || double.IsInfinity(distance))
        {
            distance = 0;
        }

        if (open)
        {
            _paneTransform.X = 0;
            _paneTransform.Y = 0;
        }
        else
        {
            _paneTransform.X = Placement == DrawerPlacement.Left ? -distance : Placement == DrawerPlacement.Right ? distance : 0;
            _paneTransform.Y = Placement == DrawerPlacement.Top ? -distance : Placement == DrawerPlacement.Bottom ? distance : 0;
        }
    }

    private void ConfigureTransitions()
    {
        if (_paneTransform is null || _overlay is null)
        {
            return;
        }

        var paneDuration = IsAnimationEnabled ? PaneAnimationDuration : TimeSpan.Zero;
        var overlayDuration = IsAnimationEnabled ? OverlayAnimationDuration : TimeSpan.Zero;
        _paneTransform.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = TranslateTransform.XProperty,
                Duration = paneDuration,
                Easing = PaneEasing,
            },
            new DoubleTransition
            {
                Property = TranslateTransform.YProperty,
                Duration = paneDuration,
                Easing = PaneEasing,
            },
        };
        _overlay.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = overlayDuration,
                Easing = OverlayEasing,
            },
        };
    }

    private void ClearTransitions()
    {
        if (_paneTransform is not null)
        {
            _paneTransform.Transitions = null;
        }

        if (_overlay is not null)
        {
            _overlay.Transitions = null;
        }
    }

    private void ApplyOverlayState(bool open)
    {
        if (_overlay is not null)
        {
            _overlay.IsHitTestVisible = open && IsLightDismissEnabled;
            _overlay.Opacity = open && IsOverlayVisible ? 1 : 0;
            _overlay.IsVisible = _isPresented;
        }

        if (_paneSurface is not null)
        {
            _paneSurface.IsHitTestVisible = open;
            _paneSurface.IsVisible = _isPresented;
        }
    }

    private void UpdateServiceRegistration()
    {
        if (VisualRoot is null)
        {
            return;
        }

        var next = Service;
        if (ReferenceEquals(next, _attachedService) && string.Equals(HostId, _attachedHostId, StringComparison.Ordinal))
        {
            return;
        }

        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Detach(this, _attachedHostId);
        }

        _attachedService = next;
        _attachedHostId = next is null ? null : HostId;
        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Attach(this, _attachedHostId);
        }
    }

    private void RestoreHostOptions()
    {
        if (_hostOptions is not { } options)
        {
            return;
        }

        Placement = options.Placement;
        DisplayMode = options.DisplayMode;
        DrawerSize = options.Size;
        IsLightDismissEnabled = options.IsLightDismissEnabled;
        IsEscapeKeyEnabled = options.IsEscapeKeyEnabled;
        IsOverlayVisible = options.IsOverlayVisible;
        IsAnimationEnabled = options.IsAnimationEnabled;
        PaneAnimationDuration = options.PaneAnimationDuration;
        OverlayAnimationDuration = options.OverlayAnimationDuration;
        _hostOptions = null;
    }

    private sealed class CloseDrawerCommand(Drawer owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => owner.IsOpen;

        public void Execute(object? parameter) => owner.TryClose(DrawerCloseReason.Command, parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record HostOptions(
        DrawerPlacement Placement,
        DrawerDisplayMode DisplayMode,
        double Size,
        bool IsLightDismissEnabled,
        bool IsEscapeKeyEnabled,
        bool IsOverlayVisible,
        bool IsAnimationEnabled,
        TimeSpan PaneAnimationDuration,
        TimeSpan OverlayAnimationDuration);

}
