using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Aviora.Controls;
using SkiaSharp;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        PerfApp.OutputDirectory = Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "results"));
        PerfApp.Diagnostics = args.Contains("--diagnostics");
        PerfApp.Optimize = args.Contains("--optimize");
        PerfApp.P0 = args.Contains("--p0") || PerfApp.Optimize;
        PerfApp.VerifyOnly = args.Contains("--verify-only");
        PerfApp.AllowDifferences = args.Contains("--allow-differences");
        int baselineIndex = Array.IndexOf(args, "--baseline");
        PerfApp.BaselineAssembly = baselineIndex >= 0 ? Path.GetFullPath(args[baselineIndex + 1]) : null;
        int prototypeIndex = Array.IndexOf(args, "--prototype");
        PerfApp.PrototypeAssembly = prototypeIndex >= 0 ? Path.GetFullPath(args[prototypeIndex + 1]) : null;
        Directory.CreateDirectory(PerfApp.OutputDirectory);
        try { return AppBuilder.Configure<PerfApp>().UsePlatformDetect().StartWithClassicDesktopLifetime([]); }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(PerfApp.OutputDirectory, "error.txt"), ex.ToString());
            return 1;
        }
    }
}

public sealed class PerfApp : Application
{
    public static string OutputDirectory = "";
    public static bool Diagnostics;
    public static bool P0;
    public static bool Optimize;
    public static bool VerifyOnly;
    public static bool AllowDifferences;
    public static string? BaselineAssembly;
    public static string? PrototypeAssembly;
    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        lifetime.MainWindow = new Window
        {
            Title = "BackdropBlur performance probe", Width = 256, Height = 128,
            WindowDecorations = WindowDecorations.None, ShowInTaskbar = false,
            Content = new PerfBootstrap(lifetime),
        };
        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class PerfBootstrap(IClassicDesktopStyleApplicationLifetime lifetime) : Control
{
    public override void Render(DrawingContext context) => context.Custom(new BootstrapOperation(new Rect(Bounds.Size), lifetime));
    private sealed class BootstrapOperation(Rect bounds, IClassicDesktopStyleApplicationLifetime lifetime) : ICustomDrawOperation
    {
        private bool _started;
        public Rect Bounds => bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            if (_started) return;
            _started = true;
            try
            {
                using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()!.Lease();
                if (lease.GrContext is not { } gpu) throw new InvalidOperationException("A real desktop GPU context is required.");
                if (PerfApp.VerifyOnly) Benchmark.Verify(gpu, PerfApp.OutputDirectory);
                else Benchmark.Run(gpu, PerfApp.OutputDirectory, PerfApp.Diagnostics);
                Dispatcher.UIThread.Post(() => lifetime.Shutdown());
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(PerfApp.OutputDirectory, "error.txt"), ex.ToString());
                Dispatcher.UIThread.Post(() => lifetime.Shutdown(1));
            }
        }
    }
}

internal sealed record Scenario(string Name, int SurfaceWidth, int SurfaceHeight, int Width, int Height,
    int Count, double Radius, double Scale = 1, bool Overlap = false, bool RecreateOperation = false,
    bool DynamicBackground = true, bool SnapshotOnly = false, bool FilterOnly = false,
    double RenderScale = 1, bool Legacy = false, bool ForceLocal = false, double ClipFraction = 1);
internal sealed record Sample(string Scenario, int Round, int Frame, double TotalMs, double SubmitMs, long ManagedBytes, double BaselineMs);

internal static class Benchmark
{
    private const int WarmupFrames = 20;
    private const int MeasuredFrames = 120;
    private const int Rounds = 3;
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type ContextType = typeof(ISkiaSharpApiLeaseFeature).Assembly.GetType("Avalonia.Skia.DrawingContextImpl", true)!;
    private static readonly Type InfoType = ContextType.GetNestedType("CreateInfo", BindingFlags.Public | BindingFlags.NonPublic)!;
    private static readonly Func<Rect, CornerRadius, double, double, bool, ICustomDrawOperation> NewOperation = BuildOperationFactory(typeof(BackdropBlur));

