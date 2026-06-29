using System;
using System.Globalization;
using ApiClient.Core.Json;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ApiClient.UI.Converters;

/// <summary>Maps a <see cref="JsonNodeKind"/> to a color for the JSON response tree.</summary>
public sealed class JsonNodeKindToBrushConverter : IValueConverter
{
    private static readonly IBrush StringBrush = new SolidColorBrush(Color.Parse("#CE9178"));
    private static readonly IBrush NumberBrush = new SolidColorBrush(Color.Parse("#B5CEA8"));
    private static readonly IBrush BooleanBrush = new SolidColorBrush(Color.Parse("#569CD6"));
    private static readonly IBrush NullBrush = new SolidColorBrush(Color.Parse("#808080"));
    private static readonly IBrush ContainerBrush = new SolidColorBrush(Color.Parse("#9E9E9E"));

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        JsonNodeKind.String => StringBrush,
        JsonNodeKind.Number => NumberBrush,
        JsonNodeKind.Boolean => BooleanBrush,
        JsonNodeKind.Null => NullBrush,
        _ => ContainerBrush,
    };

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
