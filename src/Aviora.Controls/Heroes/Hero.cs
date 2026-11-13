using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;

namespace Aviora.Controls;

/// <summary>
/// Marks a content element for shared-element (hero) transitions within a <see cref="HeroHost"/>.
/// </summary>
/// <remarks>
/// Place two <see cref="Hero"/> controls with matching <see cref="Control.Tag"/> values inside the same
/// <see cref="HeroHost"/>. When one departs or becomes hidden and the other appears, the host animates
/// a visual shuttle smoothly between their bounds.
/// </remarks>
public class Hero : ContentControl
{
    /// <summary>Defines the <see cref="IsHeroEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsHeroEnabledProperty =
        AvaloniaProperty.Register<Hero, bool>(nameof(IsHeroEnabled), true);

    /// <summary>Defines the <see cref="Duration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan?> DurationProperty =
        AvaloniaProperty.Register<Hero, TimeSpan?>(
            nameof(Duration),
            validate: value => value is null || value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="Easing"/> property.</summary>
    public static readonly StyledProperty<Easing?> EasingProperty =
        AvaloniaProperty.Register<Hero, Easing?>(nameof(Easing));

    /// <summary>Defines the <see cref="FlightMode"/> property.</summary>
    public static readonly StyledProperty<HeroFlightMode?> FlightModeProperty =
        AvaloniaProperty.Register<Hero, HeroFlightMode?>(nameof(FlightMode));

    /// <summary>Defines the <see cref="ShuttleTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ShuttleTemplateProperty =
        AvaloniaProperty.Register<Hero, IDataTemplate?>(nameof(ShuttleTemplate));

    /// <summary>Defines the <see cref="IsPlaceholderHiddenDuringFlight"/> property.</summary>
    public static readonly StyledProperty<bool> IsPlaceholderHiddenDuringFlightProperty =
        AvaloniaProperty.Register<Hero, bool>(nameof(IsPlaceholderHiddenDuringFlight), true);

    private HeroHost? _host;
    private Rect? _lastBoundsInHost;
    private double _lastRenderScaling = 1.0;
    private bool _isRegisteredAsVisible;
    private object? _registeredTag;
    private int _flightHideCount;
    private double _opacityBeforeFlight = 1.0;
    private bool _hitTestBeforeFlight = true;
    private bool _isHiddenByClosingOverlay;

    static Hero()
    {
        TagProperty.Changed.AddClassHandler<Hero>((hero, e) => hero.OnTagChanged(e.OldValue, e.NewValue));
        IsHeroEnabledProperty.Changed.AddClassHandler<Hero>((hero, _) => hero.SyncVisibilityState());
        IsVisibleProperty.Changed.AddClassHandler<Hero>((hero, _) => hero.SyncVisibilityState());
    }

    /// <summary>Gets or sets whether this control participates in hero transitions.</summary>
    public bool IsHeroEnabled
    {
        get => GetValue(IsHeroEnabledProperty);
        set => SetValue(IsHeroEnabledProperty, value);
    }

    /// <summary>Gets or sets an optional flight duration override for this hero.</summary>
    public TimeSpan? Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Gets or sets an optional easing override for this hero.</summary>
    public Easing? Easing
    {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    /// <summary>Gets or sets an optional flight mode override for this hero.</summary>
    public HeroFlightMode? FlightMode
    {
        get => GetValue(FlightModeProperty);
        set => SetValue(FlightModeProperty, value);
    }

    /// <summary>Gets or sets an optional template used to render the flying shuttle.</summary>
    public IDataTemplate? ShuttleTemplate
    {
        get => GetValue(ShuttleTemplateProperty);
        set => SetValue(ShuttleTemplateProperty, value);
    }

    /// <summary>Gets or sets whether this hero is hidden while a matching shuttle is in flight.</summary>
    public bool IsPlaceholderHiddenDuringFlight
    {
        get => GetValue(IsPlaceholderHiddenDuringFlightProperty);
        set => SetValue(IsPlaceholderHiddenDuringFlightProperty, value);
    }

    internal Rect? LastBoundsInHost => _lastBoundsInHost;

    internal double LastRenderScaling => _lastRenderScaling;

    internal bool TryGetCurrentBoundsInHost(HeroHost host, out Rect bounds)
    {
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            Matrix? transform = this.TransformToVisual(host);
            if (transform.HasValue)
            {
                Point topLeft = transform.Value.Transform(new Point(0, 0));
                Point bottomRight = transform.Value.Transform(new Point(Bounds.Width, Bounds.Height));
                double left = Math.Min(topLeft.X, bottomRight.X);
                double top = Math.Min(topLeft.Y, bottomRight.Y);
                double width = Math.Abs(bottomRight.X - topLeft.X);
                double height = Math.Abs(bottomRight.Y - topLeft.Y);
                if (width > 0 && height > 0)
                {
                    bounds = new Rect(left, top, width, height);
                    _lastBoundsInHost = bounds;
                    _lastRenderScaling = TopLevel.GetTopLevel(host)?.RenderScaling ?? _lastRenderScaling;
                    return true;
                }
            }
        }

        bounds = default;
        return false;
    }

    internal void BeginFlightPlaceholderHide()
    {
        if (!IsPlaceholderHiddenDuringFlight)
        {
            return;
        }

        if (_flightHideCount == 0)
        {
            _opacityBeforeFlight = Opacity;
            _hitTestBeforeFlight = IsHitTestVisible;
            Opacity = 0;
            IsHitTestVisible = false;
        }

        _flightHideCount++;
    }

    internal void EndFlightPlaceholderHide()
    {
        if (_flightHideCount <= 0)
        {
            return;
        }

        _flightHideCount--;
        if (_flightHideCount == 0)
        {
            Opacity = _opacityBeforeFlight;
            IsHitTestVisible = _hitTestBeforeFlight;
        }
    }

    internal void BeginOverlayCloseDeparture()
    {
        if (_isHiddenByClosingOverlay)
        {
            return;
        }

        _isHiddenByClosingOverlay = true;

        if (_host is not null && _isRegisteredAsVisible && _registeredTag is not null)
        {
            TryGetCurrentBoundsInHost(_host, out _);
            _host.NotifyHeroDeparting(this, _registeredTag, _lastBoundsInHost, _lastRenderScaling);
            _host.UnregisterHero(this);
            _isRegisteredAsVisible = false;
            _registeredTag = null;
        }

        BeginFlightPlaceholderHide();
    }

    internal void EndOverlayCloseDeparture()
    {
        if (!_isHiddenByClosingOverlay)
        {
            return;
        }

        _isHiddenByClosingOverlay = false;
        EndFlightPlaceholderHide();
        SyncVisibilityState();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _host = this.FindAncestorOfType<HeroHost>();
        LayoutUpdated += OnHostLayoutUpdated;

        if (_host is not null)
        {
            _lastRenderScaling = TopLevel.GetTopLevel(_host)?.RenderScaling ?? 1.0;
        }

        SyncVisibilityState();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= OnHostLayoutUpdated;

        if (_host is not null && _isRegisteredAsVisible && _registeredTag is not null)
        {
            _host.NotifyHeroDeparting(this, _registeredTag, _lastBoundsInHost, _lastRenderScaling);
            _host.UnregisterHero(this);
        }

        _isRegisteredAsVisible = false;
        _registeredTag = null;
        _isHiddenByClosingOverlay = false;
        _host = null;

        if (_flightHideCount > 0)
        {
            _flightHideCount = 0;
            Opacity = _opacityBeforeFlight;
            IsHitTestVisible = _hitTestBeforeFlight;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnHostLayoutUpdated(object? sender, EventArgs e)
    {
        if (_host is null)
        {
            return;
        }

        if (IsEffectivelyVisible)
        {
            TryGetCurrentBoundsInHost(_host, out _);
        }

        SyncVisibilityState();
    }

    private void OnTagChanged(object? oldValue, object? newValue)
    {
        if (_host is null)
        {
            return;
        }

        if (_isRegisteredAsVisible && oldValue is not null)
        {
            _host.NotifyHeroDeparting(this, oldValue, _lastBoundsInHost, _lastRenderScaling);
            _host.UnregisterHero(this);
            _isRegisteredAsVisible = false;
            _registeredTag = null;
        }

        SyncVisibilityState();
    }

    private void SyncVisibilityState()
    {
        if (_host is null)
        {
            return;
        }

        object? tag = Tag;
        bool shouldBeActive = !_isHiddenByClosingOverlay && IsHeroEnabled && IsEffectivelyVisible && tag is not null;

        if (shouldBeActive && !_isRegisteredAsVisible)
        {
            _isRegisteredAsVisible = true;
            _registeredTag = tag;
            TryGetCurrentBoundsInHost(_host, out _);
            _host.NotifyHeroArrived(this, tag!);
        }
        else if (!shouldBeActive && _isRegisteredAsVisible)
        {
            object? departingTag = _registeredTag ?? tag;
            _isRegisteredAsVisible = false;
            _registeredTag = null;
            if (departingTag is not null)
            {
                _host.NotifyHeroDeparting(this, departingTag, _lastBoundsInHost, _lastRenderScaling);
            }

            _host.UnregisterHero(this);
        }
    }
}
