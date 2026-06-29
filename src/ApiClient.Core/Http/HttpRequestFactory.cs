using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using ApiClient.Core.Model;
using ApiClient.Core.Variables;

namespace ApiClient.Core.Http;

/// <summary>
/// Turns a stored <see cref="ApiRequest"/> plus a set of environment variables into a
/// ready-to-send <see cref="HttpRequestMessage"/>. This is the deterministic, network-free
/// part of the request pipeline: it resolves <c>{{variables}}</c>, applies authentication
/// (via pluggable <see cref="IAuthProvider"/>s), assembles the URL and query string, copies
/// headers, and builds the body. Actually sending the message is a separate concern.
/// </summary>
public sealed class HttpRequestFactory
{
    private readonly IReadOnlyDictionary<AuthType, IAuthProvider> _authProviders;
    private readonly VariableResolver _resolver;

    /// <summary>Creates a factory with the given auth providers and (optionally) a custom resolver.</summary>
    public HttpRequestFactory(IEnumerable<IAuthProvider> authProviders, VariableResolver? resolver = null)
    {
        _authProviders = authProviders.ToDictionary(p => p.AuthType);
        _resolver = resolver ?? new VariableResolver();
    }

    /// <summary>Creates a factory wired with the built-in auth providers (Bearer, Basic, API key).</summary>
    public static HttpRequestFactory CreateDefault()
        => new HttpRequestFactory([new BearerAuthProvider(), new BasicAuthProvider(), new ApiKeyAuthProvider()]);

    /// <summary>
    /// Builds an <see cref="HttpRequestMessage"/> for <paramref name="request"/>, resolving
    /// all <c>{{variables}}</c> against <paramref name="variables"/>.
    /// </summary>
    public HttpRequestMessage Create(ApiRequest request, IReadOnlyDictionary<string, string> variables)
    {
        ResolveValue resolve = template => template is null ? null : _resolver.Resolve(template, variables);

        var authContext = ApplyAuth(request.Auth, resolve);

        var headers = ResolveEnabled(request.Headers, resolve).Concat(authContext.Headers).ToList();
        var queryParams = ResolveEnabled(request.Query, resolve).Concat(authContext.QueryParams).ToList();

        var message = new HttpRequestMessage(new HttpMethod(request.Method), BuildUri(resolve(request.Url), queryParams));

        var content = BuildContent(request.Body, resolve);
        if (content is not null)
            message.Content = content;

        foreach (var header in headers)
            message.Headers.TryAddWithoutValidation(header.Name, header.Value);

        return message;
    }

    private AuthContext ApplyAuth(RequestAuth auth, ResolveValue resolve)
    {
        var context = new AuthContext();
        if (_authProviders.TryGetValue(auth.Type, out var provider))
            provider.Apply(auth, resolve, context);
        return context;
    }

    private static IEnumerable<KeyValueItem> ResolveEnabled(IReadOnlyList<KeyValueItem> items, ResolveValue resolve)
        => items
            .Where(item => item.Enabled)
            .Select(item => item with { Name = resolve(item.Name) ?? string.Empty, Value = resolve(item.Value) ?? string.Empty });

    private static Uri BuildUri(string? resolvedUrl, IReadOnlyList<KeyValueItem> queryParams)
    {
        var url = resolvedUrl ?? string.Empty;

        if (queryParams.Count > 0)
        {
            var query = string.Join("&", queryParams.Select(p =>
                $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value)}"));
            url += (url.Contains('?') ? "&" : "?") + query;
        }

        return new Uri(url, UriKind.RelativeOrAbsolute);
    }

    private static HttpContent? BuildContent(RequestBody body, ResolveValue resolve)
    {
        switch (body.Type)
        {
            case BodyType.Raw:
                var content = new StringContent(resolve(body.Text) ?? string.Empty, Encoding.UTF8);
                if (!string.IsNullOrEmpty(body.MediaType))
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(body.MediaType);
                return content;

            case BodyType.FormUrlEncoded:
                var fields = ResolveEnabled(body.Form, resolve)
                    .Select(f => new KeyValuePair<string, string>(f.Name, f.Value));
                return new FormUrlEncodedContent(fields);

            case BodyType.None:
            default:
                return null;
        }
    }
}
