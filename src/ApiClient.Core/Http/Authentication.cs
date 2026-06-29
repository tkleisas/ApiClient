using System;
using System.Collections.Generic;
using System.Text;
using ApiClient.Core.Model;

namespace ApiClient.Core.Http;

/// <summary>
/// Resolves a template string (which may contain <c>{{variables}}</c>) to its final value.
/// Returns <c>null</c> when given <c>null</c>.
/// </summary>
public delegate string? ResolveValue(string? template);

/// <summary>
/// Collects the header and query-parameter additions an <see cref="IAuthProvider"/> wants
/// to contribute to an outgoing request. Values added here are already variable-resolved.
/// The factory merges these into the request rather than letting providers mutate the
/// <see cref="System.Net.Http.HttpRequestMessage"/> directly, which keeps providers simple
/// and order-independent.
/// </summary>
public sealed class AuthContext
{
    /// <summary>Headers to add to the request.</summary>
    public List<KeyValueItem> Headers { get; } = [];

    /// <summary>Query string parameters to add to the request URL.</summary>
    public List<KeyValueItem> QueryParams { get; } = [];
}

/// <summary>
/// Applies an authentication scheme to an outgoing request. Implementations are pluggable:
/// new schemes are added by registering another provider, not by editing the factory.
/// </summary>
public interface IAuthProvider
{
    /// <summary>The scheme this provider handles.</summary>
    AuthType AuthType { get; }

    /// <summary>
    /// Contributes auth headers/query params for <paramref name="auth"/> into
    /// <paramref name="context"/>, using <paramref name="resolve"/> to expand any
    /// <c>{{variables}}</c> in the auth fields.
    /// </summary>
    void Apply(RequestAuth auth, ResolveValue resolve, AuthContext context);
}

/// <summary>Adds <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
public sealed class BearerAuthProvider : IAuthProvider
{
    /// <inheritdoc/>
    public AuthType AuthType => AuthType.Bearer;

    /// <inheritdoc/>
    public void Apply(RequestAuth auth, ResolveValue resolve, AuthContext context)
        => context.Headers.Add(new KeyValueItem("Authorization", $"Bearer {resolve(auth.Token)}"));
}

/// <summary>Adds <c>Authorization: Basic &lt;base64(user:pass)&gt;</c>.</summary>
public sealed class BasicAuthProvider : IAuthProvider
{
    /// <inheritdoc/>
    public AuthType AuthType => AuthType.Basic;

    /// <inheritdoc/>
    public void Apply(RequestAuth auth, ResolveValue resolve, AuthContext context)
    {
        var credentials = $"{resolve(auth.Username)}:{resolve(auth.Password)}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        context.Headers.Add(new KeyValueItem("Authorization", $"Basic {encoded}"));
    }
}

/// <summary>Adds an API key as either a header or a query parameter (per <see cref="RequestAuth.ApiKeyLocation"/>).</summary>
public sealed class ApiKeyAuthProvider : IAuthProvider
{
    /// <inheritdoc/>
    public AuthType AuthType => AuthType.ApiKey;

    /// <inheritdoc/>
    public void Apply(RequestAuth auth, ResolveValue resolve, AuthContext context)
    {
        var item = new KeyValueItem(resolve(auth.ApiKeyName) ?? string.Empty, resolve(auth.ApiKeyValue) ?? string.Empty);
        if (auth.ApiKeyLocation == ApiKeyLocation.Query)
            context.QueryParams.Add(item);
        else
            context.Headers.Add(item);
    }
}
