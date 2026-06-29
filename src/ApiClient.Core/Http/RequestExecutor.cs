using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Model;

namespace ApiClient.Core.Http;

/// <summary>
/// Runs the full send path for a request: builds an <see cref="System.Net.Http.HttpRequestMessage"/>
/// (resolving variables and applying auth) via a <see cref="HttpRequestFactory"/>, then sends it
/// through an <see cref="IHttpSender"/>. This is the single entry point the UI and a future CLI
/// runner call, keeping them ignorant of the individual pipeline stages.
/// </summary>
public sealed class RequestExecutor(HttpRequestFactory factory, IHttpSender sender)
{
    /// <summary>Builds and sends <paramref name="request"/>, resolving <c>{{variables}}</c> against <paramref name="variables"/>.</summary>
    public Task<ApiResponse> ExecuteAsync(
        ApiRequest request,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
        => sender.SendAsync(factory.Create(request, variables), cancellationToken);
}
