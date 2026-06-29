using System;
using System.IO;
using System.Linq;
using ApiClient.Core.ImportExport;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class BrunoImporterTests
{
    private const string GetUserBru = """
        meta {
          name: Get User
          type: http
          seq: 1
        }

        get {
          url: {{baseUrl}}/users/1
          body: json
          auth: bearer
        }

        params:query {
          verbose: true
          ~debug: false
        }

        headers {
          Accept: application/json
          ~X-Trace: 1
        }

        auth:bearer {
          token: {{token}}
        }

        body:json {
          {
            "name": "Ada"
          }
        }
        """;

    [Fact]
    public void Parses_name_method_and_url()
    {
        var request = BrunoImporter.ParseRequest(GetUserBru);

        Assert.Equal("Get User", request.Name);
        Assert.Equal("GET", request.Method);
        Assert.Equal("{{baseUrl}}/users/1", request.Url);
    }

    [Fact]
    public void Parses_headers_including_disabled()
    {
        var request = BrunoImporter.ParseRequest(GetUserBru);

        Assert.Equal("application/json", request.Headers.Single(h => h.Name == "Accept").Value);
        Assert.False(request.Headers.Single(h => h.Name == "X-Trace").Enabled);
    }

    [Fact]
    public void Parses_query_params_including_disabled()
    {
        var request = BrunoImporter.ParseRequest(GetUserBru);

        Assert.True(request.Query.Single(q => q.Name == "verbose").Enabled);
        Assert.False(request.Query.Single(q => q.Name == "debug").Enabled);
    }

    [Fact]
    public void Parses_bearer_auth()
    {
        var request = BrunoImporter.ParseRequest(GetUserBru);

        Assert.Equal(AuthType.Bearer, request.Auth.Type);
        Assert.Equal("{{token}}", request.Auth.Token);
    }

    [Fact]
    public void Parses_json_body_with_media_type_and_content()
    {
        var request = BrunoImporter.ParseRequest(GetUserBru);

        Assert.Equal(BodyType.Raw, request.Body.Type);
        Assert.Equal("application/json", request.Body.MediaType);
        Assert.Contains("\"name\": \"Ada\"", request.Body.Text);
    }

    [Fact]
    public void Parses_post_with_form_urlencoded_body()
    {
        const string bru = """
            meta {
              name: Create
            }
            post {
              url: https://h/users
              body: form-urlencoded
              auth: none
            }
            body:form-urlencoded {
              name: Ada
              ~role: admin
            }
            """;

        var request = BrunoImporter.ParseRequest(bru);

        Assert.Equal("POST", request.Method);
        Assert.Equal(BodyType.FormUrlEncoded, request.Body.Type);
        Assert.Equal("Ada", request.Body.Form.Single(f => f.Name == "name").Value);
        Assert.False(request.Body.Form.Single(f => f.Name == "role").Enabled);
    }

    [Fact]
    public void Parses_basic_auth()
    {
        const string bru = """
            meta { name: B }
            get {
              url: https://h
              auth: basic
            }
            auth:basic {
              username: user
              password: pass
            }
            """;

        var request = BrunoImporter.ParseRequest(bru);

        Assert.Equal(AuthType.Basic, request.Auth.Type);
        Assert.Equal("user", request.Auth.Username);
        Assert.Equal("pass", request.Auth.Password);
    }

    [Fact]
    public void Parses_api_key_auth_in_query()
    {
        const string bru = """
            meta { name: K }
            get {
              url: https://h
              auth: apikey
            }
            auth:apikey {
              key: api_key
              value: secret
              placement: queryparams
            }
            """;

        var request = BrunoImporter.ParseRequest(bru);

        Assert.Equal(AuthType.ApiKey, request.Auth.Type);
        Assert.Equal("api_key", request.Auth.ApiKeyName);
        Assert.Equal("secret", request.Auth.ApiKeyValue);
        Assert.Equal(ApiKeyLocation.Query, request.Auth.ApiKeyLocation);
    }

    [Fact]
    public void Throws_when_there_is_no_method_block()
    {
        const string notARequest = """
            vars {
              baseUrl: https://h
            }
            """;

        Assert.Throws<FormatException>(() => BrunoImporter.ParseRequest(notARequest));
    }

    [Fact]
    public void Import_collection_walks_bru_files_into_a_tree()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrunoImportTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Users"));
        try
        {
            File.WriteAllText(Path.Combine(root, "health.bru"), SimpleGetBru("Health", "https://h/health"));
            File.WriteAllText(Path.Combine(root, "Users", "get-user.bru"), SimpleGetBru("Get User", "https://h/users/1"));

            var collection = BrunoImporter.ImportCollection(root);

            Assert.Equal("https://h/health", collection.Requests.Single(r => r.Name == "Health").Url);
            var users = collection.Folders.Single(f => f.Name == "Users");
            Assert.Equal("https://h/users/1", users.Requests.Single(r => r.Name == "Get User").Url);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Import_collection_prunes_folders_with_no_requests()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrunoImportTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "environments"));
        Directory.CreateDirectory(Path.Combine(root, "empty"));
        try
        {
            File.WriteAllText(Path.Combine(root, "ping.bru"), SimpleGetBru("Ping", "https://h/ping"));
            File.WriteAllText(Path.Combine(root, "environments", "env.bru"), "vars {\n  x: 1\n}");

            var collection = BrunoImporter.ImportCollection(root);

            Assert.Empty(collection.Folders);
            Assert.Single(collection.Requests);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string SimpleGetBru(string name, string url) => $$"""
        meta {
          name: {{name}}
          type: http
        }
        get {
          url: {{url}}
          auth: none
        }
        """;
}
