using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ApiClient.Core.Variables;

/// <summary>
/// Substitutes <c>{{name}}</c> tokens in a template string with values drawn from
/// a set of variables. Unknown tokens are left untouched so that partially-resolved
/// templates remain visible to the user rather than silently vanishing.
/// </summary>
public sealed partial class VariableResolver
{
    [GeneratedRegex(@"\{\{\s*(?<name>[^{}]+?)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    /// <summary>Resolves <paramref name="template"/> against <paramref name="variables"/>.</summary>
    public string Resolve(string? template, IReadOnlyDictionary<string, string> variables)
        => ResolveDetailed(template, variables).Value;

    /// <summary>
    /// Resolves <paramref name="template"/> and additionally reports which referenced
    /// variable names could not be resolved (in order of first appearance, distinct).
    /// </summary>
    public ResolutionResult ResolveDetailed(string? template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
            return new ResolutionResult(template ?? string.Empty, []);

        List<string>? unresolved = null;

        var value = TokenRegex().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            if (variables.TryGetValue(name, out var replacement))
                return replacement;

            unresolved ??= [];
            if (!unresolved.Contains(name))
                unresolved.Add(name);
            return match.Value;
        });

        return new ResolutionResult(value, unresolved ?? (IReadOnlyList<string>)[]);
    }
}

/// <summary>The outcome of resolving a template: the substituted text plus any unresolved names.</summary>
public sealed record ResolutionResult(string Value, IReadOnlyList<string> UnresolvedNames);
