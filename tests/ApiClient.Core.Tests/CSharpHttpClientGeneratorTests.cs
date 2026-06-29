using System;
using System.Text;
using ApiClient.Core.CodeGen;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class CSharpHttpClientGeneratorTests
{
    private static CSharpHttpClientGenerator Generator() => CSharpHttpClientGenerator.CreateDefault();

    [Fact]
    public void Reports_its_identity_as_a_client_generator()
    {
        var generator = Generator();

        Assert.Equal("csharp-httpclient", generator.Id);
        Assert.Equal(CodeGenScenario.Client, generator.Scenario);
        Assert.False(string.IsNullOrWhiteSpace(generator.DisplayName));
    }

    [Fact]
    public void Emits_required_usings_and_send_scaffold()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Url = "https://h/" });

        Assert.Contains("using System.Net.Http;", code);
        Assert.Contains("new HttpClient()", code);
        Assert.Contains("await client.SendAsync(request)", code);
        Assert.Contains("ReadAsStringAsync()", code);
    }

    [Fact]
    public void Maps_method_and_url()
    {
        var request = new ApiRequest { Name = "x", Method = "GET", Url = "https://api.example.com/users/1" };

        var code = Generator().Generate(request);

        Assert.Contains("new HttpRequestMessage(HttpMethod.Get, \"https://api.example.com/users/1\")", code);
    }

    [Fact]
    public void Uses_custom_method_constructor_for_unknown_verbs()
    {
        var request = new ApiRequest { Name = "x", Method = "PURGE", Url = "https://h/" };

        var code = Generator().Generate(request);

        Assert.Contains("new HttpMethod(\"PURGE\")", code);
    }

    [Fact]
    public void Appends_enabled_query_params_to_the_url_and_skips_disabled()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "https://h/path",
            Query =
            [
                new KeyValueItem("a", "1"),
                new KeyValueItem("b", "2", Enabled: false),
                new KeyValueItem("c", "{{v}}"),
            ],
        };

        var code = Generator().Generate(request);

        Assert.Contains("\"https://h/path?a=1&c={{v}}\"", code);
    }

    [Fact]
    public void Emits_enabled_headers_and_skips_disabled()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "https://h/",
            Headers =
            [
                new KeyValueItem("Accept", "application/json"),
                new KeyValueItem("X-Secret", "nope", Enabled: false),
            ],
        };

        var code = Generator().Generate(request);

        Assert.Contains("request.Headers.TryAddWithoutValidation(\"Accept\", \"application/json\");", code);
        Assert.DoesNotContain("X-Secret", code);
    }

    [Fact]
    public void Emits_raw_body_with_escaped_text_and_media_type()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Method = "POST",
            Url = "https://h/",
            Body = new RequestBody { Type = BodyType.Raw, MediaType = "application/json", Text = "{\"a\":1}" },
        };

        var code = Generator().Generate(request);

        Assert.Contains("new StringContent(\"{\\\"a\\\":1}\", Encoding.UTF8, \"application/json\")", code);
    }

    [Fact]
    public void Emits_form_url_encoded_body()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Method = "POST",
            Url = "https://h/",
            Body = new RequestBody
            {
                Type = BodyType.FormUrlEncoded,
                Form = [new KeyValueItem("k", "v"), new KeyValueItem("k2", "v2")],
            },
        };

        var code = Generator().Generate(request);

        Assert.Contains("new FormUrlEncodedContent(", code);
        Assert.Contains("new KeyValuePair<string, string>(\"k\", \"v\")", code);
        Assert.Contains("new KeyValuePair<string, string>(\"k2\", \"v2\")", code);
    }

    [Fact]
    public void Emits_bearer_authorization_header()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "https://h/",
            Auth = new RequestAuth { Type = AuthType.Bearer, Token = "{{token}}" },
        };

        var code = Generator().Generate(request);

        Assert.Contains("request.Headers.TryAddWithoutValidation(\"Authorization\", \"Bearer {{token}}\");", code);
    }

    [Fact]
    public void Emits_basic_authorization_header_with_base64()
    {
        var request = new ApiRequest
        {
            Name = "x",
            Url = "https://h/",
            Auth = new RequestAuth { Type = AuthType.Basic, Username = "user", Password = "pass" },
        };

        var code = Generator().Generate(request);

        var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));
        Assert.Contains($"request.Headers.TryAddWithoutValidation(\"Authorization\", \"{expected}\");", code);
    }

    [Fact]
    public void No_auth_emits_no_authorization_header()
    {
        var request = new ApiRequest { Name = "x", Url = "https://h/" };

        var code = Generator().Generate(request);

        Assert.DoesNotContain("Authorization", code);
    }
}
