# BackdropBlur performance probe

This Windows desktop probe measures the production `BackdropBlur.BackdropOperation` on GPU-backed Skia surfaces. It never modifies the control implementation. Avalonia 12.1's internal drawing context and the private operation are accessed through reflection; the operation constructor is compiled into a delegate before timing starts.

Run from the repository root:

```powershell
dotnet run -c Release --project tools/BackdropBlur.Perf -- artifacts/backdrop-blur-perf/main
dotnet run -c Release --project tools/BackdropBlur.Perf -- artifacts/backdrop-blur-perf/stages --diagnostics
```

The probe opens a small desktop window to acquire Avalonia's GPU context and exits automatically. It requires a GPU-backed surface; errors are written to `error.txt` instead of silently falling back to CPU rendering.

Each scenario runs three rounds, each with 20 warmup frames and 120 measured frames. Scenario order is shuffled using a fixed seed. Each measured scene is preceded by an adjacent frame containing the same background without blur. `GRContext.Flush(true, true)` submits commands and waits for GPU completion for both frames. Results contain absolute scene times and paired differences; these are synchronous rendering costs, not application FPS or GPU shader timestamps.

Outputs:

- `results.json`: scenario settings, aggregate timings, allocation counters, and per-round means.
- `frames.csv`: every measured frame, including its adjacent background baseline.
- `progress.txt`: the most recently completed scenario.

The main sweep varies blur sigma, panel area, panel count, overlap, physical surface size, DPI, and parameter invalidation. A static-background case still forces repaint; it does not measure idle application CPU usage. Radius zero performs no blur, matching the control's zero-radius fast path.

The diagnostics compare cropped snapshot plus unfiltered image drawing against a cached-image blur filter. Both use a 472×312 crop, a 400×240 output clip, and sigma 12. Their values should not be added as if they were independent stages of one frame.

## P0 comparisons and GPU visual verification

Before changing the control, preserve a Release build in a separate output directory:

```powershell
dotnet build tools/BackdropBlur.Perf -c Release -o artifacts/backdrop-blur-baseline
```

After implementing a candidate, build and compare it with the preserved control assembly:

```powershell
dotnet build tools/BackdropBlur.Perf -c Release -o artifacts/backdrop-blur-candidate
dotnet artifacts/backdrop-blur-candidate/BackdropBlur.Perf.dll artifacts/backdrop-blur-comparison --p0 --baseline artifacts/backdrop-blur-baseline/Aviora.Controls.dll
dotnet artifacts/backdrop-blur-candidate/BackdropBlur.Perf.dll artifacts/backdrop-blur-visual --verify-only --baseline artifacts/backdrop-blur-baseline/Aviora.Controls.dll
```

The P0 sweep compares original rendering, the candidate's default full-resolution path, and sampling scales 0.75 and 0.5 on 1080p, 4K, 200% DPI, four/nine panels, and overlapping panels. Both versions run in one GPU process using separate assembly load contexts with shared Avalonia/Skia types. Each variant has its own adjacent no-effect baseline. Variants retain separate surfaces/resources and run in randomized order for each frame; scene groups also shuffle each round. This reduces long-term load drift without eliminating system/driver noise. The first measured frame of each variant follows its warmup block.

Optionally pass `--prototype <prototype Aviora.Controls.dll>` to include a local full-resolution Surface prototype whose private operation accepts the fifth `bool` argument. This experimental switch is absent from the delivered control. Three/four-argument constructors and the current fifth resource-owner argument are supported by the probe.

GPU verification covers sampling scales 1/0.75/0.5/0.25, sigma 4/12, DPI 100/125/200%, and 0/15-degree rotation with fractional positioning and asymmetric corners. It compares each sampling scale against the same scale in the baseline assembly, changes the background and opacity, then restores the pixels. `verification.json` reports errors averaged over the entire image (including unchanged background), maximum channel errors, and opacity errors. Preview PNGs use sigma 12 and 100% DPI. The visual error threshold is a regression guard, not proof of perceptual equivalence; inspect the PNGs and small-radius cases.

## P1 comparisons and resource lifetime

Preserve the P0 assembly before making further changes. Compare the candidate at identical sampling scales:

```powershell
dotnet artifacts/backdrop-blur-candidate/BackdropBlur.Perf.dll artifacts/backdrop-blur-p1-comparison --optimize --baseline artifacts/backdrop-blur-baseline/Aviora.Controls.dll
dotnet artifacts/backdrop-blur-candidate/BackdropBlur.Perf.dll artifacts/backdrop-blur-p1-visual --verify-only --allow-differences --baseline artifacts/backdrop-blur-baseline/Aviora.Controls.dll
```

The optimization sweep has eight scene groups: 1080p, 4K, 200% DPI, four/nine panels, four overlapping panels, animated radius, and a partially visible panel. Each group compares old/new at scales 1/0.75/0.5. This produces 17,280 measured effect frames plus their paired background frames. Radius animation alternates exact sigma 12/12.5 and recreates operations each frame. The partial case has a 1600×900 DIP panel with only one quarter of each dimension visible.

Each panel has its own resource owner. On replacement, the probe creates the new operation before disposing its predecessor, matching retained drawing scene replacement. No resources are shared across panels. This exercises the production native resource cache, including work texture capacity reuse; managed counters still exclude native texture memory.

The default verification requires exact full-resolution pixels. `--allow-differences` permits native backdrop rounding with a maximum of two channel levels and mean/opacity error at most 0.25. Sampled paths must stay within 16 maximum channel levels and mean/opacity error at most 1 versus the same-scale P0 baseline. These bounds target this P1 comparison; they are not thresholds for comparing arbitrary earlier implementations or sampling resolutions.

`resources.json` records additional checks: immutable predecessor parameters, resource reuse, idempotent disposal, final native resource release, no drawing after disposal, no resurrection of disposed resources, and GPU→CPU→GPU context switching with exact restored GPU pixels.

Each visual case also grows the radius through a second operation with the same owner, then renders the predecessor at its original radius. `capacityMaxError` / `capacityMeanError` and `capacityGrowthPassed` cover texture growth separately from fixed-size sampling. Fixed-size sampled paths in P1 keep exact-size textures; capacity is reserved only after growth.

The probe includes background painting, the production blur operation, and GPU completion. It excludes the UI control's `Border` surface, Avalonia layout/binding/compositor overhead, OS presentation, VSync, and foreground content. Managed allocation counters exclude native Skia/GPU texture memory and other threads. The first-frame value is the first draw on a new surface/operation, not a process cold-start measurement. Offscreen physical surface sizes are explicit; the host display's current DPI does not determine them.
