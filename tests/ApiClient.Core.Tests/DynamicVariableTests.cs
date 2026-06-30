using System;
using System.Collections.Generic;
using ApiClient.Core.Variables;
using Xunit;

namespace ApiClient.Core.Tests;

public class DynamicVariableTests
{
    private static readonly Dictionary<string, string> None = new();

    [Fact]
    public void Resolves_guid_dynamic_variable()
    {
        var value = new VariableResolver().Resolve("{{$guid}}", None);

        Assert.True(Guid.TryParse(value, out _));
    }

    [Fact]
    public void Resolves_timestamp_dynamic_variable()
    {
        var value = new VariableResolver().Resolve("{{$timestamp}}", None);

        Assert.True(long.TryParse(value, out _));
    }

    [Fact]
    public void An_explicit_variable_still_takes_precedence_over_dynamic_names()
    {
        var value = new VariableResolver().Resolve("{{$guid}}", new Dictionary<string, string> { ["$guid"] = "fixed" });

        Assert.Equal("fixed", value);
    }
}
