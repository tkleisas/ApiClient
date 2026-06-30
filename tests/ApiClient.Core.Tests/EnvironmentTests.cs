using System;
using System.IO;
using System.Linq;
using ApiClient.Core.ImportExport;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using Xunit;

namespace ApiClient.Core.Tests;

public class EnvironmentTests
{
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ApiClientEnvTests", Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void To_variable_map_includes_only_enabled_entries()
    {
        var env = new ApiEnvironment
        {
            Name = "Local",
            Variables =
            [
                new KeyValueItem("baseUrl", "https://localhost"),
                new KeyValueItem("debug", "1", Enabled: false),
            ],
        };

        var map = env.ToVariableMap();

        Assert.Equal("https://localhost", map["baseUrl"]);
        Assert.False(map.ContainsKey("debug"));
    }

    [Fact]
    public void Environment_store_round_trips()
    {
        using var dir = new TempDir();
        new EnvironmentStore().Save(
            new ApiEnvironment { Name = "UAT", Variables = [new KeyValueItem("baseUrl", "https://uat")] },
            dir.Path);

        var loaded = new EnvironmentStore().Load(dir.Path);

        var uat = Assert.Single(loaded);
        Assert.Equal("UAT", uat.Name);
        Assert.Equal("https://uat", uat.ToVariableMap()["baseUrl"]);
    }

    [Fact]
    public void Environment_store_returns_empty_when_no_environments_folder()
    {
        using var dir = new TempDir();

        Assert.Empty(new EnvironmentStore().Load(dir.Path));
    }

    [Fact]
    public void Imports_bruno_environments_from_vars_blocks()
    {
        using var dir = new TempDir();
        var envFolder = Path.Combine(dir.Path, "environments");
        Directory.CreateDirectory(envFolder);
        File.WriteAllText(Path.Combine(envFolder, "Local.bru"), "vars {\n  baseUrl: https://localhost\n  ~off: x\n}");

        var environments = BrunoImporter.ImportEnvironments(dir.Path);

        var local = Assert.Single(environments);
        Assert.Equal("Local", local.Name);
        Assert.Equal("https://localhost", local.ToVariableMap()["baseUrl"]);
        Assert.False(local.ToVariableMap().ContainsKey("off"));
    }
}
