using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ApiClient.UI.Converters;

/// <summary>Converts a test's pass/fail bool to a check or cross symbol.</summary>
public sealed class PassedToSymbolConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "✓" : "✗";

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts a test's pass/fail bool to a green or red brush.</summary>
public sealed class PassedToBrushConverter : IValueConverter
{
    private static readonly IBrush Pass = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush Fail = new SolidColorBrush(Color.Parse("#E06C75"));

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Pass : Fail;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
