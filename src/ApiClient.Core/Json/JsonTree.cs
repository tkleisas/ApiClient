using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ApiClient.Core.Json;

/// <summary>The kind of a JSON tree node, used by the UI for color coding.</summary>
public enum JsonNodeKind
{
    /// <summary>A JSON object <c>{ ... }</c>.</summary>
    Object,

    /// <summary>A JSON array <c>[ ... ]</c>.</summary>
    Array,

    /// <summary>A string value.</summary>
    String,

    /// <summary>A numeric value.</summary>
    Number,

    /// <summary>A boolean value.</summary>
    Boolean,

    /// <summary>A null value.</summary>
    Null,
}

/// <summary>
/// A node in a parsed JSON document, suitable for display in an expandable, color-coded
/// tree. Built from <see cref="System.Text.Json"/> — no third-party JSON library.
/// </summary>
public sealed class JsonTreeNode
{
    /// <summary>The property name (for object members) or index label like <c>[0]</c> (for array elements); empty for the root.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The display value: the literal for primitives, or a summary like <c>{3}</c> / <c>[5]</c> for containers.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>The node kind.</summary>
    public JsonNodeKind Kind { get; init; }

    /// <summary>Child nodes (members or elements); empty for primitives.</summary>
    public IReadOnlyList<JsonTreeNode> Children { get; init; } = [];
}

/// <summary>Parses JSON text into a <see cref="JsonTreeNode"/> tree.</summary>
public static class JsonTree
{
    /// <summary>Parses <paramref name="json"/> into a tree.</summary>
    /// <exception cref="JsonException">The input is not valid JSON.</exception>
    public static JsonTreeNode Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Build(string.Empty, document.RootElement);
    }

    private static JsonTreeNode Build(string name, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var members = element.EnumerateObject().Select(p => Build(p.Name, p.Value)).ToList();
                return new JsonTreeNode { Name = name, Kind = JsonNodeKind.Object, Value = $"{{{members.Count}}}", Children = members };

            case JsonValueKind.Array:
                var items = element.EnumerateArray().Select((e, i) => Build($"[{i}]", e)).ToList();
                return new JsonTreeNode { Name = name, Kind = JsonNodeKind.Array, Value = $"[{items.Count}]", Children = items };

            case JsonValueKind.String:
                return new JsonTreeNode { Name = name, Kind = JsonNodeKind.String, Value = $"\"{element.GetString()}\"" };

            case JsonValueKind.Number:
                return new JsonTreeNode { Name = name, Kind = JsonNodeKind.Number, Value = element.GetRawText() };

            case JsonValueKind.True:
            case JsonValueKind.False:
                return new JsonTreeNode { Name = name, Kind = JsonNodeKind.Boolean, Value = element.GetRawText() };

            default:
                return new JsonTreeNode { Name = name, Kind = JsonNodeKind.Null, Value = "null" };
        }
    }
}
