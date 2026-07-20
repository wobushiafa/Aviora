using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Aviora.Controls;

internal static class AvioraControlPalette
{
    internal static readonly IBrush Accent = Brush("#0F766E");
    internal static readonly IBrush AccentStrong = Brush("#115E59");
    internal static readonly IBrush SurfaceRaised = Brush("#F8FAFC");
    internal static readonly IBrush Border = Brush("#CBD5E1");
    internal static readonly IBrush Subtle = Brush("#E2E8F0");
    internal static readonly IBrush Muted = Brush("#94A3B8");
    internal static readonly IBrush Text = Brush("#0F172A");
    internal static readonly IBrush TextMuted = Brush("#64748B");
    internal static readonly IBrush Warning = Brush("#D97706");
    internal static readonly IBrush Danger = Brush("#DC2626");
    internal static readonly IBrush Highlight = Brush("#B3FFFFFF");

    private static ImmutableSolidColorBrush Brush(string color) =>
        new(Color.Parse(color));
}
