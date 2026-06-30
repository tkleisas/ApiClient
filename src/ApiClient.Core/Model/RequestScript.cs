namespace ApiClient.Core.Model;

/// <summary>
/// JavaScript scripts attached to a request: a pre-request script (runs before sending,
/// can modify the request and set variables) and a post-response script (runs after the
/// response, for chaining via <c>bru.setVar</c> and assertions via <c>test</c>/<c>expect</c>).
/// </summary>
public record RequestScript
{
    /// <summary>JavaScript run before the request is sent.</summary>
    public string PreRequest { get; init; } = string.Empty;

    /// <summary>JavaScript run after the response is received.</summary>
    public string PostResponse { get; init; } = string.Empty;
}
