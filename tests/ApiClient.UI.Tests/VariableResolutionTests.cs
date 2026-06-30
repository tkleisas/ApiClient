using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class VariableResolutionTests
{
    private sealed class CapturingSender : IHttpSender
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new ApiResponse { StatusCode = 200, IsSuccessStatusCode = true, Body = "{}" });
        }
    }

    [Fact]
    public async Task Sending_resolves_variables_from_the_active_environment()
    {
        var sender = new CapturingSender();
        var editor = new RequestEditorViewModel(
            new RequestExecutor(HttpRequestFactory.CreateDefault(), sender),
            new StandaloneHostServices())
        {
            Url = "{{baseUrl}}/users",
            Variables = new Dictionary<string, string> { ["baseUrl"] = "https://example.com" },
        };

        await editor.SendCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com/users", sender.LastRequest!.RequestUri!.ToString());
    }
}
