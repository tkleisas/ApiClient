using System;
using System.IO;
using System.Text.Json;
using ApiClient.Core.Model;
using ApiClient.Core.Serialization;

namespace ApiClient.Core.Storage;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as a JSON file. Loading is forgiving: a
/// missing or unreadable file yields defaults rather than failing, so the app always
/// starts. Uses the same JSON conventions as the rest of the storage layer.
/// </summary>
public sealed class SettingsStore
{
    private readonly string _path;

    /// <summary>Creates a store using the default per-user settings path.</summary>
    public SettingsStore()
        : this(DefaultPath())
    {
    }

    /// <summary>Creates a store backed by an explicit file path (used for testing).</summary>
    public SettingsStore(string path) => _path = path;

    /// <summary>The default settings file path, under the user's application-data directory.</summary>
    public static string DefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ApiClient",
        "settings.json");

    /// <summary>Loads the settings, returning defaults if the file is missing or invalid.</summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), RequestSerializer.Options);
                if (settings is not null)
                    return settings;
            }
        }
        catch (JsonException)
        {
            // Corrupt settings file — fall back to defaults rather than failing to start.
        }

        return new AppSettings();
    }

    /// <summary>Persists <paramref name="settings"/>, creating the directory if needed.</summary>
    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_path, JsonSerializer.Serialize(settings, RequestSerializer.Options));
    }
}
