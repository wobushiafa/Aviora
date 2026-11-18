using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;

namespace Aviora.Controls;

/// <summary>
/// Hosts content and orchestrates shared-element (hero) transitions between matching <see cref="Hero"/> controls.
/// </summary>
[TemplatePart(OverlayLayerPartName, typeof(Canvas))]
public class HeroHost : ContentControl
{
    internal const string OverlayLayerPartName = "PART_HeroOverlayLayer";

    /// <summary>Defines the <see cref="IsAnimationEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<HeroHost, bool>(nameof(IsAnimationEnabled), true);

    /// <summary>Defines the <see cref="Duration"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<HeroHost, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(320),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="Easing"/> property.</summary>
    public static readonly StyledProperty<Easing?> EasingProperty =
        AvaloniaProperty.Register<HeroHost, Easing?>(nameof(Easing), new CubicEaseInOut());

    /// <summary>Defines the <see cref="FlightMode"/> property.</summary>
    public static readonly StyledProperty<HeroFlightMode> FlightModeProperty =
        AvaloniaProperty.Register<HeroHost, HeroFlightMode>(nameof(FlightMode), HeroFlightMode.Arc);

    /// <summary>Defines the <see cref="CaptureTimeout"/> property.</summary>
    public static readonly StyledProperty<TimeSpan> CaptureTimeoutProperty =
        AvaloniaProperty.Register<HeroHost, TimeSpan>(
            nameof(CaptureTimeout),
            TimeSpan.FromMilliseconds(180),
            validate: value => value >= TimeSpan.Zero);

    /// <summary>Defines the <see cref="DefaultShuttleTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> DefaultShuttleTemplateProperty =
        AvaloniaProperty.Register<HeroHost, IDataTemplate?>(nameof(DefaultShuttleTemplate));

    private readonly List<Hero> _activeHeroes = [];
    private readonly Dictionary<object, DepartureRecord> _departures = new(EqualityComparer<object>.Default);
    private readonly Dictionary<object, ActiveFlight> _activeFlights = new(EqualityComparer<object>.Default);
    private readonly List<PendingArrival> _pendingArrivals = [];

    private Canvas? _overlayLayer;
    private bool _arrivalDispatchScheduled;
    private bool _animationFrameRequested;

    /// <summary>Occurs when a hero shuttle flight begins.</summary>
    public event EventHandler<HeroFlightEventArgs>? HeroFlightStarted;

    /// <summary>Occurs when a hero shuttle flight finishes or is cancelled.</summary>
    public event EventHandler<HeroFlightEventArgs>? HeroFlightCompleted;