    private static Func<Rect, CornerRadius, double, double, bool, ICustomDrawOperation> BuildOperationFactory(Type controlType)
    {
        var type = controlType.GetNestedType("BackdropOperation", BindingFlags.NonPublic)!;
        var ownerType = controlType.GetNestedType("BackdropResourcesOwner", BindingFlags.NonPublic);
        var ctor = (ownerType is null ? null : type.GetConstructor(Flags, null,
            [typeof(Rect), typeof(CornerRadius), typeof(double), typeof(double), ownerType], null))
            ?? type.GetConstructor(Flags, null, [typeof(Rect), typeof(CornerRadius), typeof(double), typeof(double), typeof(bool)], null)
            ?? type.GetConstructor(Flags, null, [typeof(Rect), typeof(CornerRadius), typeof(double), typeof(double)], null)
            ?? type.GetConstructor(Flags, null, [typeof(Rect), typeof(CornerRadius), typeof(double)], null)!;
        var bounds = Expression.Parameter(typeof(Rect));
        var corners = Expression.Parameter(typeof(CornerRadius));
        var radius = Expression.Parameter(typeof(double));
        var renderScale = Expression.Parameter(typeof(double));
        var local = Expression.Parameter(typeof(bool));
        Expression[] arguments = ctor.GetParameters().Length switch
        {
            5 => [bounds, corners, radius, renderScale, ownerType is not null
                ? Expression.Constant(Activator.CreateInstance(ownerType, true), ownerType) : local],
            4 => [bounds, corners, radius, renderScale],
            _ => [bounds, corners, radius],
        };
        return Expression.Lambda<Func<Rect, CornerRadius, double, double, bool, ICustomDrawOperation>>(
            Expression.Convert(Expression.New(ctor, arguments), typeof(ICustomDrawOperation)), bounds, corners, radius, renderScale, local).Compile();
    }

    private static (IDrawingContextImpl Impl, ImmediateDrawingContext Immediate) MakeContext(SKSurface surface, GRContext? gpu)
    {
        object info = Activator.CreateInstance(InfoType)!;
        InfoType.GetField("Canvas")!.SetValue(info, surface.Canvas);
        InfoType.GetField("Surface")!.SetValue(info, surface);
        InfoType.GetField("GrContext")!.SetValue(info, gpu);
        InfoType.GetField("Dpi")!.SetValue(info, new Vector(96, 96));
        var impl = (IDrawingContextImpl)Activator.CreateInstance(ContextType, Flags, null, [info, Array.Empty<IDisposable>()], null)!;
        var immediate = (ImmediateDrawingContext)Activator.CreateInstance(typeof(ImmediateDrawingContext), Flags, null, [impl, true], null)!;
        return (impl, immediate);
    }

