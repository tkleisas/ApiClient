using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class HttpClientSenderTests
{
    /// <summary>A test double that returns a canned response and records the request it saw.</summary>
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    private static (HttpClientSender Sender, StubHandler Handler) Make(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHandler(responder);
        return (new HttpClientSender(new HttpClient(handler)), handler);
    }

    private static string? HeaderValue(ApiResponse response, string name)
    {
        foreach (var h in response.Headers)
            if (string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))
                return h.Value;
        return null;
    }

    [Fact]
    public async Task Captures_status_code_reason_and_success_flag()
    {
        var (sender, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            ReasonPhrase = "Created",
            Content = new StringContent(""),
        });

        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.Equal(201, response.StatusCode);
        Assert.Equal("Created", response.ReasonPhrase);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Captures_body_text_and_size_in_bytes()
    {
        var (sender, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("hello"),
        });

        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.Equal("hello", response.Body);
        Assert.Equal(5, response.SizeBytes);
    }

    [Fact]
    public async Task Captures_content_type()
    {
        var (sender, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        });

        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.Equal("application/json", response.ContentType);
    }

    [Fact]
    public async Task Captures_response_headers()
    {
        var (sender, _) = Make(_ =>
        {
            var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };
            message.Headers.Add("X-Custom", "abc");
            return message;
        });

        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.Equal("abc", HeaderValue(response, "X-Custom"));
    }

    [Fact]
    public async Task Reports_non_negative_elapsed_time()
    {
        var (sender, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        var response = await sender.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://h/"));

        Assert.True(response.Elapsed >= TimeSpan.Zero);
    }

    [Fact]
    public async Task Sends_the_provided_request_unchanged()
    {
        var (sender, handler) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });

        await sender.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://h/x"));

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://h/x", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task Works_end_to_end_with_the_request_factory()
    {
        var request = new ApiRequest { Name = "ping", Url = "{{base}}/ping" };
        var message = HttpRequestFactory.CreateDefault()
            .Create(request, new Dictionary<string, string> { ["base"] = "http://h" });
        var (sender, handler) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("pong") });

        var response = await sender.SendAsync(message);

        Assert.Equal("http://h/ping", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("pong", response.Body);
    }
}
