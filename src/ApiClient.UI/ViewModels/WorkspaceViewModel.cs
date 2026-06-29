using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.ImportExport;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiClient.UI.ViewModels;

/// <summary>
/// The standalone shell: a collection explorer (tree of folders/requests) alongside the
/// request <see cref="Editor"/>. Selecting a request node loads it into the editor.
/// </summary>
public partial class WorkspaceViewModel : ViewModelBase
{
    private readonly CollectionStore _store;
    private readonly SettingsStore _settingsStore;
    private readonly ReconfigurableHttpSender? _sender;

    /// <summary>Default constructor: builds the editor with a TLS-configured HTTP client from saved settings.</summary>
    public WorkspaceViewModel()
        : this(new CollectionStore(), new SettingsStore())
    {
    }

    /// <summary>Creates the workspace from stores, building a TLS-configured editor from the loaded settings.</summary>
    public WorkspaceViewModel(CollectionStore store, SettingsStore settingsStore)
    {
        _store = store;
        _settingsStore = settingsStore;
        Settings = _settingsStore.Load();

        _sender = new ReconfigurableHttpSender(new HttpClientSender(TlsHandlerFactory.CreateClient(Settings.ToTlsOptions())));
        var executor = new RequestExecutor(HttpRequestFactory.CreateDefault(), _sender);
        Editor = new RequestEditorViewModel(executor, new StandaloneHostServices());
    }

    /// <summary>Creates the workspace with an explicit editor and stores (used for testing).</summary>
    public WorkspaceViewModel(RequestEditorViewModel editor, CollectionStore store, SettingsStore settingsStore)
    {
        Editor = editor;
        _store = store;
        _settingsStore = settingsStore;
        Settings = _settingsStore.Load();
    }

    /// <summary>The current application settings.</summary>
    public AppSettings Settings { get; private set; }

    /// <summary>Persists <paramref name="settings"/>, updates <see cref="Settings"/>, and rebuilds the HTTP client so TLS changes apply immediately.</summary>
    public void SaveSettings(AppSettings settings)
    {
        _settingsStore.Save(settings);
        Settings = settings;
        _sender?.Set(new HttpClientSender(TlsHandlerFactory.CreateClient(settings.ToTlsOptions())));
    }

    /// <summary>The request editor shown beside the tree.</summary>
    public RequestEditorViewModel Editor { get; }

    /// <summary>Root nodes of the loaded collection.</summary>
    public ObservableCollection<CollectionNodeViewModel> Nodes { get; } = [];

    [ObservableProperty]
    private CollectionNodeViewModel? _selectedNode;

    /// <summary>Loads the collection rooted at <paramref name="directory"/> into the tree.</summary>
    public void LoadCollection(string directory)
    {
        var collection = _store.Load(directory);
        Nodes.Clear();
        Nodes.Add(BuildRoot(collection, directory));
    }

    /// <summary>Imports a Bruno (<c>.bru</c>) collection folder into the tree. Saving a request converts it to the native format.</summary>
    public void ImportBrunoCollection(string directory)
    {
        var collection = BrunoImporter.ImportCollection(directory);
        Nodes.Clear();
        Nodes.Add(BuildRoot(collection, directory));
    }

    /// <summary>Whether the current selection is a saved request that can be written back to disk.</summary>
    public bool CanSaveSelectedRequest => SelectedNode is { IsRequest: true, Directory: not null };

    /// <summary>Writes the editor's current request back to the selected request's file on disk.</summary>
    [RelayCommand(CanExecute = nameof(CanSaveSelectedRequest))]
    private void SaveSelectedRequest()
    {
        if (SelectedNode is { IsRequest: true, Directory: { } directory })
            _store.SaveRequest(Editor.ToRequest(), directory);
    }

    partial void OnSelectedNodeChanged(CollectionNodeViewModel? value)
    {
        if (value is { IsRequest: true, Request: not null })
            Editor.LoadFrom(value.Request);

        SaveSelectedRequestCommand.NotifyCanExecuteChanged();
    }

    private static CollectionNodeViewModel BuildRoot(Collection collection, string directory)
    {
        var root = CollectionNodeViewModel.Folder(collection.Name);
        AddChildren(root, collection.Folders, collection.Requests, directory);
        return root;
    }

    private static void AddChildren(
        CollectionNodeViewModel node,
        IReadOnlyList<CollectionFolder> folders,
        IReadOnlyList<ApiRequest> requests,
        string directory)
    {
        foreach (var request in requests)
            node.Children.Add(CollectionNodeViewModel.ForRequest(request, directory));

        foreach (var folder in folders)
        {
            var folderNode = CollectionNodeViewModel.Folder(folder.Name);
            AddChildren(folderNode, folder.Folders, folder.Requests, Path.Combine(directory, folder.Name));
            node.Children.Add(folderNode);
        }
    }
}
