using System;
using System.Text;
using ApiClient.Core.Model;

namespace ApiClient.Core.CodeGen;

/// <summary>
/// Generates a C# ASP.NET Core <em>minimal API</em> endpoint stub that implements the
/// request's method and URL path — the server side of "code for this request". The body
/// is a placeholder for the developer to fill in. <c>{{variables}}</c> in the URL are left
/// as-is in the route, to be edited by the developer.
/// </summary>
public sealed class CSharpMinimalApiGenerator : ICodeGenerator
{
    /// <inheritdoc/>
    public string Id => "csharp-minimal-api";

    /// <inheritdoc/>
    public string DisplayName => "C# — ASP.NET minimal API";

    /// <inheritdoc/>
    public CodeGenScenario Scenario => CodeGenScenario.Server;

    /// <inheritdoc/>
    public string Generate(ApiRequest request)
    {
        var path = PathOf(request.Url);
        var sb = new StringBuilder();
        sb.AppendLine($"// Minimal API endpoint for: {request.Method.ToUpperInvariant()} {path}");

        var mapMethod = MapMethodFor(request.Method);
        if (mapMethod is not null)
        {
            sb.AppendLine($"app.{mapMethod}({Literal(path)}, () =>");
        }
        else
        {
            sb.AppendLine($"app.MapMethods({Literal(path)}, new[] {{ {Literal(request.Method.ToUpperInvariant())} }}, () =>");
        }

        sb.AppendLine("{");
        sb.AppendLine("    // TODO: implement handler");
        sb.AppendLine("    return Results.Ok();");
        sb.AppendLine("});");

        return sb.ToString();
    }

    private static string? MapMethodFor(string method) => method.ToUpperInvariant() switch
    {
        "GET" => "MapGet",
        "POST" => "MapPost",
        "PUT" => "MapPut",
        "DELETE" => "MapDelete",
        "PATCH" => "MapPatch",
        _ => null,
    };

    private static string PathOf(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;

        // Relative or templated URL: strip any query string and ensure a leading slash.
        var queryIndex = url.IndexOf('?');
        var path = queryIndex >= 0 ? url[..queryIndex] : url;
        if (path.Length == 0)
            return "/";
        return path.StartsWith('/') ? path : "/" + path;
    }

    private static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
