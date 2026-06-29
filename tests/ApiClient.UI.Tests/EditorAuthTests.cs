using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class EditorAuthTests
{
    [Fact]
    public void LoadFrom_then_ToRequest_round_trips_bearer_auth()
    {
        var vm = new RequestEditorViewModel();

        vm.LoadFrom(new ApiRequest
        {
            Name = "x",
            Url = "https://h",
            Auth = new RequestAuth { Type = AuthType.Bearer, Token = "tok-123" },
        });

        Assert.Equal(AuthType.Bearer, vm.SelectedAuthType);
        Assert.Equal("tok-123", vm.AuthToken);
        Assert.True(vm.IsBearerAuth);

        var rebuilt = vm.ToRequest();
        Assert.Equal(AuthType.Bearer, rebuilt.Auth.Type);
        Assert.Equal("tok-123", rebuilt.Auth.Token);
    }

    [Fact]
    public void Bearer_token_is_sent_as_authorization_header()
    {
        // A bearer-auth request, once built, must produce an Authorization header
        // through the same factory the runtime uses — i.e. it is actually sent.
        var vm = new RequestEditorViewModel();
        vm.LoadFrom(new ApiRequest
        {
            Name = "x",
            Url = "https://h/api",
            Auth = new RequestAuth { Type = AuthType.Bearer, Token = "abc" },
        });

        var message = ApiClient.Core.Http.HttpRequestFactory.CreateDefault()
            .Create(vm.ToRequest(), new System.Collections.Generic.Dictionary<string, string>());

        Assert.True(message.Headers.TryGetValues("Authorization", out var values));
        Assert.Equal("Bearer abc", string.Join(",", values!));
    }

    [Fact]
    public void Round_trips_api_key_auth()
    {
        var vm = new RequestEditorViewModel();

        vm.LoadFrom(new ApiRequest
        {
            Name = "x",
            Url = "https://h",
            Auth = new RequestAuth
            {
                Type = AuthType.ApiKey,
                ApiKeyName = "X-Api-Key",
                ApiKeyValue = "secret",
                ApiKeyLocation = ApiKeyLocation.Query,
            },
        });

        Assert.True(vm.IsApiKeyAuth);
        var rebuilt = vm.ToRequest().Auth;
        Assert.Equal("X-Api-Key", rebuilt.ApiKeyName);
        Assert.Equal("secret", rebuilt.ApiKeyValue);
        Assert.Equal(ApiKeyLocation.Query, rebuilt.ApiKeyLocation);
    }
}
