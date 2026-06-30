using System.Collections.Generic;

namespace ApiClient.Core.Model;

/// <summary>
/// A named set of variables (e.g. Local / UAT / Prod) used to resolve <c>{{tokens}}</c> in
/// requests before they are sent. Named <c>ApiEnvironment</c> to avoid clashing with
/// <see cref="System.Environment"/>.
/// </summary>
public record ApiEnvironment
{
    /// <summary>Storage schema version. Currently <c>1</c>.</summary>
    public int Version { get; init; } = 1;

    /// <summary>The environment's display name.</summary>
    public required string Name { get; init; }

    /// <summary>The variables, in order. Disabled entries are kept but not applied.</summary>
    public IReadOnlyList<KeyValueItem> Variables { get; init; } = [];

    /// <summary>Builds a name→value map of the enabled variables (later entries win on duplicate names).</summary>
    public IReadOnlyDictionary<string, string> ToVariableMap()
    {
        var map = new Dictionary<string, string>();
        foreach (var variable in Variables)
        {
            if (variable.Enabled)
                map[variable.Name] = variable.Value;
        }

        return map;
    }
}
