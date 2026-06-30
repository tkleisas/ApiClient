using System;
using System.IO;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class LastCollectionTests
{
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "ApiClientLastColTests", Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Opening_a_collection_remembers_it_in_settings()
    {
        using var dir = new TempDir();
        var settingsPath = Path.Combine(dir.Path, "settings.json");
        var collectionDir = Path.Combine(dir.Path, "col");
        var store = new CollectionStore();
        store.Save(new Collection { Name = "C" }, collectionDir);

        var ws = new WorkspaceViewModel(new RequestEditorViewModel(), store, new SettingsStore(settingsPath));
        ws.LoadCollection(collectionDir);

        var reloaded = new SettingsStore(settingsPath).Load();
        Assert.Equal(collectionDir, reloaded.LastCollectionDirectory);
        Assert.False(reloaded.LastCollectionIsBruno);
    }

    [Fact]
    public void Saving_settings_preserves_the_remembered_collection()
    {
        using var dir = new TempDir();
        var settingsPath = Path.Combine(dir.Path, "settings.json");
        var collectionDir = Path.Combine(dir.Path, "col");
        var store = new CollectionStore();
        store.Save(new Collection { Name = "C" }, collectionDir);

        var ws = new WorkspaceViewModel(new RequestEditorViewModel(), store, new SettingsStore(settingsPath));
        ws.LoadCollection(collectionDir);

        // Save unrelated settings (as the Settings dialog would) — must not wipe the remembered collection.
        ws.SaveSettings(new AppSettings { Theme = AppTheme.Dark });

        var reloaded = new SettingsStore(settingsPath).Load();
        Assert.Equal(collectionDir, reloaded.LastCollectionDirectory);
        Assert.Equal(AppTheme.Dark, reloaded.Theme);
    }
}
