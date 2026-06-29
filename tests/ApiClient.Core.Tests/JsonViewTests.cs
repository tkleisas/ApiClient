using System.Linq;
using ApiClient.Core.Json;
using Xunit;

namespace ApiClient.Core.Tests;

public class JsonViewTests
{
    [Fact]
    public void Prettify_indents_compact_json()
    {
        var ok = JsonFormatter.TryPrettify("{\"a\":1,\"b\":[2,3]}", out var pretty);

        Assert.True(ok);
        Assert.Contains("\n", pretty);
        Assert.Contains("\"a\": 1", pretty);
    }

    [Fact]
    public void Prettify_returns_false_for_non_json()
    {
        var ok = JsonFormatter.TryPrettify("not json", out var pretty);

        Assert.False(ok);
        Assert.Equal("not json", pretty);
    }

    [Fact]
    public void Tree_parses_object_members_with_kinds()
    {
        var root = JsonTree.Parse("{\"name\":\"Ada\",\"age\":30,\"active\":true,\"note\":null}");

        Assert.Equal(JsonNodeKind.Object, root.Kind);
        Assert.Equal(JsonNodeKind.String, root.Children.Single(c => c.Name == "name").Kind);
        Assert.Equal("\"Ada\"", root.Children.Single(c => c.Name == "name").Value);
        Assert.Equal(JsonNodeKind.Number, root.Children.Single(c => c.Name == "age").Kind);
        Assert.Equal(JsonNodeKind.Boolean, root.Children.Single(c => c.Name == "active").Kind);
        Assert.Equal(JsonNodeKind.Null, root.Children.Single(c => c.Name == "note").Kind);
    }

    [Fact]
    public void Tree_parses_arrays_with_indexed_children()
    {
        var root = JsonTree.Parse("[10,20,30]");

        Assert.Equal(JsonNodeKind.Array, root.Kind);
        Assert.Equal(3, root.Children.Count);
        Assert.Equal("[0]", root.Children[0].Name);
        Assert.Equal("20", root.Children[1].Value);
    }
}
