using System;
using System.IO;
using System.Linq;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class TreeOperationsTests
{
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ApiClientTreeTests", Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }

    private static (WorkspaceViewModel Workspace, CollectionStore Store) Open(string dir)
    {
        var store = new CollectionStore();
        store.Save(new Collection { Name = "C", Requests = [new ApiRequest { Name = "Ping", Url = "https://h/p" }] }, dir);
        var ws = new WorkspaceViewModel(new RequestEditorViewModel(), store, new SettingsStore());
        ws.LoadCollection(dir);
        return (ws, store);
    }

    [Fact]
    public void Add_request_creates_a_file_and_appears_in_the_tree()
    {
        using var dir = new TempDir();
        var (ws, store) = Open(dir.Path);

        ws.AddRequest(null);

        Assert.Equal(2, store.Load(dir.Path).Requests.Count);
        Assert.Contains(ws.Nodes.Single().Children, n => n.Title == "New Request");
    }

    [Fact]
    public void Delete_request_removes_the_file()
    {
        using var dir = new TempDir();
        var (ws, store) = Open(dir.Path);
        var ping = ws.Nodes.Single().Children.Single(n => n.Title == "Ping");

        ws.DeleteNode(ping);

        Assert.Empty(store.Load(dir.Path).Requests);
    }

    [Fact]
    public void Rename_request_changes_the_stored_name()
    {
        using var dir = new TempDir();
        var (ws, store) = Open(dir.Path);
        var ping = ws.Nodes.Single().Children.Single(n => n.Title == "Ping");

        ws.RenameNode(ping, "Pong");

        var requests = store.Load(dir.Path).Requests;
        Assert.Single(requests);
        Assert.Equal("Pong", requests[0].Name);
    }

    [Fact]
    public void Add_folder_creates_a_directory_node()
    {
        using var dir = new TempDir();
        var (ws, _) = Open(dir.Path);

        ws.AddFolder(null);

        Assert.True(Directory.Exists(Path.Combine(dir.Path, "New Folder")));
        Assert.Contains(ws.Nodes.Single().Children, n => !n.IsRequest && n.Title == "New Folder");
    }
}
