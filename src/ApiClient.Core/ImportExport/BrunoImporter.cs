using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApiClient.Core.Model;

namespace ApiClient.Core.ImportExport;

/// <summary>
/// Imports <a href="https://www.usebruno.com/">Bruno</a> <c>.bru</c> files into the
/// <see cref="ApiRequest"/> model. The <c>.bru</c> format is a block DSL
/// (<c>name { key: value }</c>); disabled entries are prefixed with <c>~</c>.
/// </summary>
public static class BrunoImporter
{
    private static readonly HashSet<string> Methods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "delete", "patch", "options", "head", "trace",
    };

    /// <summary>Parses a single <c>.bru</c> document into an <see cref="ApiRequest"/>.</summary>
    /// <exception cref="FormatException">The content has no HTTP method block.</exception>
    public static ApiRequest ParseRequest(string bru)
    {
        var blocks = ScanBlocks(bru);

        var methodBlock = blocks.FirstOrDefault(b => Methods.Contains(b.Name));
        if (methodBlock.Name is null)
            throw new FormatException("No HTTP method block found in .bru content.");

        var meta = ParseDictionary(FindBlock(blocks, "meta"));
        var methodFields = ParseDictionary(methodBlock.Inner);

        var bodyMode = methodFields.GetValueOrDefault("body", "none");
        var authMode = methodFields.GetValueOrDefault("auth", "none");

        return new ApiRequest
        {
            Name = meta.GetValueOrDefault("name", "Untitled"),
            Method = methodBlock.Name.ToUpperInvariant(),
            Url = methodFields.GetValueOrDefault("url", string.Empty),
            Headers = ParseEntries(FindBlock(blocks, "headers")),
            Query = ParseEntries(FindBlock(blocks, "params:query")),
            Body = BuildBody(bodyMode, blocks),
            Auth = BuildAuth(authMode, blocks),
        };
    }

    /// <summary>
    /// Imports a Bruno collection directory: every <c>.bru</c> request file becomes an
    /// <see cref="ApiRequest"/>, sub-directories become folders. Files that are not requests
    /// (e.g. environment or folder-settings files) are skipped.
    /// </summary>
    public static Collection ImportCollection(string directory) => new Collection
    {
        Name = new DirectoryInfo(directory).Name,
        Requests = LoadRequests(directory),
        Folders = LoadFolders(directory),
    };

    /// <summary>
    /// Imports Bruno environments from the collection's <c>environments/</c> folder. Each
    /// <c>*.bru</c> file there defines a <c>vars { }</c> block; the file name is the environment name.
    /// </summary>
    public static IReadOnlyList<ApiEnvironment> ImportEnvironments(string directory)
    {
        var folder = Path.Combine(directory, "environments");
        if (!Directory.Exists(folder))
            return [];

        return Directory.EnumerateFiles(folder, "*.bru")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(path => new ApiEnvironment
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Variables = ParseEntries(FindBlock(ScanBlocks(File.ReadAllText(path)), "vars")),
            })
            .ToList();
    }

    private static IReadOnlyList<ApiRequest> LoadRequests(string directory)
    {
        var requests = new List<ApiRequest>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.bru").OrderBy(p => p, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, "collection.bru", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "folder.bru", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                requests.Add(ParseRequest(File.ReadAllText(file)));
            }
            catch (FormatException)
            {
                // Not a request file (e.g. an environment definition) — skip it.
            }
        }

        return requests;
    }

    private static IReadOnlyList<CollectionFolder> LoadFolders(string directory)
        => Directory.EnumerateDirectories(directory)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(path => new CollectionFolder
            {
                Name = new DirectoryInfo(path).Name,
                Requests = LoadRequests(path),
                Folders = LoadFolders(path),
            })
            // Drop folders that contain no requests (e.g. Bruno's environments folder).
            .Where(folder => folder.Requests.Count > 0 || folder.Folders.Count > 0)
            .ToList();

    private static RequestBody BuildBody(string mode, List<(string Name, string Inner)> blocks) => mode switch
    {
        "json" => new RequestBody { Type = BodyType.Raw, MediaType = "application/json", Text = FindBlock(blocks, "body:json").Trim() },
        "text" => new RequestBody { Type = BodyType.Raw, MediaType = "text/plain", Text = FindBlock(blocks, "body:text").Trim() },
        "xml" => new RequestBody { Type = BodyType.Raw, MediaType = "application/xml", Text = FindBlock(blocks, "body:xml").Trim() },
        "form-urlencoded" => new RequestBody { Type = BodyType.FormUrlEncoded, Form = ParseEntries(FindBlock(blocks, "body:form-urlencoded")) },
        _ => new RequestBody(),
    };

    private static RequestAuth BuildAuth(string mode, List<(string Name, string Inner)> blocks)
    {
        switch (mode)
        {
            case "bearer":
                var bearer = ParseDictionary(FindBlock(blocks, "auth:bearer"));
                return new RequestAuth { Type = AuthType.Bearer, Token = bearer.GetValueOrDefault("token") };

            case "basic":
                var basic = ParseDictionary(FindBlock(blocks, "auth:basic"));
                return new RequestAuth
                {
                    Type = AuthType.Basic,
                    Username = basic.GetValueOrDefault("username"),
                    Password = basic.GetValueOrDefault("password"),
                };

            case "apikey":
                var apikey = ParseDictionary(FindBlock(blocks, "auth:apikey"));
                var placement = apikey.GetValueOrDefault("placement", "header");
                return new RequestAuth
                {
                    Type = AuthType.ApiKey,
                    ApiKeyName = apikey.GetValueOrDefault("key"),
                    ApiKeyValue = apikey.GetValueOrDefault("value"),
                    ApiKeyLocation = placement.Contains("query", StringComparison.OrdinalIgnoreCase)
                        ? ApiKeyLocation.Query
                        : ApiKeyLocation.Header,
                };

            default:
                return new RequestAuth();
        }
    }

    /// <summary>Splits the document into top-level <c>name { ... }</c> blocks, matching braces by depth.</summary>
    private static List<(string Name, string Inner)> ScanBlocks(string text)
    {
        var blocks = new List<(string, string)>();
        var i = 0;
        while (i < text.Length)
        {
            var open = text.IndexOf('{', i);
            if (open < 0)
                break;

            var name = text[i..open].Trim();

            var depth = 1;
            var j = open + 1;
            while (j < text.Length && depth > 0)
            {
                if (text[j] == '{') depth++;
                else if (text[j] == '}') depth--;
                j++;
            }

            var inner = text[(open + 1)..(j - 1)];
            if (name.Length > 0)
                blocks.Add((name, inner));
            i = j;
        }

        return blocks;
    }

    private static string FindBlock(List<(string Name, string Inner)> blocks, string name)
        => blocks.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)).Inner ?? string.Empty;

    private static Dictionary<string, string> ParseDictionary(string inner)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value, _) in ParseLines(inner))
            dictionary[key] = value;
        return dictionary;
    }

    private static IReadOnlyList<KeyValueItem> ParseEntries(string inner)
        => ParseLines(inner).Select(line => new KeyValueItem(line.Key, line.Value, line.Enabled)).ToList();

    private static IEnumerable<(string Key, string Value, bool Enabled)> ParseLines(string inner)
    {
        foreach (var raw in inner.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var enabled = true;
            if (line.StartsWith('~'))
            {
                enabled = false;
                line = line[1..].Trim();
            }

            var colon = line.IndexOf(':');
            if (colon < 0)
                continue;

            yield return (line[..colon].Trim(), line[(colon + 1)..].Trim(), enabled);
        }
    }
}
