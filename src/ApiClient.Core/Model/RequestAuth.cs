namespace ApiClient.Core.Model;

/// <summary>The authentication scheme applied to a request.</summary>
public enum AuthType
{
    /// <summary>No authentication is applied.</summary>
    None,

    /// <summary>HTTP Bearer token: adds <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    Bearer,

    /// <summary>HTTP Basic auth: adds <c>Authorization: Basic &lt;base64(user:pass)&gt;</c>.</summary>
    Basic,

    /// <summary>An API key sent as a header or query parameter (see <see cref="ApiKeyLocation"/>).</summary>
    ApiKey,
}

/// <summary>Where an API key is placed on the outgoing request.</summary>
public enum ApiKeyLocation
{
    /// <summary>Send the API key as a request header.</summary>
    Header,

    /// <summary>Send the API key as a query string parameter.</summary>
    Query,
}

/// <summary>
/// Describes how a request authenticates. The active fields depend on <see cref="Type"/>;
/// all string fields may contain <c>{{variables}}</c> so that secrets can be sourced
/// from a (git-ignored) environment rather than stored inline.
/// </summary>
public record RequestAuth
{
    /// <summary>Which authentication scheme to apply. Defaults to <see cref="AuthType.None"/>.</summary>
    public AuthType Type { get; init; } = AuthType.None;

    /// <summary>The token for <see cref="AuthType.Bearer"/>.</summary>
    public string? Token { get; init; }

    /// <summary>The username for <see cref="AuthType.Basic"/>.</summary>
    public string? Username { get; init; }

    /// <summary>The password for <see cref="AuthType.Basic"/>.</summary>
    public string? Password { get; init; }

    /// <summary>The key name (header or query parameter name) for <see cref="AuthType.ApiKey"/>.</summary>
    public string? ApiKeyName { get; init; }

    /// <summary>The key value for <see cref="AuthType.ApiKey"/>.</summary>
    public string? ApiKeyValue { get; init; }

    /// <summary>Whether the API key is sent as a header or query parameter. Defaults to <see cref="ApiKeyLocation.Header"/>.</summary>
    public ApiKeyLocation ApiKeyLocation { get; init; } = ApiKeyLocation.Header;
}
