using System.Text.Encodings.Web;
using System.Text.Json;

namespace ApiClient.Core.Json;

/// <summary>Formats JSON text for display, using only <see cref="System.Text.Json"/>.</summary>
public static class JsonFormatter
{
    private static readonly JsonSerializerOptions Pretty = new JsonSerializerOptions
    {
        WriteIndented = true,
        // Keep non-ASCII (e.g. Greek) readable rather than \uXXXX-escaped.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Tries to re-format <paramref name="text"/> as indented JSON. Returns <c>false</c>
    /// (with <paramref name="pretty"/> set to the original text) when it is not valid JSON.
    /// </summary>
    public static bool TryPrettify(string text, out string pretty)
    {
        pretty = text;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            using var document = JsonDocument.Parse(text);
            pretty = JsonSerializer.Serialize(document.RootElement, Pretty);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
