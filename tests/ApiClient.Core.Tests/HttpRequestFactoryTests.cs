using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class HttpRequestFactoryTests
{
    private static HttpRequestFactory Factory() => HttpRequestFactory.CreateDefault();

    private static Dictionary<string, string> Vars(params (string Key, string Value)[] pairs)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (key, value) in pairs)
            dict[key] = value;
        return dict;
    }

    private static string? Header(HttpRequestMessage message, string name)
        => message.Headers.TryGetValues(name, out var values)
            ? string.Join(",", values)
            : null;

    [Fact]
    public void Sets_method_and_resolves_url()
    {
        var request = new ApiRequest { Name = "x", Method = "DELETE", Url = "{{baseUrl}}/users/1" };

        var message = Factory().Create(request, Vars(("baseUrl", "https://api.example.com")));

        Assert.Equal(HttpMethod.Delete, message.Method);
        Assert.Equal("https://api.example.com/users/1", message.RequestUri!.ToString());
    }

    [Fact]
    public void Appends_enabled_query_params_resolves_values_and_skips_disabled()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "http://h/path",
            Query =
            [
                new KeyValueItem("a", "1"),
                new KeyValueItem("b", "2", Enabled: false),
                new KeyValueItem("c", "{{v}}"),
            ],
        };

        var message = Factory().Create(request, Vars(("v", "3")));

        Assert.Equal("http://h/path?a=1&c=3", message.RequestUri!.ToString());
    }

    [Fact]
    public void Adds_enabled_headers_resolves_them_and_skips_disabled()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "http://h/",
            Headers =
            [
                new KeyValueItem("Accept", "application/json"),
                new KeyValueItem("X-Secret", "nope", Enabled: false),
                new KeyValueItem("X-Trace", "{{t}}"),
            ],
        };

        var message = Factory().Create(request, Vars(("t", "abc")));

        Assert.Equal("application/json", Header(message, "Accept"));
        Assert.Equal("abc", Header(message, "X-Trace"));
        Assert.Null(Header(message, "X-Secret"));
    }

    [Fact]
    public async Task Builds_raw_body_with_media_type_and_resolved_text()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Method = "POST",
            Url = "http://h/",
            Body = new RequestBody { Type = BodyType.Raw, MediaType = "application/json", Text = "{\"id\":{{id}}}" },
        };

        var message = Factory().Create(request, Vars(("id", "7")));

        Assert.Equal("application/json", message.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("{\"id\":7}", await message.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Builds_form_url_encoded_body()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Method = "POST",
            Url = "http://h/",
            Body = new RequestBody
            {
                Type = BodyType.FormUrlEncoded,
                Form = [new KeyValueItem("k", "v"), new KeyValueItem("k2", "{{x}}")],
            },
        };

        var message = Factory().Create(request, Vars(("x", "y")));

        Assert.Equal("application/x-www-form-urlencoded", message.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("k=v&k2=y", await message.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Bearer_auth_adds_resolved_authorization_header()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "http://h/",
            Auth = new RequestAuth { Type = AuthType.Bearer, Token = "{{tok}}" },
        };

        var message = Factory().Create(request, Vars(("tok", "abc123")));

        Assert.Equal("Bearer abc123", Header(message, "Authorization"));
    }

    [Fact]
    public void Basic_auth_adds_base64_credentials()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "http://h/",
            Auth = new RequestAuth { Type = AuthType.Basic, Username = "user", Password = "pass" },
        };

        var message = Factory().Create(request, Vars());

        var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));
        Assert.Equal(expected, Header(message, "Authorization"));
    }

    [Fact]
    public void ApiKey_auth_in_header_adds_named_header()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "http://h/",
            Auth = new RequestAuth
            {
                Type = AuthType.ApiKey,
                ApiKeyName = "X-Api-Key",
                ApiKeyValue = "{{k}}",
                ApiKeyLocation = ApiKeyLocation.Header,
            },
        };

        var message = Factory().Create(request, Vars(("k", "secret")));

        Assert.Equal("secret", Header(message, "X-Api-Key"));
    }

    [Fact]
    public void ApiKey_auth_in_query_appends_query_param()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "http://h/path",
            Auth = new RequestAuth
            {
                Type = AuthType.ApiKey,
                ApiKeyName = "api_key",
                ApiKeyValue = "secret",
                ApiKeyLocation = ApiKeyLocation.Query,
            },
        };

        var message = Factory().Create(request, Vars());

        Assert.Equal("http://h/path?api_key=secret", message.RequestUri!.ToString());
    }

    [Fact]
    public void No_auth_adds_no_authorization_header()
    {
        var request = new ApiRequest { Name = "x", Url = "http://h/" };

        var message = Factory().Create(request, Vars());

        Assert.Null(Header(message, "Authorization"));
    }
}
