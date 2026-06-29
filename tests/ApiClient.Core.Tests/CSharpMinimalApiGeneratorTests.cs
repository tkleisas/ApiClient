using ApiClient.Core.CodeGen;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class CSharpMinimalApiGeneratorTests
{
    private static CSharpMinimalApiGenerator Generator() => new CSharpMinimalApiGenerator();

    [Fact]
    public void Reports_its_identity_as_a_server_generator()
    {
        var generator = Generator();

        Assert.Equal("csharp-minimal-api", generator.Id);
        Assert.Equal(CodeGenScenario.Server, generator.Scenario);
        Assert.False(string.IsNullOrWhiteSpace(generator.DisplayName));
    }

    [Fact]
    public void Maps_get_to_MapGet_with_the_url_path()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Method = "GET", Url = "https://api.example.com/users/1" });

        Assert.Contains("app.MapGet(\"/users/1\"", code);
    }

    [Fact]
    public void Maps_post_to_MapPost()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Method = "POST", Url = "https://h/users" });

        Assert.Contains("app.MapPost(\"/users\"", code);
    }

    [Fact]
    public void Ignores_the_query_string_in_the_path()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Method = "GET", Url = "https://h/search?q=hi" });

        Assert.Contains("app.MapGet(\"/search\"", code);
        Assert.DoesNotContain("?q=hi", code);
    }

    [Fact]
    public void Uses_MapMethods_for_unknown_verbs()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Method = "PURGE", Url = "https://h/cache" });

        Assert.Contains("app.MapMethods(\"/cache\"", code);
        Assert.Contains("PURGE", code);
    }

    [Fact]
    public void Emits_a_handler_returning_a_result()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Method = "GET", Url = "https://h/ping" });

        Assert.Contains("Results.Ok()", code);
    }

    [Fact]
    public void Defaults_path_to_root_when_url_has_none()
    {
        var code = Generator().Generate(new ApiRequest { Name = "x", Method = "GET", Url = "https://h" });

        Assert.Contains("app.MapGet(\"/\"", code);
    }
}
