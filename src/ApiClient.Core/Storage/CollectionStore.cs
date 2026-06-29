using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ApiClient.Core.Model;
using ApiClient.Core.Serialization;

namespace ApiClient.Core.Storage;

/// <summary>
/// Loads and saves <see cref="Collection"/>s as plain folders of files: a
/// <c>collection.json</c> manifest at the root, one <c>*.req.json</c> file per request,
/// and a sub-directory per folder. This is what makes collections git-friendly and
/// shareable — there is no database, only files the user owns.
/// </summary>
public sealed class CollectionStore
{
    private const string ManifestFileName = "collection.json";
    private const string RequestFileSuffix = ".req.json";
    private const string RequestFilePattern = "*" + RequestFileSuffix;

    private readonly RequestSerializer _serializer;

    /// <summary>Creates a store, optionally with a custom request serializer.</summary>
    public CollectionStore(RequestSerializer? serializer = null)
        => _serializer = serializer ?? new RequestSerializer();

    /// <summary>
    /// Loads the collection rooted at <paramref name="directory"/>. The name comes from
    /// the <c>collection.json</c> manifest if present, otherwise from the directory name.
    /// Files that are not <c>*.req.json</c> are ignored.
    /// </summary>
    public Collection Load(string directory)
    {
        var name = new DirectoryInfo(directory).Name;
        var manifestPath = Path.Combine(directory, ManifestFileName);
        if (File.Exists(manifestPath))
        {
            var manifest = JsonSerializer.Deserialize<CollectionManifest>(
                File.ReadAllText(manifestPath), RequestSerializer.Options);
            if (manifest is not null)
                name = manifest.Name;
        }

        return new Collection
        {
            Name = name,
            Requests = LoadRequests(directory),
            Folders = LoadFolders(directory),
        };
    }

    /// <summary>
    /// Saves <paramref name="collection"/> into <paramref name="directory"/>, creating it
    /// if needed. Writes the manifest, the root requests, and a sub-directory per folder.
    /// </summary>
    public void Save(Collection collection, string directory)
    {
        Directory.CreateDirectory(directory);

        var manifest = new CollectionManifest { Name = collection.Name };
        File.WriteAllText(
            Path.Combine(directory, ManifestFileName),
            JsonSerializer.Serialize(manifest, RequestSerializer.Options));

        SaveContents(collection.Requests, collection.Folders, directory);
    }

    /// <summary>
    /// Saves a single request as a <c>*.req.json</c> file in <paramref name="directory"/>
    /// (created if needed). The file name is derived from the request's name, so saving a
    /// request loaded from disk overwrites its original file.
    /// </summary>
    public void SaveRequest(ApiRequest request, string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, FileNameFor(request)), _serializer.Serialize(request));
    }

    private void SaveContents(IReadOnlyList<ApiRequest> requests, IReadOnlyList<CollectionFolder> folders, string directory)
    {
        foreach (var request in requests)
            File.WriteAllText(Path.Combine(directory, FileNameFor(request)), _serializer.Serialize(request));

        foreach (var folder in folders)
        {
            var folderPath = Path.Combine(directory, folder.Name);
            Directory.CreateDirectory(folderPath);
            SaveContents(folder.Requests, folder.Folders, folderPath);
        }
    }

    private IReadOnlyList<ApiRequest> LoadRequests(string directory)
        => Directory.EnumerateFiles(directory, RequestFilePattern)
            .OrderBy(path => path, System.StringComparer.Ordinal)
            .Select(path => _serializer.Deserialize(File.ReadAllText(path)))
            .ToList();

    private IReadOnlyList<CollectionFolder> LoadFolders(string directory)
        => Directory.EnumerateDirectories(directory)
            .OrderBy(path => path, System.StringComparer.Ordinal)
            .Select(path => new CollectionFolder
            {
                Name = new DirectoryInfo(path).Name,
                Requests = LoadRequests(path),
                Folders = LoadFolders(path),
            })
            .ToList();

    private static string FileNameFor(ApiRequest request)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(request.Name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return safe + RequestFileSuffix;
    }
}
