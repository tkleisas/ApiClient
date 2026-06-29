using System.Collections.Generic;
using ApiClient.Core.Variables;
using Xunit;

namespace ApiClient.Core.Tests;

public class VariableResolverTests
{
    private static VariableResolver Resolver() => new VariableResolver();

    [Fact]
    public void Substitutes_a_known_variable()
    {
        var vars = new Dictionary<string, string> { ["baseUrl"] = "https://api.example.com" };

        var result = Resolver().Resolve("{{baseUrl}}/users", vars);

        Assert.Equal("https://api.example.com/users", result);
    }

    [Fact]
    public void Substitutes_multiple_occurrences_of_the_same_variable()
    {
        var vars = new Dictionary<string, string> { ["host"] = "localhost" };

        var result = Resolver().Resolve("{{host}}:{{host}}", vars);

        Assert.Equal("localhost:localhost", result);
    }

    [Fact]
    public void Substitutes_several_different_variables()
    {
        var vars = new Dictionary<string, string>
        {
            ["scheme"] = "https",
            ["host"] = "example.com",
            ["version"] = "v2",
        };

        var result = Resolver().Resolve("{{scheme}}://{{host}}/{{version}}/ping", vars);

        Assert.Equal("https://example.com/v2/ping", result);
    }

    [Fact]
    public void Trims_whitespace_inside_the_braces()
    {
        var vars = new Dictionary<string, string> { ["token"] = "abc123" };

        var result = Resolver().Resolve("Bearer {{ token }}", vars);

        Assert.Equal("Bearer abc123", result);
    }

    [Fact]
    public void Leaves_unknown_tokens_untouched()
    {
        var vars = new Dictionary<string, string> { ["known"] = "yes" };

        var result = Resolver().Resolve("{{known}} {{unknown}}", vars);

        Assert.Equal("yes {{unknown}}", result);
    }

    [Fact]
    public void Returns_input_unchanged_when_no_tokens_present()
    {
        var result = Resolver().Resolve("https://example.com/health", new Dictionary<string, string>());

        Assert.Equal("https://example.com/health", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Handles_null_or_empty_template(string? template)
    {
        var result = Resolver().Resolve(template, new Dictionary<string, string> { ["x"] = "y" });

        Assert.Equal(template ?? string.Empty, result);
    }

    [Fact]
    public void Reports_unresolved_variable_names()
    {
        var vars = new Dictionary<string, string> { ["a"] = "1" };

        var result = Resolver().ResolveDetailed("{{a}}/{{b}}/{{c}}", vars);

        Assert.Equal("1/{{b}}/{{c}}", result.Value);
        Assert.Equal(new[] { "b", "c" }, result.UnresolvedNames);
    }
}
