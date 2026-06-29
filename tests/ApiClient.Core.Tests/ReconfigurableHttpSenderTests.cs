using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class ReconfigurableHttpSenderTests
{
    private sealed class TaggedSender(string tag) : IHttpSender
    {
        public Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ApiResponse { StatusCode = 200, Body = tag });
    }

    [Fact]
    public async Task Delegates_to_the_initial_inner_sender()
    {
        var sender = new ReconfigurableHttpSender(new TaggedSender("first"));

        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.Equal("first", response.Body);
    }

    [Fact]
    public async Task Uses_the_replacement_after_set()
    {
        var sender = new ReconfigurableHttpSender(new TaggedSender("first"));

        sender.Set(new TaggedSender("second"));
        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.Equal("second", response.Body);
    }
}
