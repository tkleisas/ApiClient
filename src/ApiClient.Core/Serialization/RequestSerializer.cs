using System.Text.Json;
using System.Text.Json.Serialization;
using ApiClient.Core.Model;

namespace ApiClient.Core.Serialization;

/// <summary>
/// Serializes <see cref="ApiRequest"/> instances to and from the on-disk JSON format.
/// The format is intentionally stable, human-readable, and diff-friendly: camelCase
/// property names, enums written as strings, indented output, and null optionals
/// omitted. Unknown properties are ignored on read so that files written by newer
/// versions degrade gracefully.
/// </summary>
public sealed class RequestSerializer
{
    /// <summary>
    /// The shared JSON options describing the request file format. Exposed so other
    /// components (e.g. collection storage) can reuse the exact same conventions.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions() => new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializes <paramref name="request"/> to its JSON file representation.</summary>
    public string Serialize(ApiRequest request)
        => JsonSerializer.Serialize(request, Options);

    /// <summary>
    /// Deserializes a request from its JSON file representation.
    /// </summary>
    /// <exception cref="System.Text.Json.JsonException">The input is not valid JSON or cannot be mapped to a request.</exception>
    public ApiRequest Deserialize(string json)
        => JsonSerializer.Deserialize<ApiRequest>(json, Options)
           ?? throw new JsonException("Request JSON deserialized to null.");
}
