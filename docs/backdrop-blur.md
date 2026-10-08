# BackdropBlur

`BackdropBlur` blurs the page content already drawn behind its bounds and adds an optional tint and border. Draw background content first, then the blur surface, then foreground controls as later siblings in a `Grid`.

```xml
<Grid xmlns:aviora="https://github.com/wobushiafa/Aviora"
      Height="240">
    <Grid Background="#2563EB">
        <Ellipse Width="180" Height="180" Fill="#FB923C" />
        <Border Height="8" Background="#FDE68A" />
    </Grid>
    <aviora:BackdropBlur Margin="24"
                         BlurRadius="12"
                         RenderScale="1"
                         CornerRadius="24"
                         Background="#99FFFFFF"
                         BorderBrush="#CCFFFFFF"
                         BorderThickness="1"
                         IsHitTestVisible="False" />
    <StackPanel Margin="48" VerticalAlignment="Center" Spacing="8">
        <TextBlock Text="Clear foreground" Foreground="#0F172A" />
        <Button Content="Continue" HorizontalAlignment="Left" />
    </StackPanel>
</Grid>
```

## Properties

| Property | Default | Behavior |
|---|---|---|
| `BlurRadius` | `12` | Gaussian sigma in DIPs; finite values ≥0. Zero disables blur. |
| `RenderScale` | `1` | Background sampling resolution, from 0.25 to 1. Lower values reduce detail and can improve large/high-DPI regions. |
| `CornerRadius` | Inherited control default | Clips the blur and tint, including individual corner radii. |
| `Background` | Inherited control default | Tint painted above the blur. Use transparency to keep the blurred background visible. |
| `BorderBrush`, `BorderThickness` | Inherited control defaults | Standard border styling, including asymmetric thickness. |
| `BackgroundSizing` | Inherited control default | Uses standard Avalonia border background sizing. |

The control inherits `TemplatedControl` and does not host foreground content. Place text and buttons above it as siblings. Its corner radius does not clip those siblings; use a surrounding clipping container when foreground clipping is needed. Background content must belong to the same rendered scene; this is not desktop/window acrylic.

## Styling and accessibility

Tint and border brushes can reference application theme resources. The control does not define dedicated theme resource keys or require a special content template.

Treat the blur surface as decorative and set `IsHitTestVisible="False"` when placing interactive controls above it. Keep foreground text readable against both light and dark backgrounds. The surface adds no text, focus target, or accessible action; place accessible labels and interactions in the foreground controls.

## Quality and performance

Start with `RenderScale="1"`. Try 0.75 or 0.5 for large/high-DPI panels and validate on target devices. The blur radius retains its DIP meaning, while corners, borders, and foreground content keep their original resolution. A scale of 0.25 can visibly reduce small-radius detail.

Background pixels are refreshed on every draw. Native resources are reused within each control and released when its last retained drawing operation is disposed. Radius or size animation can reuse work texture capacity; different controls retain independent backgrounds and preserve drawing order.

The performance and GPU image checks currently cover Windows Skia OpenGL. Results are synchronous rendering costs; real page scrolling, presentation, other rendering backends, and long-running GPU memory usage require application-level validation.

See the [P1 performance report](performance/2026-10-08-backdrop-blur-p1.md) and [probe instructions](../tools/BackdropBlur.Perf/README.md).
