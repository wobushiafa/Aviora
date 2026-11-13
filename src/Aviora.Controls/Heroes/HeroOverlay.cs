using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Aviora.Controls;

/// <summary>
/// Presents modal or fullscreen content over a page with smooth enter/exit transitions
/// coordinated with <see cref="HeroHost"/>.
/// </summary>
[TemplatePart(BackdropPartName, typeof(Border))]
[TemplatePart(PresenterPartName, typeof(ContentControl))]
public class HeroOverlay : ContentControl
{
    internal const string BackdropPartName = "PART_Backdrop";
    internal const string PresenterPartName = "PART_OverlayPresenter";

    /// <summary>Defines the <see cref="IsOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<HeroOverlay, bool>(
            nameof(IsOpen),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="BackdropBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackdropBrushProperty =
        AvaloniaProperty.Register<HeroOverlay, IBrush?>(
            nameof(BackdropBrush),
            new ImmutableSolidColorBrush(Color.FromArgb(165, 8, 12, 20)));

    /// <summary>Defines the <see cref="IsAnimationEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<HeroOverlay, bool>(nameof(IsAnimationEnabled), true);

    /// <summary>Defines the <see cref="TransitionDuration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> TransitionDurationProperty =
        AvaloniaProperty.Register<HeroOverlay, TimeSpan>(
            nameof(TransitionDuration),
            TimeSpan.FromMilliseconds(320),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="TransitionEasing"/> property.</summary>
    public static readonly StyledProperty<Easing?> TransitionEasingProperty =
        AvaloniaProperty.Register<HeroOverlay, Easing?>(nameof(TransitionEasing), new CubicEaseInOut());

    /// <summary>Defines the <see cref="IsLightDismissEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
        AvaloniaProperty.Register<HeroOverlay, bool>(nameof(IsLightDismissEnabled), true);

    /// <summary>Defines the <see cref="IsEscapeKeyEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsEscapeKeyEnabledProperty =
        AvaloniaProperty.Register<HeroOverlay, bool>(nameof(IsEscapeKeyEnabled), true);

    private Border? _backdrop;
    private ContentControl? _presenter;
    private bool _isAnimating;
    private bool _animatingOpen;
    private bool _frameRequested;
    private long _startTimestamp;
    private double _fromProgress;
    private double _currentProgress;

    static HeroOverlay()
    {
        IsOpenProperty.Changed.AddClassHandler<HeroOverlay>((overlay, _) => overlay.OnIsOpenChanged());
    }

    /// <summary>Gets or sets whether the overlay is currently open.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the brush painted behind the overlay content.</summary>
    public IBrush? BackdropBrush
    {
        get => GetValue(BackdropBrushProperty);
        set => SetValue(BackdropBrushProperty, value);
    }

    /// <summary>Gets or sets whether enter and exit transitions are animated.</summary>
    public bool IsAnimationEnabled
    {
        get => GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>Gets or sets the duration of the overlay enter and exit transitions.</summary>
    public TimeSpan TransitionDuration
    {
        get => GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <summary>Gets or sets the easing function used for the overlay transition.</summary>
    public Easing? TransitionEasing
    {
        get => GetValue(TransitionEasingProperty);
        set => SetValue(TransitionEasingProperty, value);
    }

    /// <summary>Gets or sets whether clicking the backdrop closes the overlay.</summary>
    public bool IsLightDismissEnabled
    {
        get => GetValue(IsLightDismissEnabledProperty);
        set => SetValue(IsLightDismissEnabledProperty, value);
    }

    /// <summary>Gets or sets whether pressing Escape closes the overlay.</summary>
    public bool IsEscapeKeyEnabled
    {
        get => GetValue(IsEscapeKeyEnabledProperty);
        set => SetValue(IsEscapeKeyEnabledProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_backdrop is not null)
        {
            _backdrop.PointerPressed -= OnBackdropPointerPressed;
        }

        base.OnApplyTemplate(e);

        _backdrop = e.NameScope.Find<Border>(BackdropPartName);
        _presenter = e.NameScope.Find<ContentControl>(PresenterPartName);

        if (_backdrop is not null)
        {
            _backdrop.PointerPressed += OnBackdropPointerPressed;
        }

        SyncImmediateState(IsOpen);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && IsOpen && IsEscapeKeyEnabled && e.Key == Key.Escape)
        {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _frameRequested = false;
        _isAnimating = false;
        EndOverlayCloseForHeroes();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsOpen && IsLightDismissEnabled)
        {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
        }
    }

    private void OnIsOpenChanged()
    {
        bool open = IsOpen;
        bool canAnimate = IsAnimationEnabled &&
            TransitionDuration > TimeSpan.Zero &&
            _backdrop is not null &&
            _presenter is not null &&
            TopLevel.GetTopLevel(this) is not null;

        if (!canAnimate)
        {
            SyncImmediateState(open);
            return;
        }

        if (open)
        {
            EndOverlayCloseForHeroes();
            IsVisible = true;
            IsHitTestVisible = true;
        }
        else
        {
            IsHitTestVisible = false;
            BeginOverlayCloseForHeroes();
        }

        _animatingOpen = open;
        _fromProgress = _currentProgress;
        _startTimestamp = Stopwatch.GetTimestamp();
        _isAnimating = true;
        ApplyProgress(_currentProgress);
        RequestNextFrame();
    }

    private void SyncImmediateState(bool open)
    {
        _isAnimating = false;
        _currentProgress = open ? 1.0 : 0.0;
        EndOverlayCloseForHeroes();
        ApplyProgress(_currentProgress);
        IsVisible = open;
        IsHitTestVisible = open;
    }

    private void RequestNextFrame()
    {
        if (!_isAnimating || _frameRequested)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            SyncImmediateState(IsOpen);
            return;
        }

        _frameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan _)
    {
        _frameRequested = false;
        if (!_isAnimating)
        {
            return;
        }

        double elapsedMs = (Stopwatch.GetTimestamp() - _startTimestamp) * 1000.0 / Stopwatch.Frequency;
        double durationMs = Math.Max(1.0, TransitionDuration.TotalMilliseconds);
        double rawT = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
        Easing easing = TransitionEasing ?? new CubicEaseInOut();
        double easedT = Math.Clamp(easing.Ease(rawT), 0.0, 1.0);

        double target = _animatingOpen ? 1.0 : 0.0;
        _currentProgress = _fromProgress + ((target - _fromProgress) * easedT);
        ApplyProgress(_currentProgress);

        if (rawT >= 1.0)
        {
            _isAnimating = false;
            _currentProgress = target;
            ApplyProgress(target);

            if (!_animatingOpen)
            {
                IsVisible = false;
                EndOverlayCloseForHeroes();
            }

            return;
        }

        RequestNextFrame();
    }

    private void ApplyProgress(double progress)
    {
        if (_backdrop is not null)
        {
            _backdrop.Opacity = progress;
        }

        if (_presenter is not null)
        {
            _presenter.Opacity = progress;
        }
    }

    private void BeginOverlayCloseForHeroes()
    {
        if (_presenter is null)
        {
            return;
        }

        foreach (Hero hero in _presenter.GetVisualDescendants().OfType<Hero>())
        {
            hero.BeginOverlayCloseDeparture();
        }
    }

    private void EndOverlayCloseForHeroes()
    {
        if (_presenter is null)
        {
            return;
        }

        foreach (Hero hero in _presenter.GetVisualDescendants().OfType<Hero>())
        {
            hero.EndOverlayCloseDeparture();
        }
    }
}
