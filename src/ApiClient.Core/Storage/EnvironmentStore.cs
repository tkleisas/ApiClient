using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ApiClient.Core.Model;
using ApiClient.Core.Serialization;

namespace ApiClient.Core.Storage;

/// <summary>
/// Loads and saves a collection's environments as JSON files in an <c>environments/</c>
/// sub-directory (one <c>*.env.json</c> per environment). Uses the shared JSON conventions.
/// </summary>
public sealed class EnvironmentStore
{
    private const string FolderName = "environments";
    private const string FileSuffix = ".env.json";

    /// <summary>Loads all environments for the collection rooted at <paramref name="collectionDirectory"/>.</summary>
    public IReadOnlyList<ApiEnvironment> Load(string collectionDirectory)
    {
        var folder = Path.Combine(collectionDirectory, FolderName);
        if (!Directory.Exists(folder))
            return [];

        return Directory.EnumerateFiles(folder, "*" + FileSuffix)
            .OrderBy(p => p, System.StringComparer.Ordinal)
            .Select(path => JsonSerializer.Deserialize<ApiEnvironment>(File.ReadAllText(path), RequestSerializer.Options))
            .Where(env => env is not null)
            .Select(env => env!)
            .ToList();
    }

    /// <summary>Saves <paramref name="environment"/> into the collection's <c>environments/</c> folder.</summary>
    public void Save(ApiEnvironment environment, string collectionDirectory)
        => File.WriteAllText(FilePathFor(environment.Name, collectionDirectory, createFolder: true),
            JsonSerializer.Serialize(environment, RequestSerializer.Options));

    /// <summary>Deletes the file for the environment named <paramref name="name"/>, if it exists.</summary>
    public void Delete(string name, string collectionDirectory)
    {
        var path = FilePathFor(name, collectionDirectory, createFolder: false);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string FilePathFor(string name, string collectionDirectory, bool createFolder)
    {
        var folder = Path.Combine(collectionDirectory, FolderName);
        if (createFolder)
            Directory.CreateDirectory(folder);

        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return Path.Combine(folder, safe + FileSuffix);
    }
}
