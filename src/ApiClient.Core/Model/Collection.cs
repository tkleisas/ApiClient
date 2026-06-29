using System.Collections.Generic;

namespace ApiClient.Core.Model;

/// <summary>
/// A folder within a collection: a named group that can contain requests and further
/// sub-folders. Mirrors a directory on disk.
/// </summary>
public record CollectionFolder
{
    /// <summary>The folder name (its directory name on disk).</summary>
    public required string Name { get; init; }

    /// <summary>Child folders, nested arbitrarily deep.</summary>
    public IReadOnlyList<CollectionFolder> Folders { get; init; } = [];

    /// <summary>The requests directly contained in this folder.</summary>
    public IReadOnlyList<ApiRequest> Requests { get; init; } = [];
}

/// <summary>
/// A collection of API requests, organized as a tree of folders. On disk this is a
/// directory: a <c>collection.json</c> manifest at the root, one <c>*.req.json</c> file
/// per request, and a sub-directory per folder.
/// </summary>
public record Collection
{
    /// <summary>The collection's display name.</summary>
    public required string Name { get; init; }

    /// <summary>Top-level folders.</summary>
    public IReadOnlyList<CollectionFolder> Folders { get; init; } = [];

    /// <summary>Requests at the root of the collection (not inside any folder).</summary>
    public IReadOnlyList<ApiRequest> Requests { get; init; } = [];
}

/// <summary>
/// The persisted, root-level metadata for a collection (the <c>collection.json</c> file).
/// Folders and requests are represented by the directory structure rather than listed here.
/// </summary>
public record CollectionManifest
{
    /// <summary>Storage schema version of the manifest. Currently <c>1</c>.</summary>
    public int Version { get; init; } = 1;

    /// <summary>The collection's display name.</summary>
    public required string Name { get; init; }
}
