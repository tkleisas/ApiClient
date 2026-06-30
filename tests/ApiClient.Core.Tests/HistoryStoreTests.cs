using System;
using System.IO;
using System.Linq;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using Xunit;

namespace ApiClient.Core.Tests;

public class HistoryStoreTests
{
    private static string TempFile() => Path.Combine(
        Path.GetTempPath(), "ApiClientHistoryTests", Guid.NewGuid().ToString("N"), "history.jsonl");

    [Fact]
    public void Load_returns_empty_when_no_file()
    {
        Assert.Empty(new HistoryStore(TempFile()).Load());
    }

    [Fact]
    public void Append_then_load_returns_entries_newest_first()
    {
        var path = TempFile();
        try
        {
            var store = new HistoryStore(path);
            store.Append(new HistoryEntry { Method = "GET", Url = "https://h/1", Status = 200 });
            store.Append(new HistoryEntry { Method = "POST", Url = "https://h/2", Status = 201 });

            var loaded = new HistoryStore(path).Load();

            Assert.Equal(2, loaded.Count);
            Assert.Equal("https://h/2", loaded[0].Url);
            Assert.Equal("https://h/1", loaded[1].Url);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Load_caps_to_the_requested_maximum()
    {
        var path = TempFile();
        try
        {
            var store = new HistoryStore(path);
            for (var i = 0; i < 5; i++)
                store.Append(new HistoryEntry { Method = "GET", Url = $"https://h/{i}" });

            var loaded = store.Load(max: 2);

            Assert.Equal(2, loaded.Count);
            Assert.Equal("https://h/4", loaded[0].Url);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Append_writes_one_line_per_entry()
    {
        var path = TempFile();
        try
        {
            var store = new HistoryStore(path);
            store.Append(new HistoryEntry { Method = "GET", Url = "https://h/1" });
            store.Append(new HistoryEntry { Method = "GET", Url = "https://h/2" });

            Assert.Equal(2, File.ReadAllLines(path).Length);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
