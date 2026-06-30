using System;
using System.IO;
using System.Linq;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class WorkspaceViewModelTests
{
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ApiClientUITests", Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }

    private static WorkspaceViewModel NewWorkspace() => new WorkspaceViewModel(
        new RequestEditorViewModel(),
        new CollectionStore(),
        new SettingsStore(System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ApiClientUITests", Guid.NewGuid().ToString("N") + ".json")));

    private static Collection SampleCollection() => new Collection
    {
        Name = "API",
        Requests = [new ApiRequest { Name = "Health", Url = "https://h/health" }],
        Folders =
        [
            new CollectionFolder
            {
                Name = "Users",
                Requests = [new ApiRequest { Name = "Get User", Url = "https://h/users/1" }],
            },
        ],
    };

    [Fact]
    public void Load_collection_builds_a_node_tree()
    {
        using var dir = new TempDir();
        new CollectionStore().Save(SampleCollection(), dir.Path);
        var ws = NewWorkspace();

        ws.LoadCollection(dir.Path);

        var root = Assert.Single(ws.Nodes);
        Assert.Equal("API", root.Title);
        Assert.False(root.IsRequest);

        var health = root.Children.Single(n => n.Title == "Health");
        Assert.True(health.IsRequest);

        var users = root.Children.Single(n => n.Title == "Users");
        Assert.False(users.IsRequest);
        Assert.Contains(users.Children, n => n.Title == "Get User" && n.IsRequest);
    }

    [Fact]
    public void Selecting_a_request_node_loads_it_into_the_editor()
    {
        using var dir = new TempDir();
        new CollectionStore().Save(SampleCollection(), dir.Path);
        var ws = NewWorkspace();
        ws.LoadCollection(dir.Path);

        var getUser = ws.Nodes.Single().Children
            .Single(n => n.Title == "Users").Children
            .Single(n => n.Title == "Get User");

        ws.SelectedNode = getUser;

        Assert.Equal("https://h/users/1", ws.Editor.Url);
    }

    [Fact]
    public void Saving_the_selected_request_persists_editor_edits_to_disk()
    {
        using var dir = new TempDir();
        new CollectionStore().Save(SampleCollection(), dir.Path);
        var ws = NewWorkspace();
        ws.LoadCollection(dir.Path);

        var getUser = ws.Nodes.Single().Children
            .Single(n => n.Title == "Users").Children
            .Single(n => n.Title == "Get User");
        ws.SelectedNode = getUser;
        ws.Editor.Url = "https://h/users/2";

        ws.SaveSelectedRequestCommand.Execute(null);

        var reloaded = new CollectionStore().Load(dir.Path);
        var users = reloaded.Folders.Single(f => f.Name == "Users");
        Assert.Equal("https://h/users/2", users.Requests.Single(r => r.Name == "Get User").Url);
    }

    [Fact]
    public void Selecting_a_folder_node_does_not_change_the_editor()
    {
        using var dir = new TempDir();
        new CollectionStore().Save(SampleCollection(), dir.Path);
        var ws = NewWorkspace();
        ws.LoadCollection(dir.Path);
        var originalUrl = ws.Editor.Url;

        ws.SelectedNode = ws.Nodes.Single().Children.Single(n => n.Title == "Users");

        Assert.Equal(originalUrl, ws.Editor.Url);
    }
}
