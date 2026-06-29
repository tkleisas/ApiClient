using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApiClient.Core.Http;
using ApiClient.Core.Model;

namespace ApiClient.Core.CodeGen;

/// <summary>
/// Generates a self-contained C# snippet that sends a request using <see cref="System.Net.Http.HttpClient"/>.
/// Authentication is expanded through the same <see cref="IAuthProvider"/>s the runtime uses, so generated
/// code matches what the app actually sends. <c>{{variables}}</c> are left as-is in the emitted strings so
/// the user can substitute or templatize them.
/// </summary>
public sealed class CSharpHttpClientGenerator : ICodeGenerator
{
    private readonly IReadOnlyDictionary<AuthType, IAuthProvider> _authProviders;

    /// <summary>Creates a generator using the supplied auth providers.</summary>
    public CSharpHttpClientGenerator(IEnumerable<IAuthProvider> authProviders)
        => _authProviders = authProviders.ToDictionary(p => p.AuthType);

    /// <summary>Creates a generator wired with the built-in auth providers (Bearer, Basic, API key).</summary>
    public static CSharpHttpClientGenerator CreateDefault()
        => new CSharpHttpClientGenerator([new BearerAuthProvider(), new BasicAuthProvider(), new ApiKeyAuthProvider()]);

    /// <inheritdoc/>
    public string Id => "csharp-httpclient";

    /// <inheritdoc/>
    public string DisplayName => "C# — HttpClient";

    /// <inheritdoc/>
    public CodeGenScenario Scenario => CodeGenScenario.Client;

    /// <inheritdoc/>
    public string Generate(ApiRequest request)
    {
        // Leave {{variables}} untouched in the generated code.
        ResolveValue identity = template => template;

        var authContext = new AuthContext();
        if (_authProviders.TryGetValue(request.Auth.Type, out var provider))
            provider.Apply(request.Auth, identity, authContext);

        var headers = request.Headers.Where(h => h.Enabled).Concat(authContext.Headers).ToList();
        var queryParams = request.Query.Where(q => q.Enabled).Concat(authContext.QueryParams).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine("using var client = new HttpClient();");
        sb.AppendLine();
        sb.AppendLine($"using var request = new HttpRequestMessage({MethodExpression(request.Method)}, {Literal(BuildUrl(request.Url, queryParams))});");

        foreach (var header in headers)
            sb.AppendLine($"request.Headers.TryAddWithoutValidation({Literal(header.Name)}, {Literal(header.Value)});");

        AppendBody(sb, request.Body);

        sb.AppendLine();
        sb.AppendLine("using var response = await client.SendAsync(request);");
        sb.AppendLine("response.EnsureSuccessStatusCode();");
        sb.AppendLine();
        sb.AppendLine("var responseBody = await response.Content.ReadAsStringAsync();");
        sb.AppendLine("Console.WriteLine(responseBody);");

        return sb.ToString();
    }

    private static void AppendBody(StringBuilder sb, RequestBody body)
    {
        switch (body.Type)
        {
            case BodyType.Raw:
                var mediaType = string.IsNullOrEmpty(body.MediaType) ? null : $", Encoding.UTF8, {Literal(body.MediaType)}";
                sb.AppendLine($"request.Content = new StringContent({Literal(body.Text ?? string.Empty)}{mediaType});");
                break;

            case BodyType.FormUrlEncoded:
                sb.AppendLine("request.Content = new FormUrlEncodedContent(new[]");
                sb.AppendLine("{");
                foreach (var field in body.Form.Where(f => f.Enabled))
                    sb.AppendLine($"    new KeyValuePair<string, string>({Literal(field.Name)}, {Literal(field.Value)}),");
                sb.AppendLine("});");
                break;

            case BodyType.None:
            default:
                break;
        }
    }

    private static string BuildUrl(string url, IReadOnlyList<KeyValueItem> queryParams)
    {
        if (queryParams.Count == 0)
            return url;

        var query = string.Join("&", queryParams.Select(p => $"{p.Name}={p.Value}"));
        return url + (url.Contains('?') ? "&" : "?") + query;
    }

    private static string MethodExpression(string method) => method.ToUpperInvariant() switch
    {
        "GET" => "HttpMethod.Get",
        "POST" => "HttpMethod.Post",
        "PUT" => "HttpMethod.Put",
        "DELETE" => "HttpMethod.Delete",
        "PATCH" => "HttpMethod.Patch",
        "HEAD" => "HttpMethod.Head",
        "OPTIONS" => "HttpMethod.Options",
        _ => $"new HttpMethod({Literal(method)})",
    };

    /// <summary>Renders <paramref name="value"/> as a valid C# double-quoted string literal.</summary>
    private static string Literal(string value)
    {
        var sb = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _ => c.ToString(),
            });
        }
        return sb.Append('"').ToString();
    }
}