    public static void Verify(GRContext gpu, string output)
    {
        if (PerfApp.BaselineAssembly is not { } path) throw new ArgumentException("Verification requires --baseline.");
        var legacy = BuildOperationFactory(new AssemblyLoadContext("BackdropBlurVerificationBaseline").LoadFromAssemblyPath(path)
            .GetType("Aviora.Controls.BackdropBlur", true)!);
        var results = new List<object>();
        foreach (double dpi in new[] { 1d, 1.25, 2 })
        foreach (double radius in new[] { 4d, 12 })
        foreach (double angle in new[] { 0d, 15 })
        {
            int width = (int)(640 * dpi), height = (int)(480 * dpi);
            using var surface = SKSurface.Create(gpu, true, new SKImageInfo(width, height));
            using (var proof = surface.Snapshot())
                if (!proof.IsTextureBacked) throw new InvalidOperationException("Verification must run on GPU.");
            var (_, immediate) = MakeContext(surface, gpu);
            using (immediate)
            using (var paint = new SKPaint { IsAntialias = true })
            using (var font = new SKFont(SKTypeface.Default, (float)(18 * dpi)))
            {
                byte[] Draw(ICustomDrawOperation operation, bool red, bool translucent)
                {
                    var canvas = surface.Canvas;
                    canvas.ResetMatrix();
                    canvas.Clear(red ? SKColors.DarkRed : SKColors.DarkSlateBlue);
                    for (int stripe = 0; stripe < 80; stripe++)
                    {
                        paint.Color = stripe % 2 == 0 ? SKColors.Coral : SKColors.Teal;
                        canvas.DrawRect((float)(stripe * 8 * dpi), 0, (float)(4 * dpi), height, paint);
                    }
                    paint.Color = SKColors.White;
                    for (int row = 0; row < 8; row++)
                        canvas.DrawText("Backdrop / fine text / 0123456789", (float)(70 * dpi), (float)((100 + row * 36) * dpi), font, paint);
                    var matrix = SKMatrix.CreateScale((float)dpi, (float)dpi);
                    matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation(120.3f, 140.7f));
                    matrix = SKMatrix.Concat(matrix, SKMatrix.CreateRotationDegrees((float)angle, 160, 90));
                    canvas.SetMatrix(matrix);
                    canvas.Save();
                    canvas.ClipRect(new SKRect(0, 0, 320, 180));
                    using (immediate.PushOpacity(translucent ? .5 : 1, new Rect(0, 0, 320, 180))) operation.Render(immediate);
                    canvas.Restore();
                    gpu.Flush(true, true);
                    using var image = surface.Snapshot();
                    using var bitmap = SKBitmap.FromImage(image);
                    return bitmap.Bytes;
                }
                foreach (var (scale, forceLocal) in new[] { (1d, false), (.75, false), (.5, false), (.25, false) })
                {
                    using var original = legacy(new Rect(0, 0, 320, 180), new CornerRadius(32, 0, 24, 8), radius, scale, false);
                    byte[] expected = Draw(original, false, false);
                    byte[] expectedTranslucent = Draw(original, true, true);
                    var factory = BuildOperationFactory(typeof(BackdropBlur));
                    using var operation = factory(new Rect(0, 0, 320, 180), new CornerRadius(32, 0, 24, 8), radius, scale, forceLocal);
                    byte[] actual = Draw(operation, false, false);
                    double sum = 0; int max = 0, count = 0;
                    for (int i = 0; i < actual.Length; i++)
                    {
                        int difference = Math.Abs(actual[i] - expected[i]);
                        sum += difference; max = Math.Max(max, difference);
                        if (difference != 0) count++;
                    }
                    // Reuse the same native work surfaces with changed pixels and opacity.
                    byte[] changed = Draw(operation, true, true);
                    byte[] restored = Draw(operation, false, false);
                    if (!actual.SequenceEqual(restored) || actual.SequenceEqual(changed))
                        throw new InvalidOperationException("Stale pixels or unstable resource reuse.");
                    double opacityError = changed.Select((value, index) => Math.Abs(value - expectedTranslucent[index])).Average();
                    // Native backdrop compositing can round one channel differently.
                    // Allowing that must never disable the default-path regression guard.
                    int maxAllowed = scale == 1 ? (PerfApp.AllowDifferences ? 2 : 0) : 16;
                    double meanAllowed = scale == 1 ? .25 : 1;
                    if (max > maxAllowed || sum / actual.Length > meanAllowed || opacityError > meanAllowed)
                        throw new InvalidOperationException("Default pixels or opacity differ from the baseline.");
                    // Exercise capacity growth through the same owner, then render
                    // the still-active predecessor at its original exact radius.
                    using var grown = factory(new Rect(0, 0, 320, 180), new CornerRadius(32, 0, 24, 8), radius + .5, scale, forceLocal);
                    Draw(grown, true, true);
                    byte[] afterGrowth = Draw(operation, false, false);
                    int capacityMaxError = 0;
                    double capacitySum = 0;
                    for (int i = 0; i < actual.Length; i++)
                    {
                        int difference = Math.Abs(afterGrowth[i] - actual[i]);
                        capacityMaxError = Math.Max(capacityMaxError, difference);
                        capacitySum += difference;
                    }
                    if (capacityMaxError > maxAllowed || capacitySum / actual.Length > meanAllowed)
                        throw new InvalidOperationException("Texture growth changed blur pixels beyond the allowed tolerance.");
                    results.Add(new { dpi, radius, angle, scale, forceLocal, meanAbsoluteChannelError = sum / actual.Length,
                        maxChannelError = max, opacityMeanError = opacityError, changedChannels = count, dynamicRefreshPassed = true,
                        capacityMaxError, capacityMeanError = capacitySum / actual.Length, capacityGrowthPassed = true });
                    if (dpi == 1 && radius == 12 && angle == 0)
                    {
                        using var image = surface.Snapshot();
                        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
                        File.WriteAllBytes(Path.Combine(output, $"preview-scale-{scale.ToString(CultureInfo.InvariantCulture)}-local-{forceLocal}.png"), png.ToArray());
                    }
                }
            }
        }
        File.WriteAllText(Path.Combine(output, "verification.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        VerifyResourceLifetime(gpu, output);
    }

    private static void VerifyResourceLifetime(GRContext gpu, string output)
    {
        var factory = BuildOperationFactory(typeof(BackdropBlur));
        using var surface = SKSurface.Create(gpu, true, new SKImageInfo(160, 160));
        var (_, immediate) = MakeContext(surface, gpu);
        using (immediate)
        using (var paint = new SKPaint { Color = SKColors.Black })
        {
            byte[] Draw(SKSurface target, ImmediateDrawingContext drawing, ICustomDrawOperation? operation, GRContext? context)
            {
                var canvas = target.Canvas;
                canvas.ResetMatrix();
                canvas.Clear(SKColors.White);
                canvas.DrawRect(40, 40, 80, 80, paint);
                canvas.Translate(40, 40);
                canvas.Save();
                canvas.ClipRect(new SKRect(0, 0, 80, 80));
                operation?.Render(drawing);
                canvas.Restore();
                context?.Flush(true, true);
                using var image = target.Snapshot();
                using var bitmap = SKBitmap.FromImage(image);
                return bitmap.Bytes;
            }
            object State(ICustomDrawOperation op) => op.GetType().GetField("_resources", Flags)!.GetValue(op)!;
            void AssertReleased(object state)
            {
                if ((int)state.GetType().GetField("_references", Flags)!.GetValue(state)! != 0
                    || new[] { "_paint", "_compositePaint", "_clip", "_blur", "_previousBlur", "_blurSurface" }
                        .Any(name => state.GetType().GetField(name, Flags)!.GetValue(state) is not null))
                    throw new InvalidOperationException("Native resources survived the last operation release.");
            }
            using var first = factory(new Rect(0, 0, 80, 80), default, 8, 1, false);
            object firstState = State(first);
            byte[] initial = Draw(surface, immediate, first, gpu);
            using var replacement = factory(new Rect(0, 0, 80, 80), new CornerRadius(32, 0, 0, 0), 24, .5, false);
            if (!ReferenceEquals(firstState, State(replacement))) throw new InvalidOperationException("Resources were not reused.");
            Draw(surface, immediate, replacement, gpu);
            if (!initial.SequenceEqual(Draw(surface, immediate, first, gpu)))
                throw new InvalidOperationException("Replacing an operation mutated its predecessor's parameters.");
            replacement.Dispose();
            replacement.Dispose();
            if (!initial.SequenceEqual(Draw(surface, immediate, first, gpu)))
                throw new InvalidOperationException("Disposing a replacement invalidated an active predecessor.");
            first.Dispose();
            AssertReleased(firstState);
            if (!Draw(surface, immediate, null, gpu).SequenceEqual(Draw(surface, immediate, first, gpu)))
                throw new InvalidOperationException("A disposed operation still drew pixels.");

            using var revived = factory(new Rect(0, 0, 80, 80), default, 8, .5, false);
            object revivedState = State(revived);
            if (ReferenceEquals(firstState, revivedState)) throw new InvalidOperationException("A released resource set was resurrected.");
            byte[] gpuPixels = Draw(surface, immediate, revived, gpu);
            using (var raster = SKSurface.Create(new SKImageInfo(160, 160)))
            {
                var (_, cpuDrawing) = MakeContext(raster, null);
                using (cpuDrawing)
                {
                    byte[] pixels = Draw(raster, cpuDrawing, revived, null);
                    if (pixels[(80 * 160 + 42) * 4] is < 30 or > 200)
                        throw new InvalidOperationException("GPU to CPU context switching lost blur.");
                }
            }
            if (!gpuPixels.SequenceEqual(Draw(surface, immediate, revived, gpu)))
                throw new InvalidOperationException("CPU to GPU context switching changed the restored pixels.");
            revived.Dispose();
            AssertReleased(revivedState);
            File.WriteAllText(Path.Combine(output, "resources.json"), JsonSerializer.Serialize(new
            {
                immutablePredecessorPassed = true, reusePassed = true, idempotentDisposePassed = true,
                finalReleasePassed = true, disposedRenderPassed = true, noResurrectionPassed = true,
                gpuCpuGpuSwitchPassed = true,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public static void Run(GRContext gpu, string output, bool diagnostics)
    {
        var legacyType = PerfApp.BaselineAssembly is { } baselinePath
            ? new AssemblyLoadContext("BackdropBlurBaseline").LoadFromAssemblyPath(baselinePath).GetType("Aviora.Controls.BackdropBlur", true)! : null;
        var prototypeType = PerfApp.PrototypeAssembly is { } prototypePath
            ? new AssemblyLoadContext("BackdropBlurPrototype").LoadFromAssemblyPath(prototypePath).GetType("Aviora.Controls.BackdropBlur", true)! : null;
        Scenario[] scenarios =
        [
            new("baseline-1080p", 1920, 1080, 400, 240, 0, 0),
            new("radius-0", 1920, 1080, 400, 240, 1, 0),
            new("radius-4", 1920, 1080, 400, 240, 1, 4),
            new("radius-12", 1920, 1080, 400, 240, 1, 12),
            new("radius-24", 1920, 1080, 400, 240, 1, 24),
            new("radius-48", 1920, 1080, 400, 240, 1, 48),
            new("small-200x120", 1920, 1080, 200, 120, 1, 12),
            new("large-800x480", 1920, 1080, 800, 480, 1, 12),
            new("four-panels", 1920, 1080, 400, 240, 4, 12),
            new("nine-panels", 1920, 1080, 250, 140, 9, 12),
            new("four-overlapping", 1920, 1080, 400, 240, 4, 12, Overlap: true),
            new("dpi-200-percent", 3840, 2160, 400, 240, 1, 12, Scale: 2),
            new("changing-radius", 1920, 1080, 400, 240, 1, 12, RecreateOperation: true),
            new("static-background-repaint", 1920, 1080, 400, 240, 1, 12, DynamicBackground: false),
            new("baseline-4k", 3840, 2160, 400, 240, 0, 0),
            new("same-panel-4k-surface", 3840, 2160, 400, 240, 1, 12),
        ];
        if (diagnostics)
            scenarios =
            [
                new("snapshot-copy-1080p", 1920, 1080, 400, 240, 1, 12, SnapshotOnly: true),
                new("snapshot-copy-4k", 3840, 2160, 400, 240, 1, 12, SnapshotOnly: true),
                new("filter-only-1080p", 1920, 1080, 400, 240, 1, 12, FilterOnly: true),
                new("filter-only-4k", 3840, 2160, 400, 240, 1, 12, FilterOnly: true),
            ];
        if (PerfApp.P0)
        {
            if (legacyType is null) throw new ArgumentException("--p0 requires --baseline <original Aviora.Controls.dll>.");
            Scenario[] cases =
            [
                new("1080p", 1920, 1080, 400, 240, 1, 12),
                new("4k", 3840, 2160, 400, 240, 1, 12),
                new("dpi200", 3840, 2160, 400, 240, 1, 12, Scale: 2),
                new("four", 1920, 1080, 400, 240, 4, 12),
                new("nine", 1920, 1080, 250, 140, 9, 12),
                new("overlap", 1920, 1080, 400, 240, 4, 12, Overlap: true),
            ];
            scenarios = cases.SelectMany(s => new[]
            {
                s with { Name = s.Name + "-legacy", Legacy = true },
                s with { Name = s.Name + "-default-1" },
                s with { Name = s.Name + "-local-1", ForceLocal = true },
                s with { Name = s.Name + "-local-075", RenderScale = .75 },
                s with { Name = s.Name + "-local-05", RenderScale = .5 },
            }).Where(s => !s.ForceLocal || prototypeType is not null).ToArray();
            if (PerfApp.Optimize)
                scenarios = cases.Concat(new Scenario[]
                {
                    new("animation", 1920, 1080, 400, 240, 1, 12, RecreateOperation: true),
                    new("partial", 1920, 1080, 1600, 900, 1, 12, ClipFraction: .25),
                }).SelectMany(s => new[] { 1d, .75, .5 }.SelectMany(scale => new[]
                {
                    s with { Name = s.Name + "-old-s" + scale.ToString(CultureInfo.InvariantCulture), Legacy = true, RenderScale = scale },
                    s with { Name = s.Name + "-new-s" + scale.ToString(CultureInfo.InvariantCulture), RenderScale = scale },
                })).ToArray();
        }
        var samples = new List<Sample>();
        var cold = new Dictionary<string, List<double>>();
        var random = new Random(20261008);
        var clock = Stopwatch.StartNew();
        IEnumerable<Sample> Measure(Scenario scenario, int round)
        {
            var operationType = scenario.Legacy ? legacyType! : scenario.ForceLocal ? prototypeType! : typeof(BackdropBlur);
            using var surface = SKSurface.Create(gpu, true, new SKImageInfo(scenario.SurfaceWidth, scenario.SurfaceHeight));
            if (surface is null) throw new InvalidOperationException("GPU surface creation failed.");
            using var proof = surface.Snapshot();
            if (!proof.IsTextureBacked) throw new InvalidOperationException("Unexpected CPU surface.");
            var (impl, immediate) = MakeContext(surface, gpu);
            using (immediate)
            using (var backgroundPaint = new SKPaint())
            using (var diagnosticPaint = new SKPaint { Color = SKColors.White })
            using (var diagnosticFilter = SKImageFilter.CreateBlur(12, 12, SKShaderTileMode.Clamp))
            {
                var operationFactories = Enumerable.Range(0, scenario.Radius > 0 && !scenario.SnapshotOnly && !scenario.FilterOnly ? scenario.Count : 0)
                    .Select(_ => BuildOperationFactory(operationType)).ToArray();
                var operations = operationFactories.Select(factory => factory(new Rect(0, 0, scenario.Width, scenario.Height),
                    new CornerRadius(24), scenario.Radius, scenario.RenderScale, scenario.ForceLocal)).ToArray();
                void DrawBackground(int frame)
                {
                    surface.Canvas.ResetMatrix();
                    surface.Canvas.Clear(SKColors.DarkSlateBlue);
                    int offset = scenario.DynamicBackground ? ((frame + WarmupFrames) % 24) : 0;
                    for (int stripe = 0; stripe < 48; stripe++)
                    {
                        backgroundPaint.Color = stripe % 2 == 0 ? SKColors.Coral : SKColors.Teal;
                        surface.Canvas.DrawRect(stripe * scenario.SurfaceWidth / 48f + offset, 0,
                            scenario.SurfaceWidth / 96f, scenario.SurfaceHeight, backgroundPaint);
                    }
                }
                DrawBackground(-WarmupFrames);
                // Diagnostic stages use the same crop and output clip as the 400x240,
                // radius-12 case. Filter-only reuses an immutable GPU image.
                var diagnosticCrop = new SKRectI(4, 4, 476, 316);
                using var diagnosticSource = scenario.FilterOnly ? surface.Snapshot(diagnosticCrop) : null;
                diagnosticPaint.ImageFilter = scenario.FilterOnly ? diagnosticFilter : null;
                try
                {
                    for (int frame = -WarmupFrames; frame < MeasuredFrames; frame++)
                    {
                        long baselineStart = Stopwatch.GetTimestamp();
                        DrawBackground(frame);
                        gpu.Flush(true, true);
                        double baselineMs = Stopwatch.GetElapsedTime(baselineStart).TotalMilliseconds;
                        long allocated = GC.GetAllocatedBytesForCurrentThread();
                        long start = Stopwatch.GetTimestamp();
                        DrawBackground(frame);
                        if (scenario.SnapshotOnly || scenario.FilterOnly)
                        {
                            using var snapshot = scenario.SnapshotOnly ? surface.Snapshot(diagnosticCrop) : null;
                            surface.Canvas.Save();
                            surface.Canvas.ClipRect(new SKRect(40, 40, 440, 280));
                            surface.Canvas.DrawImage(snapshot ?? diagnosticSource!, 4, 4, diagnosticPaint);
                            surface.Canvas.Restore();
                        }
                        for (int panel = 0; panel < operations.Length; panel++)
                        {
                            if (scenario.RecreateOperation)
                            {
                                var previous = operations[panel];
                                operations[panel] = operationFactories[panel](new Rect(0, 0, scenario.Width, scenario.Height),
                                    new CornerRadius(24), scenario.Radius + ((frame + WarmupFrames) % 2) * 0.5, scenario.RenderScale, scenario.ForceLocal);
                                previous.Dispose();
                            }
                            int columns = (int)Math.Ceiling(Math.Sqrt(Math.Max(1, scenario.Count)));
                            double x = scenario.Overlap ? 40 + panel * 24 : 40 + panel % columns * (scenario.Width + 40) * scenario.Scale;
                            double y = scenario.Overlap ? 40 + panel * 24 : 40 + panel / columns * (scenario.Height + 40) * scenario.Scale;
                            surface.Canvas.SetMatrix(new SKMatrix
                            {
                                ScaleX = (float)scenario.Scale, ScaleY = (float)scenario.Scale,
                                TransX = (float)x, TransY = (float)y, Persp2 = 1,
                            });
                            surface.Canvas.Save();
                            surface.Canvas.ClipRect(new SKRect(0, 0, (float)(scenario.Width * scenario.ClipFraction),
                                (float)(scenario.Height * scenario.ClipFraction)));
                            operations[panel].Render(immediate);
                            surface.Canvas.Restore();
                        }
                        long submitted = Stopwatch.GetTimestamp();
                        gpu.Flush(true, true);
                        long completed = Stopwatch.GetTimestamp();
                        var sample = new Sample(scenario.Name, round, frame,
                            Stopwatch.GetElapsedTime(start, completed).TotalMilliseconds,
                            Stopwatch.GetElapsedTime(start, submitted).TotalMilliseconds,
                            GC.GetAllocatedBytesForCurrentThread() - allocated, baselineMs);
                        if (frame >= 0) yield return sample;
                        else if (frame == -WarmupFrames)
                        {
                            if (!cold.TryGetValue(scenario.Name, out var values)) cold[scenario.Name] = values = [];
                            values.Add(sample.TotalMs);
                        }
                    }
                }
                finally { foreach (var operation in operations) operation.Dispose(); }
            }
        }
        for (int round = 0; round < Rounds; round++)
        {
            if (PerfApp.P0)
            {
                // Keep identical variants adjacent in time and randomize their order
                // each frame. Every variant retains its own surface and resources.
                foreach (var group in scenarios.GroupBy(s => s.Name.Split('-')[0]).OrderBy(_ => random.Next()))
                {
                    var enumerators = group.Select(s => Measure(s, round).GetEnumerator()).ToArray();
                    try
                    {
                        for (int frame = 0; frame < MeasuredFrames; frame++)
                            foreach (var enumerator in enumerators.OrderBy(_ => random.Next()))
                            {
                                if (!enumerator.MoveNext()) throw new InvalidOperationException("Missing comparison frame.");
                                samples.Add(enumerator.Current);
                            }
                    }
                    finally { foreach (var enumerator in enumerators) enumerator.Dispose(); }
                    File.WriteAllText(Path.Combine(output, "progress.txt"), $"Round {round + 1}/{Rounds}, {group.Key}, elapsed {clock.Elapsed.TotalSeconds:F1}s");
                }
            }
            else
                foreach (var scenario in scenarios.OrderBy(_ => random.Next()))
                {
                    samples.AddRange(Measure(scenario, round));
                    File.WriteAllText(Path.Combine(output, "progress.txt"), $"Round {round + 1}/{Rounds}, {scenario.Name}, elapsed {clock.Elapsed.TotalSeconds:F1}s");
                }
        }
        var rows = scenarios.Select(scenario =>
        {
            var values = samples.Where(x => x.Scenario == scenario.Name).ToArray();
            var times = values.Select(x => x.TotalMs).Order().ToArray();
            var deltas = values.Select(x => x.TotalMs - x.BaselineMs).Order().ToArray();
            double Percentile(double p) => times[(int)Math.Ceiling((times.Length - 1) * p)];
            return new
            {
                scenario, frames = times.Length, meanMs = times.Average(), p50Ms = Percentile(.50), p95Ms = Percentile(.95),
                p99Ms = Percentile(.99), maxMs = times[^1], meanSubmitMs = values.Average(x => x.SubmitMs),
                managedBytesPerFrame = values.Average(x => x.ManagedBytes), coldMedianMs = cold[scenario.Name].Order().ElementAt(1),
                meanBaselineMs = values.Average(x => x.BaselineMs), meanAddedMs = deltas.Average(),
                medianAddedMs = deltas[deltas.Length / 2], p95AddedMs = deltas[(int)Math.Ceiling((deltas.Length - 1) * .95)],
                framesOver16Ms = times.Count(x => x > 16.667),
                roundMeansMs = values.GroupBy(x => x.Round).Select(g => g.Average(x => x.TotalMs)).ToArray(),
            };
        }).ToArray();
        gpu.GetResourceCacheUsage(out int resources, out long cacheBytes);
        File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new
        {
            measuredAtUtc = DateTimeOffset.UtcNow, runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription, gpuBackend = gpu.Backend.ToString(),
            interleavedVariants = PerfApp.P0,
            WarmupFrames, MeasuredFrames, Rounds, elapsedSeconds = clock.Elapsed.TotalSeconds,
            gpuCacheBytes = cacheBytes, gpuCacheResources = resources, rows,
        }, new JsonSerializerOptions { WriteIndented = true }));
        var csv = new StringBuilder("scenario,round,frame,total_ms,submit_ms,managed_bytes,baseline_ms\n");
        foreach (var sample in samples)
            csv.Append(CultureInfo.InvariantCulture, $"{sample.Scenario},{sample.Round},{sample.Frame},{sample.TotalMs:F6},{sample.SubmitMs:F6},{sample.ManagedBytes},{sample.BaselineMs:F6}\n");
        File.WriteAllText(Path.Combine(output, "frames.csv"), csv.ToString());
    }
}
