using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class RequestExecutorTests
{
    /// <summary>An <see cref="IHttpSender"/> that records the message and returns a canned response.</summary>
    private sealed class FakeSender(Func<HttpRequestMessage, ApiResponse> responder) : IHttpSender
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    [Fact]
    public async Task Builds_resolved_request_then_sends_it_and_returns_the_response()
    {
        var sender = new FakeSender(_ => new ApiResponse { StatusCode = 200, IsSuccessStatusCode = true, Body = "ok" });
        var executor = new RequestExecutor(HttpRequestFactory.CreateDefault(), sender);

        var response = await executor.ExecuteAsync(
            new ApiRequest { Name = "x", Url = "{{base}}/ping" },
            new Dictionary<string, string> { ["base"] = "http://h" });

        Assert.Equal("http://h/ping", sender.LastRequest!.RequestUri!.ToString());
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("ok", response.Body);
    }
}
