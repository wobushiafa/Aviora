using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Aviora.Presentation.Loadings;

namespace Aviora.Controls;

/// <summary>Hosts page content and presents a service-controlled loading overlay above it.</summary>
public class LoadingOverlay : ContentControl, ILoadingHost
{
    /// <summary>The default identifier used to match service requests to a host.</summary>
    public const string DefaultHostId = LoadingHost.DefaultId;

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<LoadingOverlay, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="LoadingContent"/> property.</summary>
    public static readonly StyledProperty<object?> LoadingContentProperty =
        AvaloniaProperty.Register<LoadingOverlay, object?>(nameof(LoadingContent));

    /// <summary>Defines the <see cref="LoadingContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> LoadingContentTemplateProperty =
        AvaloniaProperty.Register<LoadingOverlay, IDataTemplate?>(nameof(LoadingContentTemplate));

    /// <summary>Defines the <see cref="Service"/> property.</summary>
    public static readonly StyledProperty<ILoadingHostService?> ServiceProperty =
        AvaloniaProperty.Register<LoadingOverlay, ILoadingHostService?>(nameof(Service));

    /// <summary>Defines the <see cref="HostId"/> property.</summary>
    public static readonly StyledProperty<string> HostIdProperty =
        AvaloniaProperty.Register<LoadingOverlay, string>(
            nameof(HostId),
            DefaultHostId,
            validate: value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Defines the <see cref="OverlayBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<LoadingOverlay, IBrush?>(
            nameof(OverlayBrush),
            new ImmutableSolidColorBrush(Color.FromArgb(112, 15, 23, 42)));

    /// <summary>Defines the <see cref="IndicatorStyle"/> property.</summary>
    public static readonly StyledProperty<LoadingIndicatorStyle> IndicatorStyleProperty =
        AvaloniaProperty.Register<LoadingOverlay, LoadingIndicatorStyle>(nameof(IndicatorStyle));

    /// <summary>Defines the <see cref="IndicatorBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty =
        AvaloniaProperty.Register<LoadingOverlay, IBrush?>(nameof(IndicatorBrush), Brushes.White);

    /// <summary>Defines the <see cref="TrackBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<LoadingOverlay, IBrush?>(nameof(TrackBrush), Brushes.White);

    /// <summary>Defines the <see cref="IndicatorSize"/> property.</summary>
    public static readonly StyledProperty<double> IndicatorSizeProperty =
        AvaloniaProperty.Register<LoadingOverlay, double>(nameof(IndicatorSize), 48, validate: value => value > 0);

    /// <summary>Defines the <see cref="StrokeThickness"/> property.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<LoadingOverlay, double>(nameof(StrokeThickness), 4, validate: value => value > 0);

    /// <summary>Defines the <see cref="IndicatorAnimationDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> IndicatorAnimationDurationProperty =
        AvaloniaProperty.Register<LoadingOverlay, TimeSpan>(
            nameof(IndicatorAnimationDuration),
            TimeSpan.FromMilliseconds(900),
            validate: value => value > TimeSpan.Zero);

    /// <summary>Provides compatibility for the former <c>AnimationDuration</c> styled property.</summary>
    [Obsolete("Use IndicatorAnimationDurationProperty instead.")]
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty = IndicatorAnimationDurationProperty;

