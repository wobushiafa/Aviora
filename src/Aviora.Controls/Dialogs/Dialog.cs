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
using Aviora.Presentation.Dialogs;

namespace Aviora.Controls;

/// <summary>Hosts primary content and presents modal content above it.</summary>
[TemplatePart(OverlayPartName, typeof(Border))]
[TemplatePart(SurfacePartName, typeof(Border))]
[TemplatePart(PresenterPartName, typeof(ContentControl))]
[TemplatePart(LayerHostPartName, typeof(Panel))]
public class Dialog : ContentControl, IDialogHost
{
    /// <summary>The default identifier used to match service requests to a host.</summary>
    public const string DefaultHostId = DialogHost.DefaultId;
    internal const string OverlayPartName = "PART_DialogOverlay";
    internal const string SurfacePartName = "PART_DialogSurface";
    internal const string PresenterPartName = "PART_DialogPresenter";
    internal const string LayerHostPartName = "PART_DialogLayerHost";

    /// <summary>Defines the <see cref="DialogContent"/> property.</summary>
    public static readonly StyledProperty<object?> DialogContentProperty =
        AvaloniaProperty.Register<Dialog, object?>(nameof(DialogContent));

    /// <summary>Defines the <see cref="DialogContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> DialogContentTemplateProperty =
        AvaloniaProperty.Register<Dialog, IDataTemplate?>(nameof(DialogContentTemplate));

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="DialogWidth"/> property.</summary>
    public static readonly StyledProperty<double> DialogWidthProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(DialogWidth), double.NaN, validate: IsValidSize);

    /// <summary>Defines the <see cref="DialogHeight"/> property.</summary>
    public static readonly StyledProperty<double> DialogHeightProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(DialogHeight), double.NaN, validate: IsValidSize);

    /// <summary>Defines the <see cref="MinDialogWidth"/> property.</summary>
    public static readonly StyledProperty<double> MinDialogWidthProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(MinDialogWidth), 280, validate: value => value >= 0);

    /// <summary>Defines the <see cref="MaxDialogWidth"/> property.</summary>
    public static readonly StyledProperty<double> MaxDialogWidthProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(MaxDialogWidth), 720, validate: value => value >= 0);

    /// <summary>Defines the <see cref="MinDialogHeight"/> property.</summary>
    public static readonly StyledProperty<double> MinDialogHeightProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(MinDialogHeight), 0, validate: value => value >= 0);

    /// <summary>Defines the <see cref="MaxDialogHeight"/> property.</summary>
    public static readonly StyledProperty<double> MaxDialogHeightProperty =
        AvaloniaProperty.Register<Dialog, double>(nameof(MaxDialogHeight), double.PositiveInfinity, validate: value => value >= 0);

    /// <summary>Defines the <see cref="IsLightDismissEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsLightDismissEnabled));

    /// <summary>Defines the <see cref="IsEscapeKeyEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsEscapeKeyEnabledProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsEscapeKeyEnabled), true);

    /// <summary>Defines the <see cref="IsOverlayVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsOverlayVisibleProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsOverlayVisible), true);

    /// <summary>Defines the <see cref="OverlayBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<Dialog, IBrush?>(
            nameof(OverlayBrush),
            new ImmutableSolidColorBrush(Color.FromArgb(112, 0, 0, 0)));

    /// <summary>Defines the <see cref="Service"/> property.</summary>
    public static readonly StyledProperty<IDialogHostService?> ServiceProperty =
        AvaloniaProperty.Register<Dialog, IDialogHostService?>(nameof(Service));

    /// <summary>Defines the <see cref="HostId"/> property.</summary>
    public static readonly StyledProperty<string> HostIdProperty =
        AvaloniaProperty.Register<Dialog, string>(
            nameof(HostId),
            DefaultHostId,
            validate: value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Defines the <see cref="IsAnimationEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsAnimationEnabled), true);

    /// <summary>Defines the <see cref="AnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<Dialog, TimeSpan>(
            nameof(AnimationDuration),
            TimeSpan.FromMilliseconds(180),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="DialogEasing"/> property.</summary>
    public static readonly StyledProperty<Easing> DialogEasingProperty =
        AvaloniaProperty.Register<Dialog, Easing>(nameof(DialogEasing), new CubicEaseOut());

    /// <summary>Defines the <see cref="DialogCornerRadius"/> property.</summary>
    public static readonly StyledProperty<CornerRadius> DialogCornerRadiusProperty =
        AvaloniaProperty.Register<Dialog, CornerRadius>(nameof(DialogCornerRadius), new CornerRadius(8));

    /// <summary>Defines the <see cref="DialogBoxShadow"/> property.</summary>
    public static readonly StyledProperty<BoxShadows> DialogBoxShadowProperty =
        AvaloniaProperty.Register<Dialog, BoxShadows>(nameof(DialogBoxShadow), new BoxShadows(new BoxShadow
        {
            Blur = 32,
            OffsetY = 12,
            Color = Color.FromArgb(64, 0, 0, 0),
        }));

    private readonly CloseDialogCommand _closeCommand;
    private readonly List<Dialog> _nestedDialogs = [];
    private Border? _overlay;
    private Border? _surface;
    private ContentControl? _presenter;
    private Panel? _layerHost;
    private ScaleTransform? _scaleTransform;
    private IInputElement? _previousFocus;
    private DialogRequest? _activeRequest;
    private HostOptions? _hostOptions;
    private IDialogHostService? _attachedService;
    private string? _attachedHostId;
    private bool _internalChange;
    private bool _isPresented;
    private int _transitionVersion;

    static Dialog()
    {
        IsOpenProperty.Changed.AddClassHandler<Dialog>((dialog, args) => dialog.OnIsOpenChanged(args));
        ServiceProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.UpdateServiceRegistration());
        HostIdProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.UpdateServiceRegistration());
        IsAnimationEnabledProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.ConfigureTransitions());
        AnimationDurationProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.ConfigureTransitions());
        DialogEasingProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.ConfigureTransitions());
        IsOverlayVisibleProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.ApplyVisualState(dialog.IsOpen));
        IsLightDismissEnabledProperty.Changed.AddClassHandler<Dialog>((dialog, _) => dialog.ApplyVisualState(dialog.IsOpen));
    }

    /// <summary>Gets or sets the content displayed in the dialog.</summary>
    public object? DialogContent { get => GetValue(DialogContentProperty); set => SetValue(DialogContentProperty, value); }

    /// <summary>Gets or sets the template used to render <see cref="DialogContent"/>.</summary>
    public IDataTemplate? DialogContentTemplate { get => GetValue(DialogContentTemplateProperty); set => SetValue(DialogContentTemplateProperty, value); }

    /// <summary>Gets or sets whether the dialog is open.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Gets or sets the dialog width, or NaN for automatic sizing.</summary>
    public double DialogWidth { get => GetValue(DialogWidthProperty); set => SetValue(DialogWidthProperty, value); }

    /// <summary>Gets or sets the dialog height, or NaN for automatic sizing.</summary>
    public double DialogHeight { get => GetValue(DialogHeightProperty); set => SetValue(DialogHeightProperty, value); }

    /// <summary>Gets or sets the minimum dialog width.</summary>
    public double MinDialogWidth { get => GetValue(MinDialogWidthProperty); set => SetValue(MinDialogWidthProperty, value); }

    /// <summary>Gets or sets the maximum dialog width.</summary>
    public double MaxDialogWidth { get => GetValue(MaxDialogWidthProperty); set => SetValue(MaxDialogWidthProperty, value); }

    /// <summary>Gets or sets the minimum dialog height.</summary>
    public double MinDialogHeight { get => GetValue(MinDialogHeightProperty); set => SetValue(MinDialogHeightProperty, value); }

    /// <summary>Gets or sets the maximum dialog height.</summary>
    public double MaxDialogHeight { get => GetValue(MaxDialogHeightProperty); set => SetValue(MaxDialogHeightProperty, value); }

    /// <summary>Gets or sets whether interacting with the overlay closes the dialog.</summary>
    public bool IsLightDismissEnabled { get => GetValue(IsLightDismissEnabledProperty); set => SetValue(IsLightDismissEnabledProperty, value); }

    /// <summary>Gets or sets whether Escape closes the dialog.</summary>
    public bool IsEscapeKeyEnabled { get => GetValue(IsEscapeKeyEnabledProperty); set => SetValue(IsEscapeKeyEnabledProperty, value); }

    /// <summary>Gets or sets whether the modal overlay is visible.</summary>
    public bool IsOverlayVisible { get => GetValue(IsOverlayVisibleProperty); set => SetValue(IsOverlayVisibleProperty, value); }

    /// <summary>Gets or sets the brush used to dim primary content.</summary>
    public IBrush? OverlayBrush { get => GetValue(OverlayBrushProperty); set => SetValue(OverlayBrushProperty, value); }

    /// <summary>Gets or sets the service whose requests this dialog hosts.</summary>
    public IDialogHostService? Service { get => GetValue(ServiceProperty); set => SetValue(ServiceProperty, value); }

    /// <summary>Gets or sets the identifier used to route service requests.</summary>
    public string HostId { get => GetValue(HostIdProperty); set => SetValue(HostIdProperty, value); }

    /// <summary>Gets or sets whether transitions are enabled.</summary>
    public bool IsAnimationEnabled { get => GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

    /// <summary>Gets or sets the open and close transition duration.</summary>
    public TimeSpan AnimationDuration { get => GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

    /// <summary>Gets or sets the easing function used by dialog transitions.</summary>
    public Easing DialogEasing { get => GetValue(DialogEasingProperty); set => SetValue(DialogEasingProperty, value); }

    /// <summary>Gets or sets the dialog corner radius.</summary>
    public CornerRadius DialogCornerRadius { get => GetValue(DialogCornerRadiusProperty); set => SetValue(DialogCornerRadiusProperty, value); }

    /// <summary>Gets or sets the dialog shadow.</summary>
    public BoxShadows DialogBoxShadow { get => GetValue(DialogBoxShadowProperty); set => SetValue(DialogBoxShadowProperty, value); }

    /// <summary>Gets a command that closes the dialog and uses its parameter as the result.</summary>
    public ICommand CloseCommand { get; }

    /// <summary>Occurs before the dialog opens.</summary>
    public event EventHandler<DialogOpeningEventArgs>? Opening;

    /// <summary>Occurs after the dialog enters the open state.</summary>
    public event EventHandler? Opened;

    /// <summary>Occurs before the dialog closes and allows the operation to be canceled.</summary>
    public event EventHandler<DialogClosingEventArgs>? Closing;

    /// <summary>Occurs after the dialog closes.</summary>
    public event EventHandler<DialogClosedEventArgs>? Closed;

    /// <summary>Initializes a new dialog host.</summary>
    public Dialog()
    {
        _closeCommand = new CloseDialogCommand(this);
        CloseCommand = _closeCommand;
    }

    /// <summary>Attempts to close the dialog with an optional result.</summary>
    public bool TryClose(DialogCloseReason reason = DialogCloseReason.Programmatic, object? result = null)
    {
        if (_nestedDialogs.Count > 0)
        {
            return _nestedDialogs[^1].TryClose(reason, result);
        }

        if (!IsOpen)
        {
            return false;
        }

        var args = new DialogClosingEventArgs(reason, result);
        Closing?.Invoke(this, args);
        if (args.Cancel && reason != DialogCloseReason.Canceled)
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
        _surface = e.NameScope.Find<Border>(SurfacePartName);
        _presenter = e.NameScope.Find<ContentControl>(PresenterPartName);
        _layerHost = e.NameScope.Find<Panel>(LayerHostPartName);
        if (_overlay is not null)
        {
            _overlay.PointerReleased += OnOverlayPointerReleased;
        }

        _scaleTransform = new ScaleTransform(0.96, 0.96);
        if (_surface is not null)
        {
            _surface.RenderTransform = _scaleTransform;
            _surface.RenderTransformOrigin = RelativePoint.Center;
        }

        ConfigureTransitions();
        ApplyVisualState(IsOpen);
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
        ClearNestedDialogs();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Escape && IsOpen && IsEscapeKeyEnabled)
        {
            e.Handled = TryClose(DialogCloseReason.EscapeKey);
        }
    }

    internal void Present(DialogRequest request, object? content)
    {
        if (request.PresentationMode == DialogPresentationMode.Stack && IsOpen && _layerHost is not null)
        {
            if (_nestedDialogs.Count > 0 && ReferenceEquals(_nestedDialogs[^1]._activeRequest, request))
            {
                _nestedDialogs[^1].FocusDialogContent();
                return;
            }

            PresentNested(request, content);
            return;
        }

        if (!IsOpen)
        {
            _hostOptions = new HostOptions(
                DialogWidth,
                DialogHeight,
                IsLightDismissEnabled,
                IsEscapeKeyEnabled,
                IsOverlayVisible,
                IsAnimationEnabled,
                AnimationDuration);
        }
        _activeRequest = request;
        DialogContent = content;

        if (request.Width is { } width)
        {
            DialogWidth = width;
        }

        if (request.Height is { } height)
        {
            DialogHeight = height;
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

        if (request.AnimationDuration is { } animationDuration)
        {
            AnimationDuration = animationDuration;
        }

        if (!IsOpen)
        {
            IsOpen = true;
        }
    }

    private void PresentNested(DialogRequest request, object? content)
    {
        var nested = new Dialog
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Background = Background,
            BorderBrush = BorderBrush,
            BorderThickness = BorderThickness,
            Padding = Padding,
            OverlayBrush = OverlayBrush,
            DialogContentTemplate = DialogContentTemplate,
            DialogCornerRadius = DialogCornerRadius,
            DialogBoxShadow = DialogBoxShadow,
            IsAnimationEnabled = IsAnimationEnabled,
            AnimationDuration = AnimationDuration,
            DialogEasing = DialogEasing,
        };
        nested.Closed += OnNestedDialogClosed;
        _nestedDialogs.Add(nested);
        _layerHost!.Children.Add(nested);
        nested.Present(request, content);
    }

    private void OnNestedDialogClosed(object? sender, DialogClosedEventArgs e)
    {
        if (sender is not Dialog nested)
        {
            return;
        }

        nested.Closed -= OnNestedDialogClosed;
        _nestedDialogs.Remove(nested);
        _layerHost?.Children.Remove(nested);
        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Complete(this, _attachedHostId, e.Result, e.Reason);
        }
    }

    private void ClearNestedDialogs()
    {
        foreach (Dialog nested in _nestedDialogs)
        {
            nested.Closed -= OnNestedDialogClosed;
        }

        _nestedDialogs.Clear();
        _layerHost?.Children.Clear();
    }

    void IDialogHost.Present(DialogRequest request, object? content) => Present(request, content);

    private static bool IsValidSize(double value) => double.IsNaN(value) || value >= 0;

    private void OnIsOpenChanged(AvaloniaPropertyChangedEventArgs args)
    {
        var isOpen = args.GetNewValue<bool>();
        PseudoClasses.Set(":open", isOpen);
        _closeCommand.RaiseCanExecuteChanged();
        if (_internalChange)
        {
            return;
        }

        if (isOpen)
        {
            _previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            Opening?.Invoke(this, new DialogOpeningEventArgs(_activeRequest));
            BeginOpen();
        }
        else
        {
            var closing = new DialogClosingEventArgs(DialogCloseReason.Programmatic, null);
            Closing?.Invoke(this, closing);
            if (closing.Cancel)
            {
                _internalChange = true;
                SetCurrentValue(IsOpenProperty, true);
                _internalChange = false;
                return;
            }

            BeginClose(DialogCloseReason.Programmatic, null);
        }
    }

    private void BeginOpen()
    {
        _transitionVersion++;
        var version = _transitionVersion;
        ClearTransitions();
        _isPresented = true;
        ApplyVisualState(false);
        ConfigureTransitions();
        DispatcherTimer.RunOnce(() =>
        {
            if (!IsOpen || version != _transitionVersion)
            {
                return;
            }

            ApplyVisualState(true);
            FocusDialogContent();
            DispatcherTimer.RunOnce(() =>
            {
                if (version == _transitionVersion && IsOpen)
                {
                    Opened?.Invoke(this, EventArgs.Empty);
                }
            }, IsAnimationEnabled ? AnimationDuration : TimeSpan.Zero, DispatcherPriority.Render);
        }, TimeSpan.Zero, DispatcherPriority.Render);
    }

    private void BeginClose(DialogCloseReason reason, object? result)
    {
        _transitionVersion++;
        var version = _transitionVersion;
        ApplyVisualState(false);
        var duration = _isPresented && _surface is not null && IsAnimationEnabled
            ? AnimationDuration
            : TimeSpan.Zero;
        if (duration <= TimeSpan.Zero)
        {
            FinishClose(version, reason, result);
            return;
        }

        DispatcherTimer.RunOnce(
            () => FinishClose(version, reason, result),
            duration,
            DispatcherPriority.Render);
    }

    private void FinishClose(int version, DialogCloseReason reason, object? result)
    {
        if (version != _transitionVersion)
        {
            return;
        }

        _isPresented = false;
        ApplyVisualState(false);
        Closed?.Invoke(this, new DialogClosedEventArgs(reason, result));
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

    private void FocusDialogContent()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var focusTarget = _presenter?
                .GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(control => control.Focusable && control.IsEffectivelyEnabled && control.IsVisible);
            (focusTarget ?? _presenter)?.Focus();
        }, DispatcherPriority.Input);
    }

    private void OnOverlayPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (IsOpen && IsLightDismissEnabled)
        {
            e.Handled = TryClose(DialogCloseReason.LightDismiss);
        }
    }

    private void ApplyVisualState(bool open)
    {
        if (_overlay is not null)
        {
            _overlay.IsVisible = _isPresented;
            _overlay.IsHitTestVisible = _isPresented;
            _overlay.Opacity = open && IsOverlayVisible ? 1 : 0;
        }

        if (_surface is not null)
        {
            _surface.IsVisible = _isPresented;
            _surface.IsHitTestVisible = open;
            _surface.Opacity = open ? 1 : 0;
        }

        if (_scaleTransform is not null)
        {
            var scale = open ? 1 : 0.96;
            _scaleTransform.ScaleX = scale;
            _scaleTransform.ScaleY = scale;
        }
    }

    private void ConfigureTransitions()
    {
        if (_overlay is null || _surface is null || _scaleTransform is null)
        {
            return;
        }

        var duration = IsAnimationEnabled ? AnimationDuration : TimeSpan.Zero;
        _overlay.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = DialogEasing,
            },
        };
        _surface.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = DialogEasing,
            },
        };
        _scaleTransform.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = ScaleTransform.ScaleXProperty,
                Duration = duration,
                Easing = DialogEasing,
            },
            new DoubleTransition
            {
                Property = ScaleTransform.ScaleYProperty,
                Duration = duration,
                Easing = DialogEasing,
            },
        };
    }

    private void ClearTransitions()
    {
        if (_overlay is not null)
        {
            _overlay.Transitions = null;
        }

        if (_surface is not null)
        {
            _surface.Transitions = null;
        }

        if (_scaleTransform is not null)
        {
            _scaleTransform.Transitions = null;
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

        DialogWidth = options.Width;
        DialogHeight = options.Height;
        IsLightDismissEnabled = options.IsLightDismissEnabled;
        IsEscapeKeyEnabled = options.IsEscapeKeyEnabled;
        IsOverlayVisible = options.IsOverlayVisible;
        IsAnimationEnabled = options.IsAnimationEnabled;
        AnimationDuration = options.AnimationDuration;
        _hostOptions = null;
    }

    private sealed class CloseDialogCommand(Dialog owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => owner.IsOpen;

        public void Execute(object? parameter) => owner.TryClose(DialogCloseReason.Command, parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record HostOptions(
        double Width,
        double Height,
        bool IsLightDismissEnabled,
        bool IsEscapeKeyEnabled,
        bool IsOverlayVisible,
        bool IsAnimationEnabled,
        TimeSpan AnimationDuration);
}
