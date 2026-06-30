using System.Text.Json;
using ApiClient.Core.Model;
using ApiClient.Core.Serialization;
using Xunit;

namespace ApiClient.Core.Tests;

public class RequestSerializerTests
{
    private static RequestSerializer Serializer() => new RequestSerializer();

    private static ApiRequest SampleRequest() => new ApiRequest
    {
        Name = "Get user",
        Method = "GET",
        Url = "{{baseUrl}}/users/{{id}}",
        Headers =
        [
            new KeyValueItem("Accept", "application/json"),
            new KeyValueItem("X-Trace", "1", Enabled: false),
        ],
        Query = [new KeyValueItem("verbose", "true")],
        Body = new RequestBody
        {
            Type = BodyType.Raw,
            MediaType = "application/json",
            Text = "{\"a\":1}",
        },
        Auth = new RequestAuth { Type = AuthType.Bearer, Token = "{{token}}" },
        Description = "Fetches a user by id",
    };

    [Fact]
    public void Round_trips_all_scalar_fields()
    {
        var original = SampleRequest();

        var restored = Serializer().Deserialize(Serializer().Serialize(original));

        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Method, restored.Method);
        Assert.Equal(original.Url, restored.Url);
        Assert.Equal(original.Description, restored.Description);
    }

    [Fact]
    public void Round_trips_headers_including_disabled_state()
    {
        var original = SampleRequest();

        var restored = Serializer().Deserialize(Serializer().Serialize(original));

        Assert.Equal(original.Headers, restored.Headers);
        Assert.False(restored.Headers[1].Enabled);
    }

    [Fact]
    public void Round_trips_query_body_and_auth()
    {
        var original = SampleRequest();

        var restored = Serializer().Deserialize(Serializer().Serialize(original));

        Assert.Equal(original.Query, restored.Query);
        Assert.Equal(original.Body.Type, restored.Body.Type);
        Assert.Equal(original.Body.MediaType, restored.Body.MediaType);
        Assert.Equal(original.Body.Text, restored.Body.Text);
        Assert.Equal(original.Body.Form, restored.Body.Form);
        Assert.Equal(original.Auth, restored.Auth);
    }

    [Fact]
    public void Emits_a_schema_version_field()
    {
        using var doc = JsonDocument.Parse(Serializer().Serialize(SampleRequest()));

        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());
    }

    [Fact]
    public void Uses_camelCase_property_names()
    {
        using var doc = JsonDocument.Parse(Serializer().Serialize(SampleRequest()));

        Assert.Equal("GET", doc.RootElement.GetProperty("method").GetString());
        Assert.Equal("{{baseUrl}}/users/{{id}}", doc.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public void Serializes_enums_as_strings()
    {
        using var doc = JsonDocument.Parse(Serializer().Serialize(SampleRequest()));

        Assert.Equal("Raw", doc.RootElement.GetProperty("body").GetProperty("type").GetString());
        Assert.Equal("Bearer", doc.RootElement.GetProperty("auth").GetProperty("type").GetString());
    }

    [Fact]
    public void Omits_null_optional_fields()
    {
        var request = new ApiRequest { Name = "Ping", Url = "http://localhost/health" };

        using var doc = JsonDocument.Parse(Serializer().Serialize(request));

        Assert.False(doc.RootElement.TryGetProperty("description", out _));
    }

    [Fact]
    public void Applies_sensible_defaults_to_a_new_request()
    {
        var request = new ApiRequest { Name = "X", Url = "http://x" };

        Assert.Equal("GET", request.Method);
        Assert.Empty(request.Headers);
        Assert.Empty(request.Query);
        Assert.Equal(BodyType.None, request.Body.Type);
        Assert.Equal(AuthType.None, request.Auth.Type);
    }

    [Fact]
    public void Ignores_unknown_fields_for_forward_compatibility()
    {
        const string json = """
        {
            "version": 1,
            "name": "Future",
            "url": "http://example.com",
            "somethingFromVersion99": { "nested": true }
        }
        """;

        var restored = Serializer().Deserialize(json);

        Assert.Equal("Future", restored.Name);
        Assert.Equal("GET", restored.Method);
        Assert.Equal(BodyType.None, restored.Body.Type);
    }

    [Fact]
    public void Round_trips_the_script()
    {
        var original = SampleRequest() with
        {
            Script = new RequestScript { PreRequest = "bru.setVar('a',1)", PostResponse = "test('ok', function(){})" },
        };

        var restored = Serializer().Deserialize(Serializer().Serialize(original));

        Assert.Equal("bru.setVar('a',1)", restored.Script.PreRequest);
        Assert.Equal("test('ok', function(){})", restored.Script.PostResponse);
    }

    [Fact]
    public void Missing_script_defaults_to_empty()
    {
        const string json = """{ "version": 1, "name": "X", "url": "http://x" }""";

        var restored = Serializer().Deserialize(json);

        Assert.Equal(string.Empty, restored.Script.PreRequest);
        Assert.Equal(string.Empty, restored.Script.PostResponse);
    }

    [Fact]
    public void Deserialize_throws_on_invalid_json()
    {
        Assert.ThrowsAny<JsonException>(() => Serializer().Deserialize("{ not valid"));
    }
}
