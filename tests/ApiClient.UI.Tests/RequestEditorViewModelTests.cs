using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class RequestEditorViewModelTests
{
    [Fact]
    public void LoadFrom_maps_method_url_headers_and_raw_body()
    {
        var vm = new RequestEditorViewModel();

        vm.LoadFrom(new ApiRequest
        {
            Name = "X",
            Method = "POST",
            Url = "https://h/x",
            Headers =
            [
                new KeyValueItem("Accept", "application/json"),
                new KeyValueItem("X-Off", "v", Enabled: false),
            ],
            Body = new RequestBody { Type = BodyType.Raw, MediaType = "text/plain", Text = "hello" },
        });

        Assert.Equal("POST", vm.Method);
        Assert.Equal("https://h/x", vm.Url);
        Assert.Equal("hello", vm.RequestBody);
        Assert.Equal("text/plain", vm.BodyMediaType);
        Assert.Equal(2, vm.Headers.Count);
        Assert.Equal("Accept", vm.Headers[0].Name);
        Assert.False(vm.Headers[1].Enabled);
    }

    [Fact]
    public void LoadFrom_clears_body_when_request_has_none()
    {
        var vm = new RequestEditorViewModel { RequestBody = "leftover" };

        vm.LoadFrom(new ApiRequest { Name = "X", Url = "https://h/x" });

        Assert.Equal(string.Empty, vm.RequestBody);
    }

    [Fact]
    public void LoadFrom_replaces_previously_loaded_headers()
    {
        var vm = new RequestEditorViewModel();
        vm.LoadFrom(new ApiRequest { Name = "A", Url = "https://h/a", Headers = [new KeyValueItem("First", "1")] });

        vm.LoadFrom(new ApiRequest { Name = "B", Url = "https://h/b", Headers = [new KeyValueItem("Second", "2")] });

        Assert.Single(vm.Headers);
        Assert.Equal("Second", vm.Headers[0].Name);
    }
}
