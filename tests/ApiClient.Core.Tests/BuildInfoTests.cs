using ApiClient.Core;
using Xunit;

namespace ApiClient.Core.Tests;

public class BuildInfoTests
{
    [Fact]
    public void Version_is_a_non_empty_string()
    {
        Assert.False(string.IsNullOrWhiteSpace(BuildInfo.Version));
    }

    [Fact]
    public void Version_does_not_include_build_metadata()
    {
        Assert.DoesNotContain('+', BuildInfo.Version);
    }
}