    /// <summary>Gets or sets whether hero transitions are animated.</summary>
    public bool IsAnimationEnabled
    {
        get => GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>Gets or sets the default flight duration.</summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Gets or sets the default easing function for flights.</summary>
    public Easing? Easing
    {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    /// <summary>Gets or sets the default flight interpolation mode.</summary>
    public HeroFlightMode FlightMode
    {
        get => GetValue(FlightModeProperty);
        set => SetValue(FlightModeProperty, value);
    }

    /// <summary>Gets or sets how long a departing hero remains eligible to match an arriving hero.</summary>
    public TimeSpan CaptureTimeout
    {
        get => GetValue(CaptureTimeoutProperty);
        set => SetValue(CaptureTimeoutProperty, value);
    }

    /// <summary>Gets or sets an optional fallback template for rendering flying shuttles.</summary>
    public IDataTemplate? DefaultShuttleTemplate
    {
        get => GetValue(DefaultShuttleTemplateProperty);
        set => SetValue(DefaultShuttleTemplateProperty, value);
    }

    /// <summary>Gets the number of hero shuttles currently in flight.</summary>
    public int ActiveFlightCount => _activeFlights.Count;

    /// <summary>
    /// Cancels all active hero flights and restores placeholder visibility.
    /// </summary>
    public void CancelAllFlights()
    {
        if (_activeFlights.Count == 0)
        {
            return;
        }

        List<ActiveFlight> flights = [.. _activeFlights.Values];
        foreach (ActiveFlight flight in flights)
        {
            CompleteFlight(flight);
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        CancelAllFlights();
        _overlayLayer = e.NameScope.Find<Canvas>(OverlayLayerPartName);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _animationFrameRequested = false;
        CancelAllFlights();
        ClearDepartures();
        _pendingArrivals.Clear();
        _activeHeroes.Clear();
        base.OnDetachedFromVisualTree(e);
    }

    internal void NotifyHeroArrived(Hero hero, object tag)
    {
        if (!_activeHeroes.Contains(hero))
        {
            _activeHeroes.Add(hero);
        }

        _pendingArrivals.Add(new PendingArrival(hero, tag, 0));
        ScheduleArrivalProcessing(DispatcherPriority.Render);
    }

    internal void NotifyHeroDeparting(Hero hero, object tag, Rect? lastBoundsInHost, double scaling)
    {
        // If this hero was the destination of an active flight, interrupt from mid-air bounds.
        if (_activeFlights.TryGetValue(tag, out ActiveFlight? inFlight) && ReferenceEquals(inFlight.DestinationHero, hero))
        {
            (Rect currentBounds, CornerRadius currentCornerRadius) = inFlight.EvaluateCurrentState(Stopwatch.GetTimestamp());
            RenderTargetBitmap? midAirSnapshot = CaptureSnapshot(inFlight.ShuttleBorder, currentBounds.Size, scaling);
            object? dataContext = hero.DataContext ?? hero.Content;
            IDataTemplate? shuttleTemplate = hero.ShuttleTemplate;

            CompleteFlight(inFlight);

            RecordDeparture(new DepartureRecord(
                tag,
                currentBounds,
                currentCornerRadius,
                scaling,
                null,
                dataContext,
                shuttleTemplate,
                midAirSnapshot));
            return;
        }

        if (lastBoundsInHost is not { Width: > 0, Height: > 0 } bounds)
        {
            return;
        }

        // If a settled visible peer with the same tag is already in the tree (e.g. closing an overlay),
        // fly directly back to that peer.
        Hero? settledPeer = FindVisiblePeer(hero, tag);
        if (settledPeer is not null && settledPeer.TryGetCurrentBoundsInHost(this, out Rect peerBounds))
        {
            RenderTargetBitmap? sourceSnapshot = CaptureSnapshot(hero, bounds.Size, scaling);
            DepartureRecord directDeparture = new(
                tag,
                bounds,
                hero.CornerRadius,
                scaling,
                hero,
                hero.DataContext ?? hero.Content,
                hero.ShuttleTemplate,
                sourceSnapshot);
            StartFlight(directDeparture, settledPeer, peerBounds);
            return;
        }

        RecordDeparture(new DepartureRecord(
            tag,
            bounds,
            hero.CornerRadius,
            scaling,
            hero,
            hero.DataContext ?? hero.Content,
            hero.ShuttleTemplate,
            null));
    }

    internal void UnregisterHero(Hero hero)
    {
        _activeHeroes.Remove(hero);
    }

    private void ScheduleArrivalProcessing(DispatcherPriority priority)
    {
        if (_arrivalDispatchScheduled)
        {
            return;
        }

        _arrivalDispatchScheduled = true;
        Dispatcher.UIThread.Post(ProcessPendingArrivals, priority);
    }

    private void ProcessPendingArrivals()
    {
        _arrivalDispatchScheduled = false;
        if (_pendingArrivals.Count == 0)
        {
            return;
        }

        List<PendingArrival> batch = [.. _pendingArrivals];
        _pendingArrivals.Clear();

        List<PendingArrival> retryLater = [];

        foreach (PendingArrival arrival in batch)
        {
            Hero hero = arrival.Hero;
            object tag = arrival.Tag;

            if (!hero.IsHeroEnabled || !hero.IsEffectivelyVisible || !Equals(hero.Tag, tag))
            {
                continue;
            }

            if (!hero.TryGetCurrentBoundsInHost(this, out Rect destBounds))
            {
                if (arrival.RetryCount < 2)
                {
                    retryLater.Add(arrival with { RetryCount = arrival.RetryCount + 1 });
                }

                continue;
            }

            if (_departures.Remove(tag, out DepartureRecord? departure))
            {
                departure.StopTimer();
                StartFlight(departure, hero, destBounds);
                continue;
            }

            Hero? peer = FindVisiblePeer(hero, tag);
            if (peer is not null && peer.TryGetCurrentBoundsInHost(this, out Rect peerBounds))
            {
                DepartureRecord peerDeparture = new(
                    tag,
                    peerBounds,
                    peer.CornerRadius,
                    peer.LastRenderScaling,
                    peer,
                    peer.DataContext ?? peer.Content,
                    peer.ShuttleTemplate,
                    null);
                StartFlight(peerDeparture, hero, destBounds, sourcePeer: peer);
            }
        }

        if (retryLater.Count > 0)
        {
            _pendingArrivals.AddRange(retryLater);
            ScheduleArrivalProcessing(DispatcherPriority.Loaded);
        }
    }

    private Hero? FindVisiblePeer(Hero exclude, object tag)
    {
        for (int i = _activeHeroes.Count - 1; i >= 0; i--)
        {
            Hero candidate = _activeHeroes[i];
            if (!ReferenceEquals(candidate, exclude) &&
                candidate.IsHeroEnabled &&
                candidate.IsEffectivelyVisible &&
                Equals(candidate.Tag, tag))
            {
                return candidate;
            }
        }

        return null;
    }

    private void RecordDeparture(DepartureRecord record)
    {
        if (_departures.Remove(record.Tag, out DepartureRecord? existing))
        {
            existing.Dispose();
        }

        _departures[record.Tag] = record;

        TimeSpan timeout = CaptureTimeout;
        if (timeout > TimeSpan.Zero)
        {
            DispatcherTimer timer = new() { Interval = timeout };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (_departures.TryGetValue(record.Tag, out DepartureRecord? current) && ReferenceEquals(current, record))
                {
                    _departures.Remove(record.Tag);
                    record.Dispose();
                }
            };
            record.ExpiryTimer = timer;
            timer.Start();
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_departures.TryGetValue(record.Tag, out DepartureRecord? current) && ReferenceEquals(current, record))
                {
                    _departures.Remove(record.Tag);
                    record.Dispose();
                }
            }, DispatcherPriority.Loaded);
        }
    }

    private void ClearDepartures()
    {
        foreach (DepartureRecord record in _departures.Values)
        {
            record.Dispose();
        }

        _departures.Clear();
    }

    private void StartFlight(DepartureRecord departure, Hero destination, Rect destBounds, Hero? sourcePeer = null)
    {
        object tag = departure.Tag;
        TimeSpan duration = destination.Duration ?? Duration;
        HeroFlightMode flightMode = destination.FlightMode ?? FlightMode;
        Easing easing = destination.Easing ?? Easing ?? new CubicEaseInOut();

        if (!IsAnimationEnabled || duration <= TimeSpan.Zero || _overlayLayer is null)
        {
            departure.Dispose();
            return;
        }

        if (_activeFlights.TryGetValue(tag, out ActiveFlight? existingFlight))
        {
            CompleteFlight(existingFlight);
        }

        double hostScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? departure.RenderScaling;
        double destArea = destBounds.Width * destBounds.Height;
        double destScaling = destArea > 250_000 ? 1.0 : Math.Min(hostScaling, 1.5);
        double sourceArea = departure.BoundsInHost.Width * departure.BoundsInHost.Height;
        double sourceScaling = sourceArea > 250_000 ? 1.0 : Math.Min(departure.RenderScaling, 1.5);

        RenderTargetBitmap? sourceSnapshot = departure.Snapshot ??
            CaptureSnapshot(departure.SourceHero, departure.BoundsInHost.Size, sourceScaling);
        RenderTargetBitmap? destSnapshot = CaptureSnapshot(destination, destBounds.Size, destScaling);

        IDataTemplate? shuttleTemplate = destination.ShuttleTemplate ?? departure.ShuttleTemplate ?? DefaultShuttleTemplate;
        object? shuttleData = destination.DataContext ?? destination.Content ?? departure.DataContext;

        (Border shuttleBorder, Image? sourceImage, Image? destImage) = BuildShuttleVisual(
            shuttleTemplate,
            shuttleData,
            sourceSnapshot,
            destSnapshot,
            destBounds,
            destination.CornerRadius);

        destination.BeginFlightPlaceholderHide();
        sourcePeer?.BeginFlightPlaceholderHide();

        _overlayLayer.Children.Add(shuttleBorder);

        CompositionVisual? shuttleVisual = ElementComposition.GetElementVisual(shuttleBorder);
        CompositionVisual? sourceVisual = sourceImage is not null ? ElementComposition.GetElementVisual(sourceImage) : null;
        CompositionVisual? destVisual = destImage is not null ? ElementComposition.GetElementVisual(destImage) : null;

        bool usesComposition = shuttleVisual is not null;
        bool needsUiFrameLoop = !usesComposition || ShouldAnimateCornerRadius(departure.CornerRadius, destination.CornerRadius, destBounds);

        ScaleTransform? fallbackScale = null;
        TranslateTransform? fallbackTranslate = null;

        if (usesComposition)
        {
            StartCompositionFlightAnimations(
                shuttleVisual!,
                sourceVisual,
                destVisual,
                departure.BoundsInHost,
                destBounds,
                duration,
                flightMode,
                easing);

            if (needsUiFrameLoop)
            {
                double baseWidth = Math.Max(1.0, destBounds.Width);
                double baseHeight = Math.Max(1.0, destBounds.Height);
                shuttleBorder.CornerRadius = ScaleCornerRadiusForShuttle(
                    departure.CornerRadius,
                    departure.BoundsInHost.Width / baseWidth,
                    departure.BoundsInHost.Height / baseHeight);
            }
        }
        else
        {
            double baseWidth = Math.Max(1.0, destBounds.Width);
            double baseHeight = Math.Max(1.0, destBounds.Height);
            fallbackScale = new ScaleTransform(departure.BoundsInHost.Width / baseWidth, departure.BoundsInHost.Height / baseHeight);
            fallbackTranslate = new TranslateTransform(departure.BoundsInHost.X, departure.BoundsInHost.Y);
            TransformGroup transformGroup = new();
            transformGroup.Children.Add(fallbackScale);
            transformGroup.Children.Add(fallbackTranslate);
            shuttleBorder.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
            shuttleBorder.RenderTransform = transformGroup;
            shuttleBorder.CornerRadius = ScaleCornerRadiusForShuttle(
                departure.CornerRadius,
                fallbackScale.ScaleX,
                fallbackScale.ScaleY);
        }

        ActiveFlight flight = new(
            tag,
            departure.BoundsInHost,
            destBounds,
            departure.CornerRadius,
            destination.CornerRadius,
            duration,
            flightMode,
            easing,
            Stopwatch.GetTimestamp(),
            shuttleBorder,
            shuttleVisual,
            sourceVisual,
            destVisual,
            fallbackScale,
            fallbackTranslate,
            sourceImage,
            destImage,
            sourceSnapshot,
            destSnapshot,
            destination,
            sourcePeer,
            needsUiFrameLoop);

        DispatcherTimer completionTimer = new() { Interval = duration };
        completionTimer.Tick += (_, _) =>
        {
            completionTimer.Stop();
            if (_activeFlights.TryGetValue(tag, out ActiveFlight? current) && ReferenceEquals(current, flight))
            {
                CompleteFlight(flight);
            }
        };
        flight.CompletionTimer = completionTimer;
        completionTimer.Start();

        departure.OwnershipTransferred();
        _activeFlights[tag] = flight;

        HeroFlightStarted?.Invoke(this, new HeroFlightEventArgs(tag, departure.BoundsInHost, destBounds, duration, flightMode));
        if (needsUiFrameLoop)
        {
            RequestNextFrame();
        }
    }

    private static bool ShouldAnimateCornerRadius(CornerRadius from, CornerRadius to, Rect destBounds)
    {
        double maxDiff = Math.Max(
            Math.Max(Math.Abs(from.TopLeft - to.TopLeft), Math.Abs(from.TopRight - to.TopRight)),
            Math.Max(Math.Abs(from.BottomRight - to.BottomRight), Math.Abs(from.BottomLeft - to.BottomLeft)));

        return maxDiff > 10.0 && (destBounds.Width * destBounds.Height) < 120_000;
    }

    private static void StartCompositionFlightAnimations(
        CompositionVisual shuttleVisual,
        CompositionVisual? sourceVisual,
        CompositionVisual? destVisual,
        Rect from,
        Rect to,
        TimeSpan duration,
        HeroFlightMode mode,
        Easing easing)
    {
        Compositor compositor = shuttleVisual.Compositor;
        double baseWidth = Math.Max(1.0, to.Width);
        double baseHeight = Math.Max(1.0, to.Height);

        Vector3DKeyFrameAnimation translationAnim = compositor.CreateVector3DKeyFrameAnimation();
        translationAnim.Duration = duration;
        translationAnim.StopBehavior = AnimationStopBehavior.SetToFinalValue;

        Vector3DKeyFrameAnimation scaleAnim = compositor.CreateVector3DKeyFrameAnimation();
        scaleAnim.Duration = duration;
        scaleAnim.StopBehavior = AnimationStopBehavior.SetToFinalValue;

        const int steps = 20;
        for (int i = 0; i <= steps; i++)
        {
            float key = i / (float)steps;
            double easedT = Math.Clamp(easing.Ease(key), 0.0, 1.0);
            Rect rect = InterpolateRect(from, to, easedT, mode);
            translationAnim.InsertKeyFrame(key, new Vector3D(rect.X, rect.Y, 0));
            scaleAnim.InsertKeyFrame(key, new Vector3D(rect.Width / baseWidth, rect.Height / baseHeight, 1));
        }

        shuttleVisual.CenterPoint = new Vector3D(0, 0, 0);
        shuttleVisual.Translation = new Vector3D(from.X, from.Y, 0);
        shuttleVisual.Scale = new Vector3D(from.Width / baseWidth, from.Height / baseHeight, 1);
        shuttleVisual.StartAnimation("Translation", translationAnim);
        shuttleVisual.StartAnimation("Scale", scaleAnim);

        if (sourceVisual is not null && destVisual is not null)
        {
            ScalarKeyFrameAnimation destFade = compositor.CreateScalarKeyFrameAnimation();
            destFade.Duration = duration;
            destFade.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            destFade.InsertKeyFrame(0.0f, 0.0f);
            destFade.InsertKeyFrame(0.55f, 1.0f, easing);
            destFade.InsertKeyFrame(1.0f, 1.0f);

            ScalarKeyFrameAnimation sourceFade = compositor.CreateScalarKeyFrameAnimation();
            sourceFade.Duration = duration;
            sourceFade.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            sourceFade.InsertKeyFrame(0.0f, 1.0f);
            sourceFade.InsertKeyFrame(0.55f, 1.0f);
            sourceFade.InsertKeyFrame(1.0f, 0.0f);

            destVisual.Opacity = 0.0f;
            sourceVisual.Opacity = 1.0f;
            destVisual.StartAnimation("Opacity", destFade);
            sourceVisual.StartAnimation("Opacity", sourceFade);
        }
    }

    private static (Border Shuttle, Image? SourceImage, Image? DestImage) BuildShuttleVisual(
        IDataTemplate? shuttleTemplate,
        object? shuttleData,
        RenderTargetBitmap? sourceSnapshot,
        RenderTargetBitmap? destSnapshot,
        Rect destinationBounds,
        CornerRadius destinationCornerRadius)
    {
        double baseWidth = Math.Max(1.0, destinationBounds.Width);
        double baseHeight = Math.Max(1.0, destinationBounds.Height);

        Border shuttle = new()
        {
            Width = baseWidth,
            Height = baseHeight,
            CornerRadius = destinationCornerRadius,
            ClipToBounds = true,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(shuttle, 0);
        Canvas.SetTop(shuttle, 0);

        if (shuttleTemplate is not null && shuttleTemplate.Build(shuttleData) is Control customContent)
        {
            customContent.DataContext ??= shuttleData;
            shuttle.Child = customContent;
            return (shuttle, null, null);
        }

        Grid grid = new();
        Image? sourceImage = null;
        Image? destImage = null;

        if (sourceSnapshot is not null)
        {
            sourceImage = new Image
            {
                Source = sourceSnapshot,
                Stretch = Stretch.Fill,
            };
            RenderOptions.SetBitmapInterpolationMode(sourceImage, BitmapInterpolationMode.MediumQuality);
            grid.Children.Add(sourceImage);
        }

        if (destSnapshot is not null)
        {
            destImage = new Image
            {
                Source = destSnapshot,
                Stretch = Stretch.Fill,
            };
            RenderOptions.SetBitmapInterpolationMode(destImage, BitmapInterpolationMode.MediumQuality);
            grid.Children.Add(destImage);
        }

        shuttle.Child = grid;
        return (shuttle, sourceImage, destImage);
    }

    private void RequestNextFrame()
    {
        if (_animationFrameRequested)
        {
            return;
        }

        bool hasUiDrivenFlight = false;
        foreach (ActiveFlight flight in _activeFlights.Values)
        {
            if (flight.NeedsUiFrameLoop)
            {
                hasUiDrivenFlight = true;
                break;
            }
        }

        if (!hasUiDrivenFlight)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            CancelAllFlights();
            return;
        }

        _animationFrameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan _)
    {
        _animationFrameRequested = false;
        if (_activeFlights.Count == 0)
        {
            return;
        }

        long now = Stopwatch.GetTimestamp();
        List<ActiveFlight>? completed = null;

        foreach (ActiveFlight flight in _activeFlights.Values)
        {
            if (!flight.NeedsUiFrameLoop)
            {
                continue;
            }

            double elapsedMs = (now - flight.StartTimestamp) * 1000.0 / Stopwatch.Frequency;
            double durationMs = Math.Max(1.0, flight.Duration.TotalMilliseconds);
            double rawT = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
            double easedT = Math.Clamp(flight.Easing.Ease(rawT), 0.0, 1.0);

            Rect rect = InterpolateRect(flight.SourceBounds, flight.DestinationBounds, easedT, flight.FlightMode);
            CornerRadius cornerRadius = InterpolateCornerRadius(flight.SourceCornerRadius, flight.DestinationCornerRadius, easedT);

            double baseWidth = Math.Max(1.0, flight.ShuttleBaseSize.Width);
            double baseHeight = Math.Max(1.0, flight.ShuttleBaseSize.Height);
            double scaleX = rect.Width / baseWidth;
            double scaleY = rect.Height / baseHeight;

            if (flight.ShuttleVisual is null)
            {
                if (flight.FallbackScale is not null)
                {
                    flight.FallbackScale.ScaleX = scaleX;
                    flight.FallbackScale.ScaleY = scaleY;
                }

                if (flight.FallbackTranslate is not null)
                {
                    flight.FallbackTranslate.X = rect.X;
                    flight.FallbackTranslate.Y = rect.Y;
                }

                if (flight.SourceImage is not null && flight.DestImage is not null)
                {
                    flight.SourceImage.Opacity = Math.Min(1.0, (1.0 - easedT) * 2.0);
                    flight.DestImage.Opacity = Math.Min(1.0, easedT * 2.0);
                }
            }

            flight.ShuttleBorder.CornerRadius = ScaleCornerRadiusForShuttle(cornerRadius, scaleX, scaleY);

            if (rawT >= 1.0)
            {
                completed ??= [];
                completed.Add(flight);
            }
        }

        if (completed is not null)
        {
            foreach (ActiveFlight flight in completed)
            {
                CompleteFlight(flight);
            }
        }

        RequestNextFrame();
    }

    private void CompleteFlight(ActiveFlight flight)
    {
        if (!_activeFlights.Remove(flight.Tag))
        {
            return;
        }

        flight.StopAnimations();
        _overlayLayer?.Children.Remove(flight.ShuttleBorder);
        flight.DestinationHero.EndFlightPlaceholderHide();
        flight.SourcePeer?.EndFlightPlaceholderHide();
        flight.DisposeSnapshots();

        HeroFlightCompleted?.Invoke(
            this,
            new HeroFlightEventArgs(
                flight.Tag,
                flight.SourceBounds,
                flight.DestinationBounds,
                flight.Duration,
                flight.FlightMode));
    }

    internal static Rect InterpolateRect(Rect from, Rect to, double t, HeroFlightMode mode)
    {
        double width = Lerp(from.Width, to.Width, t);
        double height = Lerp(from.Height, to.Height, t);

        if (mode == HeroFlightMode.Linear)
        {
            double x = Lerp(from.X, to.X, t);
            double y = Lerp(from.Y, to.Y, t);
            return new Rect(x, y, width, height);
        }

        Point c0 = from.Center;
        Point c1 = to.Center;
        double dx = c1.X - c0.X;
        double dy = c1.Y - c0.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));

        Point center;
        if (distance < 1.0)
        {
            center = new Point(Lerp(c0.X, c1.X, t), Lerp(c0.Y, c1.Y, t));
        }
        else
        {
            double nx = -dy / distance;
            double ny = dx / distance;
            // Keep arc curving consistently outward/downward for a natural Material-like trajectory.
            if (ny < 0)
            {
                nx = -nx;
                ny = -ny;
            }

            double bulge = Math.Min(distance * 0.18, 64.0);
            Point control = new(
                ((c0.X + c1.X) * 0.5) + (nx * bulge),
                ((c0.Y + c1.Y) * 0.5) + (ny * bulge));

            double oneMinusT = 1.0 - t;
            center = new Point(
                (oneMinusT * oneMinusT * c0.X) + (2.0 * oneMinusT * t * control.X) + (t * t * c1.X),
                (oneMinusT * oneMinusT * c0.Y) + (2.0 * oneMinusT * t * control.Y) + (t * t * c1.Y));
        }

        return new Rect(center.X - (width * 0.5), center.Y - (height * 0.5), width, height);
    }

    internal static CornerRadius InterpolateCornerRadius(CornerRadius from, CornerRadius to, double t) =>
        new(
            Lerp(from.TopLeft, to.TopLeft, t),
            Lerp(from.TopRight, to.TopRight, t),
            Lerp(from.BottomRight, to.BottomRight, t),
            Lerp(from.BottomLeft, to.BottomLeft, t));

    private static CornerRadius ScaleCornerRadiusForShuttle(CornerRadius target, double scaleX, double scaleY)
    {
        double avgScale = Math.Max(0.05, (scaleX + scaleY) * 0.5);
        return new CornerRadius(
            target.TopLeft / avgScale,
            target.TopRight / avgScale,
            target.BottomRight / avgScale,
            target.BottomLeft / avgScale);
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static RenderTargetBitmap? CaptureSnapshot(Control? visual, Size size, double renderScaling)
    {
        if (visual is null || size.Width < 1 || size.Height < 1 || !visual.IsVisible)
        {
            return null;
        }

        double scale = double.IsFinite(renderScaling) && renderScaling > 0 ? renderScaling : 1.0;
        int pixelWidth = Math.Max(1, (int)Math.Ceiling(size.Width * scale));
        int pixelHeight = Math.Max(1, (int)Math.Ceiling(size.Height * scale));

        try
        {
            RenderTargetBitmap bitmap = new(new PixelSize(pixelWidth, pixelHeight), new Vector(96 * scale, 96 * scale));
            bitmap.Render(visual);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private sealed record PendingArrival(Hero Hero, object Tag, int RetryCount);

    private sealed class DepartureRecord : IDisposable
    {
        private readonly WeakReference<Hero>? _sourceHeroRef;
        private bool _ownershipTransferred;

        public DepartureRecord(
            object tag,
            Rect boundsInHost,
            CornerRadius cornerRadius,
            double renderScaling,
            Hero? sourceHero,
            object? dataContext,
            IDataTemplate? shuttleTemplate,
            RenderTargetBitmap? snapshot)
        {
            Tag = tag;
            BoundsInHost = boundsInHost;
            CornerRadius = cornerRadius;
            RenderScaling = renderScaling;
            _sourceHeroRef = sourceHero is null ? null : new WeakReference<Hero>(sourceHero);
            DataContext = dataContext;
            ShuttleTemplate = shuttleTemplate;
            Snapshot = snapshot;
        }

        public object Tag { get; }

        public Rect BoundsInHost { get; }

        public CornerRadius CornerRadius { get; }

        public double RenderScaling { get; }

        public Hero? SourceHero => _sourceHeroRef is not null && _sourceHeroRef.TryGetTarget(out Hero? hero) ? hero : null;

        public object? DataContext { get; }

        public IDataTemplate? ShuttleTemplate { get; }

        public RenderTargetBitmap? Snapshot { get; }

        public DispatcherTimer? ExpiryTimer { get; set; }

        public void StopTimer()
        {
            ExpiryTimer?.Stop();
            ExpiryTimer = null;
        }

        public void OwnershipTransferred()
        {
            StopTimer();
            _ownershipTransferred = true;
        }

        public void Dispose()
        {
            StopTimer();
            if (!_ownershipTransferred)
            {
                Snapshot?.Dispose();
            }
        }
    }

    private sealed class ActiveFlight
    {
        private readonly RenderTargetBitmap? _sourceSnapshot;
        private readonly RenderTargetBitmap? _destSnapshot;

        public ActiveFlight(
            object tag,
            Rect sourceBounds,
            Rect destinationBounds,
            CornerRadius sourceCornerRadius,
            CornerRadius destinationCornerRadius,
            TimeSpan duration,
            HeroFlightMode flightMode,
            Easing easing,
            long startTimestamp,
            Border shuttleBorder,
            CompositionVisual? shuttleVisual,
            CompositionVisual? sourceVisual,
            CompositionVisual? destVisual,
            ScaleTransform? fallbackScale,
            TranslateTransform? fallbackTranslate,
            Image? sourceImage,
            Image? destImage,
            RenderTargetBitmap? sourceSnapshot,
            RenderTargetBitmap? destSnapshot,
            Hero destinationHero,
            Hero? sourcePeer,
            bool needsUiFrameLoop)
        {
            Tag = tag;
            SourceBounds = sourceBounds;
            DestinationBounds = destinationBounds;
            ShuttleBaseSize = new Size(Math.Max(1.0, destinationBounds.Width), Math.Max(1.0, destinationBounds.Height));
            SourceCornerRadius = sourceCornerRadius;
            DestinationCornerRadius = destinationCornerRadius;
            Duration = duration;
            FlightMode = flightMode;
            Easing = easing;
            StartTimestamp = startTimestamp;
            ShuttleBorder = shuttleBorder;
            ShuttleVisual = shuttleVisual;
            SourceVisual = sourceVisual;
            DestVisual = destVisual;
            FallbackScale = fallbackScale;
            FallbackTranslate = fallbackTranslate;
            SourceImage = sourceImage;
            DestImage = destImage;
            _sourceSnapshot = sourceSnapshot;
            _destSnapshot = destSnapshot;
            DestinationHero = destinationHero;
            SourcePeer = sourcePeer;
            NeedsUiFrameLoop = needsUiFrameLoop;
        }

        public object Tag { get; }

        public Rect SourceBounds { get; }

        public Rect DestinationBounds { get; }

        public Size ShuttleBaseSize { get; }

        public CornerRadius SourceCornerRadius { get; }

        public CornerRadius DestinationCornerRadius { get; }

        public TimeSpan Duration { get; }

        public HeroFlightMode FlightMode { get; }

        public Easing Easing { get; }

        public long StartTimestamp { get; }

        public Border ShuttleBorder { get; }

        public CompositionVisual? ShuttleVisual { get; }

        public CompositionVisual? SourceVisual { get; }

        public CompositionVisual? DestVisual { get; }

        public ScaleTransform? FallbackScale { get; }

        public TranslateTransform? FallbackTranslate { get; }

        public Image? SourceImage { get; }

        public Image? DestImage { get; }

        public Hero DestinationHero { get; }

        public Hero? SourcePeer { get; }

        public bool NeedsUiFrameLoop { get; }

        public DispatcherTimer? CompletionTimer { get; set; }

        public (Rect Bounds, CornerRadius CornerRadius) EvaluateCurrentState(long timestamp)
        {
            double elapsedMs = (timestamp - StartTimestamp) * 1000.0 / Stopwatch.Frequency;
            double durationMs = Math.Max(1.0, Duration.TotalMilliseconds);
            double rawT = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
            double easedT = Math.Clamp(Easing.Ease(rawT), 0.0, 1.0);
            return (
                InterpolateRect(SourceBounds, DestinationBounds, easedT, FlightMode),
                InterpolateCornerRadius(SourceCornerRadius, DestinationCornerRadius, easedT));
        }

        public void StopAnimations()
        {
            CompletionTimer?.Stop();
            CompletionTimer = null;
            ShuttleVisual?.StopAnimation("Translation");
            ShuttleVisual?.StopAnimation("Scale");
            SourceVisual?.StopAnimation("Opacity");
            DestVisual?.StopAnimation("Opacity");
        }

        public void DisposeSnapshots()
        {
            if (SourceImage is not null)
            {
                SourceImage.Source = null;
            }

            if (DestImage is not null)
            {
                DestImage.Source = null;
            }

            _sourceSnapshot?.Dispose();
            _destSnapshot?.Dispose();
        }
    }
}
