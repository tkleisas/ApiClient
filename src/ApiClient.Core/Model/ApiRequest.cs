using System.Collections.Generic;

namespace ApiClient.Core.Model;

/// <summary>
/// A single API request — the fundamental unit a user edits and sends, and the unit
/// of on-disk storage (one request per file). Values throughout may contain
/// <c>{{variables}}</c> which are resolved against the active environment before sending.
/// </summary>
public record ApiRequest
{
    /// <summary>
    /// The storage schema version of this request. Bumped only on breaking format
    /// changes so that older files can be migrated rather than rejected.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>The display name of the request (also the basis for its file name).</summary>
    public required string Name { get; init; }

    /// <summary>The HTTP method, e.g. <c>GET</c>, <c>POST</c>. A string (not an enum) so custom methods are allowed. Defaults to <c>GET</c>.</summary>
    public string Method { get; init; } = "GET";

    /// <summary>The request URL, including scheme and (optionally) query string. May contain <c>{{variables}}</c>.</summary>
    public required string Url { get; init; }

    /// <summary>The HTTP headers to send, in order. Disabled entries are kept but not sent.</summary>
    public IReadOnlyList<KeyValueItem> Headers { get; init; } = [];

    /// <summary>
    /// Query string parameters, in order. These are conceptually appended to <see cref="Url"/>;
    /// keeping them as a structured list (rather than only in the URL) makes them editable in a grid.
    /// </summary>
    public IReadOnlyList<KeyValueItem> Query { get; init; } = [];

    /// <summary>The request body. Defaults to an empty <see cref="BodyType.None"/> body.</summary>
    public RequestBody Body { get; init; } = new RequestBody();

    /// <summary>The authentication applied to the request. Defaults to <see cref="AuthType.None"/>.</summary>
    public RequestAuth Auth { get; init; } = new RequestAuth();

    /// <summary>Optional free-form documentation for the request.</summary>
    public string? Description { get; init; }

    /// <summary>Declarative post-response scripting (extractions and assertions).</summary>
    public RequestScript Script { get; init; } = new RequestScript();
}
