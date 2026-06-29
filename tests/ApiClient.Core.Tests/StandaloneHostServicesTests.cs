using System.IO;
using ApiClient.Core.Hosting;
using Xunit;

namespace ApiClient.Core.Tests;

public class StandaloneHostServicesTests
{
    [Fact]
    public void Collections_root_is_a_rooted_path_by_default()
    {
        var host = new StandaloneHostServices();

        Assert.True(Path.IsPathRooted(host.CollectionsRoot));
    }

    [Fact]
    public void Collections_root_can_be_overridden()
    {
        var host = new StandaloneHostServices("/tmp/my-collections");

        Assert.Equal("/tmp/my-collections", host.CollectionsRoot);
    }

    [Fact]
    public void Reporting_status_records_the_last_message()
    {
        var host = new StandaloneHostServices();

        host.ReportStatus("200 OK");

        Assert.Equal("200 OK", host.LastStatus);
    }
}
