using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ApiClient.UI.Converters;

/// <summary>Converts a hex color string to a brush (transparent for empty/invalid, used for accent swatches/preview).</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex && Color.TryParse(hex, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
