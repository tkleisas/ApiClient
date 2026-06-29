using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Model;

namespace ApiClient.Core.Http;

/// <summary>
/// Sends a prepared <see cref="HttpRequestMessage"/> and captures the result as an
/// <see cref="ApiResponse"/>. This is the one stage of the pipeline that touches the
/// network, kept behind an interface so the rest of the system (and its tests) never
/// depend on a live connection.
/// </summary>
public interface IHttpSender
{
    /// <summary>Sends <paramref name="request"/> and returns the captured response.</summary>
    Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}

/// <summary>
/// The default <see cref="IHttpSender"/>, backed by <see cref="HttpClient"/>. The
/// <see cref="HttpClient"/> is injected so callers control its lifetime, handler, and
/// configuration (proxy, timeouts, certificates), and so tests can supply a fake handler.
/// </summary>
public sealed class HttpClientSender(HttpClient client) : IHttpSender
{
    /// <inheritdoc/>
    public async Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return new ApiResponse
        {
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            Headers = CollectHeaders(response),
            Body = Encoding.UTF8.GetString(bytes),
            ContentType = response.Content.Headers.ContentType?.MediaType,
            SizeBytes = bytes.Length,
            Elapsed = stopwatch.Elapsed,
        };
    }

    private static IReadOnlyList<KeyValueItem> CollectHeaders(HttpResponseMessage response)
        => response.Headers
            .Concat(response.Content.Headers)
            .Select(h => new KeyValueItem(h.Key, string.Join(", ", h.Value)))
            .ToList();
}
