using System;

namespace ApiClient.Core.Model;

/// <summary>One recorded send in the request history (a summary suitable for a list/grid).</summary>
public record HistoryEntry
{
    /// <summary>When the request was sent.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>The HTTP method.</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>The resolved request URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>The response status code (0 if the send failed before a response).</summary>
    public int Status { get; init; }

    /// <summary>Round-trip time in milliseconds.</summary>
    public long ElapsedMs { get; init; }

    /// <summary>Response body size in bytes.</summary>
    public long SizeBytes { get; init; }
}
