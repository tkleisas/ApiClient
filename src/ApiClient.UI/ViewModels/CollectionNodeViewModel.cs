using System.Collections.ObjectModel;
using ApiClient.Core.Model;

namespace ApiClient.UI.ViewModels;

/// <summary>
/// A node in the collection tree shown in the explorer: either a folder (with children)
/// or a request (carrying its <see cref="ApiRequest"/> so it can be loaded into the editor).
/// </summary>
public sealed class CollectionNodeViewModel
{
    private CollectionNodeViewModel(string title, bool isRequest, ApiRequest? request)
    {
        Title = title;
        IsRequest = isRequest;
        Request = request;
    }

    /// <summary>The label shown in the tree.</summary>
    public string Title { get; }

    /// <summary>True for a request node, false for a folder node.</summary>
    public bool IsRequest { get; }

    /// <summary>The request carried by a request node; null for folders.</summary>
    public ApiRequest? Request { get; }

    /// <summary>Child nodes (folders/requests). Empty for request nodes.</summary>
    public ObservableCollection<CollectionNodeViewModel> Children { get; } = [];

    /// <summary>Creates a folder node.</summary>
    public static CollectionNodeViewModel Folder(string title) => new(title, isRequest: false, request: null);

    /// <summary>Creates a request node from <paramref name="request"/>.</summary>
    public static CollectionNodeViewModel ForRequest(ApiRequest request) => new(request.Name, isRequest: true, request);
}
