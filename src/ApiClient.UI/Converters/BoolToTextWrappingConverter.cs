using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ApiClient.UI.Converters;

/// <summary>Converts a <see cref="bool"/> word-wrap flag to a <see cref="TextWrapping"/> value.</summary>
public sealed class BoolToTextWrappingConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TextWrapping.Wrap;
}
