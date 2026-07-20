using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

internal sealed class ChartTextLayout
{
    private readonly Dictionary<TextKey, FormattedText> _cache = [];

    public FormattedText Format(string value, double fontSize, IBrush brush)
    {
        var key = new TextKey(value, Math.Max(1, fontSize), brush);
        if (_cache.TryGetValue(key, out FormattedText? text))
        {
            return text;
        }

        text = new FormattedText(
            value,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
            key.FontSize,
            brush);
        _cache.Add(key, text);
        return text;
    }

    public void Clear() => _cache.Clear();

    private readonly record struct TextKey(string Value, double FontSize, IBrush Brush);
}