    /// <summary>Defines the <see cref="ShowDelay"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> ShowDelayProperty =
        AvaloniaProperty.Register<LoadingOverlay, TimeSpan>(
            nameof(ShowDelay),
            TimeSpan.Zero,
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="MinimumShowDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> MinimumShowDurationProperty =
        AvaloniaProperty.Register<LoadingOverlay, TimeSpan>(
            nameof(MinimumShowDuration),
            TimeSpan.Zero,
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="CloseDelay"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> CloseDelayProperty =
        AvaloniaProperty.Register<LoadingOverlay, TimeSpan>(
            nameof(CloseDelay),
            TimeSpan.Zero,
            validate: value => value >= TimeSpan.Zero);

    private ILoadingHostService? _attachedService;
    private string? _attachedHostId;
    private bool _hasPresentations;
    private long _openedTimestamp;
    private int _transitionVersion;

    static LoadingOverlay()
    {
        ServiceProperty.Changed.AddClassHandler<LoadingOverlay>((overlay, _) => overlay.UpdateServiceRegistration());
        HostIdProperty.Changed.AddClassHandler<LoadingOverlay>((overlay, _) => overlay.UpdateServiceRegistration());
        IsOpenProperty.Changed.AddClassHandler<LoadingOverlay>((overlay, args) => overlay.OnIsOpenChanged(args));
    }

    /// <summary>Gets or sets whether the overlay is visible.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Gets or sets the optional content or ViewModel displayed below the indicator.</summary>
    public object? LoadingContent { get => GetValue(LoadingContentProperty); set => SetValue(LoadingContentProperty, value); }

    /// <summary>Gets or sets the template used to render <see cref="LoadingContent"/>.</summary>
    public IDataTemplate? LoadingContentTemplate
    {
        get => GetValue(LoadingContentTemplateProperty);
        set => SetValue(LoadingContentTemplateProperty, value);
    }

    /// <summary>Gets or sets the service whose requests this overlay hosts.</summary>
    public ILoadingHostService? Service { get => GetValue(ServiceProperty); set => SetValue(ServiceProperty, value); }

    /// <summary>Gets or sets the identifier used to route loading requests.</summary>
    public string HostId { get => GetValue(HostIdProperty); set => SetValue(HostIdProperty, value); }

    /// <summary>Gets or sets the brush that blocks and dims the hosted page.</summary>
    public IBrush? OverlayBrush { get => GetValue(OverlayBrushProperty); set => SetValue(OverlayBrushProperty, value); }

    /// <summary>Gets or sets the built-in loading indicator visual.</summary>
    public LoadingIndicatorStyle IndicatorStyle
    {
        get => GetValue(IndicatorStyleProperty);
        set => SetValue(IndicatorStyleProperty, value);
    }

    /// <summary>Gets or sets the loading indicator brush.</summary>
    public IBrush? IndicatorBrush { get => GetValue(IndicatorBrushProperty); set => SetValue(IndicatorBrushProperty, value); }

    /// <summary>Gets or sets the loading indicator track brush.</summary>
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    /// <summary>Gets or sets the square size of the loading indicator.</summary>
    public double IndicatorSize { get => GetValue(IndicatorSizeProperty); set => SetValue(IndicatorSizeProperty, value); }

    /// <summary>Gets or sets the stroke thickness of the loading indicator.</summary>
    public double StrokeThickness { get => GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    /// <summary>Gets or sets the time required for one indicator animation cycle.</summary>
    public TimeSpan IndicatorAnimationDuration
    {
        get => GetValue(IndicatorAnimationDurationProperty);
        set => SetValue(IndicatorAnimationDurationProperty, value);
    }

    /// <summary>Gets or sets the time required for one indicator animation cycle.</summary>
    [Obsolete("Use IndicatorAnimationDuration instead.")]
    public TimeSpan AnimationDuration
    {
        get => IndicatorAnimationDuration;
        set => IndicatorAnimationDuration = value;
    }

    /// <summary>Gets or sets how long a service request must remain active before the overlay opens.</summary>
    public TimeSpan ShowDelay { get => GetValue(ShowDelayProperty); set => SetValue(ShowDelayProperty, value); }

    /// <summary>Gets or sets the minimum time an opened overlay remains visible.</summary>
    public TimeSpan MinimumShowDuration
    {
        get => GetValue(MinimumShowDurationProperty);
        set => SetValue(MinimumShowDurationProperty, value);
    }

    /// <summary>Gets or sets how long the overlay remains visible after the final presentation closes.</summary>
    public TimeSpan CloseDelay { get => GetValue(CloseDelayProperty); set => SetValue(CloseDelayProperty, value); }

    /// <inheritdoc />
    public void Synchronize(IReadOnlyList<LoadingPresentation> presentations)
    {
        ArgumentNullException.ThrowIfNull(presentations);

        int version = ++_transitionVersion;
        _hasPresentations = presentations.Count > 0;
        if (_hasPresentations)
        {
            SetCurrentValue(LoadingContentProperty, presentations[^1].Request.Content);
            if (IsOpen)
            {
                return;
            }

            if (ShowDelay == TimeSpan.Zero)
            {
                Open(version);
            }
            else
            {
                DispatcherTimer.RunOnce(() => Open(version), ShowDelay, DispatcherPriority.Normal);
            }

            return;
        }

        if (!IsOpen)
        {
            SetCurrentValue(LoadingContentProperty, null);
            return;
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(_openedTimestamp);
        TimeSpan minimumDurationRemaining = MinimumShowDuration - elapsed;
        TimeSpan remaining = CloseDelay > minimumDurationRemaining ? CloseDelay : minimumDurationRemaining;
        if (remaining <= TimeSpan.Zero)
        {
            Close(version);
        }
        else
        {
            DispatcherTimer.RunOnce(() => Close(version), remaining, DispatcherPriority.Normal);
        }
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
        _hasPresentations = false;
        _transitionVersion++;
        SetCurrentValue(IsOpenProperty, false);
        SetCurrentValue(LoadingContentProperty, null);
        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateServiceRegistration()
    {
        if (VisualRoot is null)
        {
            return;
        }

        ILoadingHostService? service = Service;
        string hostId = HostId;
        if (ReferenceEquals(service, _attachedService) && string.Equals(hostId, _attachedHostId, StringComparison.Ordinal))
        {
            return;
        }

        if (_attachedService is not null && _attachedHostId is not null)
        {
            _attachedService.Detach(this, _attachedHostId);
        }

        _attachedService = service;
        _attachedHostId = service is null ? null : hostId;
        service?.Attach(this, hostId);
    }

    private void Open(int version)
    {
        if (version != _transitionVersion || !_hasPresentations)
        {
            return;
        }

        SetCurrentValue(IsOpenProperty, true);
    }

    private void Close(int version)
    {
        if (version != _transitionVersion || _hasPresentations)
        {
            return;
        }

        SetCurrentValue(IsOpenProperty, false);
        SetCurrentValue(LoadingContentProperty, null);
    }

    private void OnIsOpenChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.GetNewValue<bool>())
        {
            _openedTimestamp = Stopwatch.GetTimestamp();
        }
    }
}
