using System;
using System.Collections.Generic;

namespace ApiClient.Core.Model;

/// <summary>
/// The captured outcome of sending an <see cref="ApiRequest"/>: the HTTP status, headers,
/// body, and client-side timing/size measurements. This is a plain, UI-agnostic snapshot
/// suitable for display, storage in history, or further processing (e.g. generating C#
/// records from a JSON body).
/// </summary>
public record ApiResponse
{
    /// <summary>The numeric HTTP status code (e.g. <c>200</c>, <c>404</c>).</summary>
    public required int StatusCode { get; init; }

    /// <summary>The HTTP reason phrase (e.g. <c>OK</c>, <c>Not Found</c>), if any.</summary>
    public string? ReasonPhrase { get; init; }

    /// <summary>Whether <see cref="StatusCode"/> is in the 2xx success range.</summary>
    public bool IsSuccessStatusCode { get; init; }

    /// <summary>The response headers (including content headers), in the order received.</summary>
    public IReadOnlyList<KeyValueItem> Headers { get; init; } = [];

    /// <summary>The response body decoded as text.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>The response media type (e.g. <c>application/json</c>), if present.</summary>
    public string? ContentType { get; init; }

    /// <summary>The size of the response body in bytes.</summary>
    public long SizeBytes { get; init; }

    /// <summary>The wall-clock time taken to send the request and read the response.</summary>
    public TimeSpan Elapsed { get; init; }
}
