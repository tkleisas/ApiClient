using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Model;

namespace ApiClient.Core.Http;

/// <summary>
/// An <see cref="IHttpSender"/> that delegates to an inner sender which can be swapped at
/// runtime. This lets the app rebuild its HTTP client (e.g. after TLS settings change)
/// without recreating the components that hold the sender.
/// </summary>
public sealed class ReconfigurableHttpSender : IHttpSender
{
    private IHttpSender _inner;

    /// <summary>Creates the sender wrapping an initial <paramref name="inner"/> sender.</summary>
    public ReconfigurableHttpSender(IHttpSender inner) => _inner = inner;

    /// <summary>Replaces the inner sender used for subsequent sends.</summary>
    public void Set(IHttpSender inner) => _inner = inner;

    /// <inheritdoc/>
    public Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        => _inner.SendAsync(request, cancellationToken);
}
