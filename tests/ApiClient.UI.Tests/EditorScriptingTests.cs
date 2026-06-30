using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class EditorScriptingTests
{
    private sealed class CapturingSender(ApiResponse response) : IHttpSender
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }

    private static RequestEditorViewModel Editor(CapturingSender sender)
        => new RequestEditorViewModel(
            new RequestExecutor(HttpRequestFactory.CreateDefault(), sender),
            new StandaloneHostServices());

    [Fact]
    public async Task Pre_request_script_can_set_a_header_that_is_sent()
    {
        var sender = new CapturingSender(new ApiResponse { StatusCode = 200, IsSuccessStatusCode = true, Body = "{}" });
        var vm = Editor(sender);
        vm.Url = "https://h/api";
        vm.PreRequestScript = "req.setHeader('X-Sig', crypto.hmacSha256('message','key'));";

        await vm.SendCommand.ExecuteAsync(null);

        Assert.True(sender.LastRequest!.Headers.TryGetValues("X-Sig", out var values));
        Assert.Equal("6e9ef29b75fffc5b7abae527d58fdadb2fe42e7219011976917343065f58ed4a", values!.Single());
    }

    [Fact]
    public async Task Post_response_assertions_populate_test_results()
    {
        var sender = new CapturingSender(new ApiResponse { StatusCode = 201, IsSuccessStatusCode = true, Body = "{}" });
        var vm = Editor(sender);
        vm.Url = "https://h/api";
        vm.PostResponseScript = "test('created', function(){ expect(res.status).toBe(201); });";

        await vm.SendCommand.ExecuteAsync(null);

        var test = Assert.Single(vm.TestResults);
        Assert.True(test.Passed);
        Assert.Equal("created", test.Name);
    }

    [Fact]
    public async Task Extracted_variable_chains_into_the_next_request()
    {
        var sender = new CapturingSender(new ApiResponse { StatusCode = 200, IsSuccessStatusCode = true, Body = "{\"token\":\"T123\"}" });
        var vm = Editor(sender);

        // First request extracts a token from the response.
        vm.Url = "https://h/login";
        vm.PostResponseScript = "bru.setVar('authToken', JSON.parse(res.body).token);";
        await vm.SendCommand.ExecuteAsync(null);

        // Second request uses it via {{authToken}}.
        vm.PostResponseScript = string.Empty;
        vm.Url = "https://h/secure?t={{authToken}}";
        await vm.SendCommand.ExecuteAsync(null);

        Assert.Equal("https://h/secure?t=T123", sender.LastRequest!.RequestUri!.ToString());
    }
}
