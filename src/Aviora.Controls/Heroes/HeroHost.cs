using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
            Rect currentBounds = inFlight.CurrentBounds;
            CornerRadius currentCornerRadius = inFlight.CurrentCornerRadius;
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

        double scaling = Math.Min(TopLevel.GetTopLevel(this)?.RenderScaling ?? departure.RenderScaling, 1.5);
        RenderTargetBitmap? sourceSnapshot = departure.Snapshot ??
            CaptureSnapshot(departure.SourceHero, departure.BoundsInHost.Size, Math.Min(departure.RenderScaling, 1.5));
        RenderTargetBitmap? destSnapshot = CaptureSnapshot(destination, destBounds.Size, scaling);

        IDataTemplate? shuttleTemplate = destination.ShuttleTemplate ?? departure.ShuttleTemplate ?? DefaultShuttleTemplate;
        object? shuttleData = destination.DataContext ?? destination.Content ?? departure.DataContext;

        (Border shuttleBorder, ScaleTransform scaleTransform, TranslateTransform translateTransform, Image? sourceImage, Image? destImage) = BuildShuttleVisual(
            shuttleTemplate,
            shuttleData,
            sourceSnapshot,
            destSnapshot,
            departure.BoundsInHost,
            destBounds,
            departure.CornerRadius);

        destination.BeginFlightPlaceholderHide();
        sourcePeer?.BeginFlightPlaceholderHide();

        _overlayLayer.Children.Add(shuttleBorder);

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
            scaleTransform,
            translateTransform,
            sourceImage,
            destImage,
            sourceSnapshot,
            destSnapshot,
            destination,
            sourcePeer);

        departure.OwnershipTransferred();
        _activeFlights[tag] = flight;

        HeroFlightStarted?.Invoke(this, new HeroFlightEventArgs(tag, departure.BoundsInHost, destBounds, duration, flightMode));
        RequestNextFrame();
    }

    private static (Border Shuttle, ScaleTransform Scale, TranslateTransform Translate, Image? SourceImage, Image? DestImage) BuildShuttleVisual(
        IDataTemplate? shuttleTemplate,
        object? shuttleData,
        RenderTargetBitmap? sourceSnapshot,
        RenderTargetBitmap? destSnapshot,
        Rect initialBounds,
        Rect destinationBounds,
        CornerRadius initialCornerRadius)
    {
        double baseWidth = Math.Max(1.0, destinationBounds.Width);
        double baseHeight = Math.Max(1.0, destinationBounds.Height);
        ScaleTransform scaleTransform = new(initialBounds.Width / baseWidth, initialBounds.Height / baseHeight);
        TranslateTransform translateTransform = new(initialBounds.X, initialBounds.Y);
        TransformGroup transformGroup = new();
        transformGroup.Children.Add(scaleTransform);
        transformGroup.Children.Add(translateTransform);

        Border shuttle = new()
        {
            Width = baseWidth,
            Height = baseHeight,
            CornerRadius = ScaleCornerRadiusForShuttle(initialCornerRadius, scaleTransform.ScaleX, scaleTransform.ScaleY),
            ClipToBounds = true,
            IsHitTestVisible = false,
            RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
            RenderTransform = transformGroup,
        };
        Canvas.SetLeft(shuttle, 0);
        Canvas.SetTop(shuttle, 0);

        if (shuttleTemplate is not null && shuttleTemplate.Build(shuttleData) is Control customContent)
        {
            customContent.DataContext ??= shuttleData;
            shuttle.Child = customContent;
            return (shuttle, scaleTransform, translateTransform, null, null);
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
                Opacity = 1,
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
                Opacity = sourceImage is not null ? 0 : 1,
            };
            RenderOptions.SetBitmapInterpolationMode(destImage, BitmapInterpolationMode.MediumQuality);
            grid.Children.Add(destImage);
        }

        shuttle.Child = grid;
        return (shuttle, scaleTransform, translateTransform, sourceImage, destImage);
    }

    private void RequestNextFrame()
    {
        if (_animationFrameRequested || _activeFlights.Count == 0)
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
            if (flight.DestinationHero.IsEffectivelyVisible &&
                flight.DestinationHero.TryGetCurrentBoundsInHost(this, out Rect liveDestBounds))
            {
                flight.DestinationBounds = liveDestBounds;
            }

            double elapsedMs = (now - flight.StartTimestamp) * 1000.0 / Stopwatch.Frequency;
            double durationMs = Math.Max(1.0, flight.Duration.TotalMilliseconds);
            double rawT = Math.Clamp(elapsedMs / durationMs, 0.0, 1.0);
            double easedT = Math.Clamp(flight.Easing.Ease(rawT), 0.0, 1.0);

            Rect rect = InterpolateRect(flight.SourceBounds, flight.DestinationBounds, easedT, flight.FlightMode);
            CornerRadius cornerRadius = InterpolateCornerRadius(flight.SourceCornerRadius, flight.DestinationCornerRadius, easedT);

            flight.CurrentBounds = rect;
            flight.CurrentCornerRadius = cornerRadius;

            double baseWidth = Math.Max(1.0, flight.ShuttleBaseSize.Width);
            double baseHeight = Math.Max(1.0, flight.ShuttleBaseSize.Height);
            double scaleX = rect.Width / baseWidth;
            double scaleY = rect.Height / baseHeight;

            flight.ScaleTransform.ScaleX = scaleX;
            flight.ScaleTransform.ScaleY = scaleY;
            flight.TranslateTransform.X = rect.X;
            flight.TranslateTransform.Y = rect.Y;
            flight.ShuttleBorder.CornerRadius = ScaleCornerRadiusForShuttle(cornerRadius, scaleX, scaleY);

            if (flight.SourceImage is not null && flight.DestImage is not null)
            {
                // Keep at least one snapshot at full opacity across [0, 1] so opaque heroes
                // never become semi-transparent mid-flight due to SourceOver alpha blending.
                flight.SourceImage.Opacity = Math.Min(1.0, (1.0 - easedT) * 2.0);
                flight.DestImage.Opacity = Math.Min(1.0, easedT * 2.0);
            }

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

        if (_activeFlights.Count > 0)
        {
            RequestNextFrame();
        }
    }

    private void CompleteFlight(ActiveFlight flight)
    {
        _activeFlights.Remove(flight.Tag);
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
            ScaleTransform scaleTransform,
            TranslateTransform translateTransform,
            Image? sourceImage,
            Image? destImage,
            RenderTargetBitmap? sourceSnapshot,
            RenderTargetBitmap? destSnapshot,
            Hero destinationHero,
            Hero? sourcePeer)
        {
            Tag = tag;
            SourceBounds = sourceBounds;
            DestinationBounds = destinationBounds;
            ShuttleBaseSize = new Size(Math.Max(1.0, destinationBounds.Width), Math.Max(1.0, destinationBounds.Height));
            CurrentBounds = sourceBounds;
            SourceCornerRadius = sourceCornerRadius;
            DestinationCornerRadius = destinationCornerRadius;
            CurrentCornerRadius = sourceCornerRadius;
            Duration = duration;
            FlightMode = flightMode;
            Easing = easing;
            StartTimestamp = startTimestamp;
            ShuttleBorder = shuttleBorder;
            ScaleTransform = scaleTransform;
            TranslateTransform = translateTransform;
            SourceImage = sourceImage;
            DestImage = destImage;
            _sourceSnapshot = sourceSnapshot;
            _destSnapshot = destSnapshot;
            DestinationHero = destinationHero;
            SourcePeer = sourcePeer;
        }

        public object Tag { get; }

        public Rect SourceBounds { get; }

        public Rect DestinationBounds { get; set; }

        public Size ShuttleBaseSize { get; }

        public Rect CurrentBounds { get; set; }

        public CornerRadius SourceCornerRadius { get; }

        public CornerRadius DestinationCornerRadius { get; }

        public CornerRadius CurrentCornerRadius { get; set; }

        public TimeSpan Duration { get; }

        public HeroFlightMode FlightMode { get; }

        public Easing Easing { get; }

        public long StartTimestamp { get; }

        public Border ShuttleBorder { get; }

        public ScaleTransform ScaleTransform { get; }

        public TranslateTransform TranslateTransform { get; }

        public Image? SourceImage { get; }

        public Image? DestImage { get; }

        public Hero DestinationHero { get; }

        public Hero? SourcePeer { get; }

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
