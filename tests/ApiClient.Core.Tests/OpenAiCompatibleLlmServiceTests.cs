using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Llm;
using Xunit;

namespace ApiClient.Core.Tests;

public class OpenAiCompatibleLlmServiceTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        public string? LastRequestBody;
        public HttpStatusCode StatusCode = HttpStatusCode.OK;
        public string ResponseBody = """
            {"choices":[{"message":{"role":"assistant","content":"pong"}}]}
            """;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private static LlmSettings EnabledSettings() => new()
    {
        Enabled = true,
        Endpoint = "https://example.test/v1",
        ApiKey = "secret-key",
        Model = "test-model",
        Temperature = 0.5
    };

    private static (OpenAiCompatibleLlmService Service, FakeHandler Handler) Create(LlmSettings? settings = null)
    {
        var handler = new FakeHandler();
        var service = new OpenAiCompatibleLlmService(settings ?? EnabledSettings(), new HttpClient(handler));
        return (service, handler);
    }

    [Fact]
    public void IsConfigured_RequiresEnabledEndpointAndModel()
    {
        Assert.False(new OpenAiCompatibleLlmService(new LlmSettings()).IsConfigured);
        Assert.False(new OpenAiCompatibleLlmService(new LlmSettings { Enabled = true, Endpoint = "", Model = "m" }).IsConfigured);
        Assert.True(new OpenAiCompatibleLlmService(EnabledSettings()).IsConfigured);
    }

    [Fact]
    public async Task ChatAsync_SendsPayloadAndExtractsContent()
    {
        var (service, handler) = Create();

        var reply = await service.ChatAsync("system prompt", "user prompt");

        Assert.Equal("pong", reply);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://example.test/v1/chat/completions", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;
        Assert.Equal("test-model", root.GetProperty("model").GetString());
        Assert.Equal(0.5, root.GetProperty("temperature").GetDouble());
        Assert.Equal("system prompt", root.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Fact]
    public async Task ChatAsync_HttpError_ThrowsFriendlyMessage()
    {
        var (service, handler) = Create();
        handler.StatusCode = HttpStatusCode.Unauthorized;
        handler.ResponseBody = """{"error":"invalid api key"}""";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync("s", "u"));

        Assert.Contains("401", ex.Message);
        Assert.Contains("invalid api key", ex.Message);
    }

    [Fact]
    public async Task ChatAsync_NotConfigured_Throws()
    {
        var service = new OpenAiCompatibleLlmService(new LlmSettings());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChatAsync("s", "u"));
    }
}
