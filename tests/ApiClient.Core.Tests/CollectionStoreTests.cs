using System;
using System.IO;
using System.Linq;
using ApiClient.Core.Model;
using ApiClient.Core.Serialization;
using ApiClient.Core.Storage;
using Xunit;

namespace ApiClient.Core.Tests;

public class CollectionStoreTests
{
    /// <summary>A throwaway directory that deletes itself at the end of a test.</summary>
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ApiClientTests", Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    private static CollectionStore Store() => new CollectionStore();

    [Fact]
    public void Save_then_load_round_trips_the_collection_name()
    {
        using var dir = new TempDir();
        Store().Save(new Collection { Name = "My API" }, dir.Path);

        var loaded = Store().Load(dir.Path);

        Assert.Equal("My API", loaded.Name);
    }

    [Fact]
    public void Save_then_load_round_trips_root_level_requests()
    {
        using var dir = new TempDir();
        var collection = new Collection
        {
            Name = "API",
            Requests =
            [
                new ApiRequest { Name = "Health", Url = "https://h/health" },
                new ApiRequest { Name = "Version", Url = "https://h/version" },
            ],
        };
        Store().Save(collection, dir.Path);

        var loaded = Store().Load(dir.Path);

        Assert.Equal(2, loaded.Requests.Count);
        Assert.Equal("https://h/health", loaded.Requests.Single(r => r.Name == "Health").Url);
        Assert.Equal("https://h/version", loaded.Requests.Single(r => r.Name == "Version").Url);
    }

    [Fact]
    public void Save_then_load_round_trips_nested_folders()
    {
        using var dir = new TempDir();
        var collection = new Collection
        {
            Name = "API",
            Requests = [new ApiRequest { Name = "Health", Url = "https://h/health" }],
            Folders =
            [
                new CollectionFolder
                {
                    Name = "Users",
                    Requests = [new ApiRequest { Name = "Get User", Url = "https://h/users/1" }],
                    Folders =
                    [
                        new CollectionFolder
                        {
                            Name = "Admin",
                            Requests = [new ApiRequest { Name = "Ban User", Url = "https://h/users/1/ban" }],
                        },
                    ],
                },
            ],
        };
        Store().Save(collection, dir.Path);

        var loaded = Store().Load(dir.Path);

        Assert.Single(loaded.Requests);
        var users = loaded.Folders.Single(f => f.Name == "Users");
        Assert.Equal("https://h/users/1", users.Requests.Single(r => r.Name == "Get User").Url);
        var admin = users.Folders.Single(f => f.Name == "Admin");
        Assert.Equal("https://h/users/1/ban", admin.Requests.Single(r => r.Name == "Ban User").Url);
    }

    [Fact]
    public void Save_writes_a_collection_manifest()
    {
        using var dir = new TempDir();

        Store().Save(new Collection { Name = "API" }, dir.Path);

        Assert.True(File.Exists(dir.Combine("collection.json")));
    }

    [Fact]
    public void Save_names_request_files_from_the_request_name()
    {
        using var dir = new TempDir();

        Store().Save(new Collection { Name = "API", Requests = [new ApiRequest { Name = "Get User", Url = "https://h/u" }] }, dir.Path);

        Assert.True(File.Exists(dir.Combine("Get User.req.json")));
    }

    [Fact]
    public void Save_sanitizes_invalid_characters_in_file_names()
    {
        using var dir = new TempDir();

        Store().Save(new Collection { Name = "API", Requests = [new ApiRequest { Name = "a/b", Url = "https://h/u" }] }, dir.Path);

        Assert.True(File.Exists(dir.Combine("a_b.req.json")));
    }

    [Fact]
    public void Load_falls_back_to_the_directory_name_when_no_manifest_present()
    {
        using var dir = new TempDir();
        var apiDir = dir.Combine("PlainApi");
        Directory.CreateDirectory(apiDir);
        File.WriteAllText(
            Path.Combine(apiDir, "Ping.req.json"),
            new RequestSerializer().Serialize(new ApiRequest { Name = "Ping", Url = "https://h/ping" }));

        var loaded = Store().Load(apiDir);

        Assert.Equal("PlainApi", loaded.Name);
        Assert.Single(loaded.Requests);
    }

    [Fact]
    public void Save_request_writes_a_single_file_that_load_picks_up()
    {
        using var dir = new TempDir();

        Store().SaveRequest(new ApiRequest { Name = "Ping", Url = "https://h/ping" }, dir.Path);

        Assert.True(File.Exists(dir.Combine("Ping.req.json")));
        var loaded = Store().Load(dir.Path);
        Assert.Equal("https://h/ping", loaded.Requests.Single(r => r.Name == "Ping").Url);
    }

    [Fact]
    public void Save_request_overwrites_the_existing_file_for_the_same_name()
    {
        using var dir = new TempDir();
        Store().SaveRequest(new ApiRequest { Name = "Ping", Url = "https://h/v1" }, dir.Path);

        Store().SaveRequest(new ApiRequest { Name = "Ping", Url = "https://h/v2" }, dir.Path);

        var loaded = Store().Load(dir.Path);
        Assert.Single(loaded.Requests);
        Assert.Equal("https://h/v2", loaded.Requests[0].Url);
    }

    [Fact]
    public void Load_ignores_files_that_are_not_request_files()
    {
        using var dir = new TempDir();
        Store().Save(new Collection { Name = "API", Requests = [new ApiRequest { Name = "Ping", Url = "https://h/p" }] }, dir.Path);
        File.WriteAllText(dir.Combine("README.md"), "# notes");
        File.WriteAllText(dir.Combine("notes.txt"), "scratch");

        var loaded = Store().Load(dir.Path);

        Assert.Single(loaded.Requests);
        Assert.Equal("Ping", loaded.Requests[0].Name);
    }
}
