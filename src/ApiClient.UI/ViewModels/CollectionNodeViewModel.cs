using System.Collections.ObjectModel;
using ApiClient.Core.Model;

namespace ApiClient.UI.ViewModels;

/// <summary>
/// A node in the collection tree shown in the explorer: either a folder (with children)
/// or a request (carrying its <see cref="ApiRequest"/> so it can be loaded into the editor).
/// </summary>
public sealed class CollectionNodeViewModel
{
    private CollectionNodeViewModel(string title, bool isRequest, ApiRequest? request, string? directory)
    {
        Title = title;
        IsRequest = isRequest;
        Request = request;
        Directory = directory;
    }

    /// <summary>The label shown in the tree.</summary>
    public string Title { get; }

    /// <summary>True for a request node, false for a folder node.</summary>
    public bool IsRequest { get; }

    /// <summary>The request carried by a request node; null for folders.</summary>
    public ApiRequest? Request { get; }

    /// <summary>The on-disk directory containing this request's file (request nodes only); null for folders.</summary>
    public string? Directory { get; }

    /// <summary>Child nodes (folders/requests). Empty for request nodes.</summary>
    public ObservableCollection<CollectionNodeViewModel> Children { get; } = [];

    /// <summary>Creates a folder node located at <paramref name="directory"/>.</summary>
    public static CollectionNodeViewModel Folder(string title, string? directory) => new(title, isRequest: false, request: null, directory);

    /// <summary>Creates a request node from <paramref name="request"/>, located in <paramref name="directory"/>.</summary>
    public static CollectionNodeViewModel ForRequest(ApiRequest request, string directory)
        => new(request.Name, isRequest: true, request, directory);
}
