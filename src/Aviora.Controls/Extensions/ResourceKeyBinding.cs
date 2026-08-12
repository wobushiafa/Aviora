using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Aviora.Controls.Extensions;

/// <summary>Creates a binding that resolves an application resource by key.</summary>
/// <param name="key">The resource key to resolve.</param>
public sealed class ResourceKeyBinding(string key) : MarkupExtension
{
    /// <summary>Gets the resource key to resolve.</summary>
    public string Key { get; } = key;

    /// <summary>Creates the binding used to resolve <see cref="Key"/>.</summary>
    /// <param name="serviceProvider">The XAML service provider.</param>
    /// <returns>A binding that resolves the resource value.</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = Key,
            Converter = ResourceKeyConverter.Instance,
        };
    }

    private sealed class ResourceKeyConverter : IValueConverter
    {
        public static ResourceKeyConverter Instance { get; } = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string key || string.IsNullOrWhiteSpace(key))
            {
                return AvaloniaProperty.UnsetValue;
            }

            if (Application.Current is { } application &&
                application.TryGetResource(key, application.ActualThemeVariant, out var resource) &&
                resource is not null)
            {
                return resource;
            }

            throw new KeyNotFoundException($"Resource '{key}' is not registered.");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
