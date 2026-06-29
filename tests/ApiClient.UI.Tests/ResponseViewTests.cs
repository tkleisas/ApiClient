using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class ResponseViewTests
{
    private sealed class FakeSender(ApiResponse response) : IHttpSender
    {
        public Task<ApiResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            => Task.FromResult(response);
    }

    private static RequestEditorViewModel EditorReturning(string body)
    {
        var sender = new FakeSender(new ApiResponse { StatusCode = 200, IsSuccessStatusCode = true, Body = body });
        return new RequestEditorViewModel(
            new RequestExecutor(HttpRequestFactory.CreateDefault(), sender),
            new StandaloneHostServices());
    }

    [Fact]
    public async Task Json_response_is_prettified_and_built_into_a_tree()
    {
        var vm = EditorReturning("{\"a\":1,\"b\":[2,3]}");

        await vm.SendCommand.ExecuteAsync(null);

        Assert.True(vm.ResponseIsJson);
        Assert.Contains("\n", vm.ResponsePretty);
        Assert.Single(vm.ResponseTree);
    }

    [Fact]
    public async Task Non_json_response_is_not_marked_json_and_has_no_tree()
    {
        var vm = EditorReturning("plain text body");

        await vm.SendCommand.ExecuteAsync(null);

        Assert.False(vm.ResponseIsJson);
        Assert.Equal("plain text body", vm.ResponsePretty);
        Assert.Empty(vm.ResponseTree);
    }

    [Fact]
    public void Default_view_is_pretty_with_word_wrap_available()
    {
        var vm = new RequestEditorViewModel();

        Assert.True(vm.IsPrettyView);
        Assert.True(vm.IsTextResponseView);

        vm.SelectedResponseView = "Tree";
        Assert.True(vm.IsTreeView);
        Assert.False(vm.IsTextResponseView);
    }
}
