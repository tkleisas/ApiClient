using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ApiClient.Core.Model;
using ApiClient.Core.Serialization;

namespace ApiClient.Core.Storage;

/// <summary>
/// Append-only request history backed by a JSONL file (one <see cref="HistoryEntry"/> per
/// line). Appending is cheap; loading returns the most recent entries first. Dependency-free.
/// </summary>
public sealed class HistoryStore
{
    // JSONL needs each entry on one line, so use a non-indented copy of the shared options.
    private static readonly JsonSerializerOptions Compact = new JsonSerializerOptions(RequestSerializer.Options) { WriteIndented = false };

    private readonly string _path;
    private readonly object _gate = new();

    /// <summary>Creates a store using the default per-user history path.</summary>
    public HistoryStore()
        : this(DefaultPath())
    {
    }

    /// <summary>Creates a store backed by an explicit file path (used for testing).</summary>
    public HistoryStore(string path) => _path = path;

    /// <summary>The default history file path, under the user's application-data directory.</summary>
    public static string DefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ApiClient",
        "history.jsonl");

    /// <summary>Appends one entry to the history file.</summary>
    public void Append(HistoryEntry entry)
    {
        var line = JsonSerializer.Serialize(entry, Compact) + "\n";
        lock (_gate)
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.AppendAllText(_path, line);
            }
            catch (IOException)
            {
                // History recording must never break a send.
            }
        }
    }

    /// <summary>Loads the most recent entries (newest first), up to <paramref name="max"/>.</summary>
    public IReadOnlyList<HistoryEntry> Load(int max = 1000)
    {
        if (!File.Exists(_path))
            return [];

        var entries = new List<HistoryEntry>();
        lock (_gate)
        {
            foreach (var line in File.ReadLines(_path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                try
                {
                    var entry = JsonSerializer.Deserialize<HistoryEntry>(line, RequestSerializer.Options);
                    if (entry is not null)
                        entries.Add(entry);
                }
                catch (JsonException)
                {
                    // Skip a corrupt line rather than failing the whole load.
                }
            }
        }

        entries.Reverse();
        return max > 0 && entries.Count > max ? entries.Take(max).ToList() : entries;
    }
}
