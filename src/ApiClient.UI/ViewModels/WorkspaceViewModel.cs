using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.ImportExport;
using ApiClient.Core.Llm;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using AvaloniaVirtualDataGrid.Core;
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
    private readonly EnvironmentStore _environmentStore = new();
    private readonly HistoryStore _historyStore = new();
    private readonly bool _llmHostProvided;

    /// <summary>Default constructor: builds the editor with a TLS-configured HTTP client from saved settings.</summary>
    public WorkspaceViewModel()
        : this(new CollectionStore(), new SettingsStore())
    {
    }

    /// <summary>Creates the workspace from stores, building a TLS-configured editor from the loaded settings.</summary>
    /// <param name="store">The collection store.</param>
    /// <param name="settingsStore">The settings store.</param>
    /// <param name="llm">
    /// Optional host-provided LLM service (e.g. from the embedding IDE). When null, the AI
    /// features use the built-in OpenAI-compatible service configured in Settings.
    /// </param>
    public WorkspaceViewModel(CollectionStore store, SettingsStore settingsStore, ILlmService? llm = null)
    {
        _store = store;
        _settingsStore = settingsStore;
        _llmHostProvided = llm is not null;
        Settings = _settingsStore.Load();

        _sender = new ReconfigurableHttpSender(new HttpClientSender(TlsHandlerFactory.CreateClient(Settings.ToTlsOptions())));
        var executor = new RequestExecutor(HttpRequestFactory.CreateDefault(), _sender);
        Editor = new RequestEditorViewModel(executor, new StandaloneHostServices())
        {
            RequestRecorded = RecordHistory,
            LlmService = llm ?? new OpenAiCompatibleLlmService(Settings.Llm),
        };

        foreach (var entry in _historyStore.Load())
            History.Add(entry);
    }

    /// <summary>Creates the workspace with an explicit editor and stores (used for testing).</summary>
    public WorkspaceViewModel(RequestEditorViewModel editor, CollectionStore store, SettingsStore settingsStore)
    {
        Editor = editor;
        _store = store;
        _settingsStore = settingsStore;
        Settings = _settingsStore.Load();
    }

    /// <summary>The directory of the currently loaded collection, or null if none is open.</summary>
    public string? CollectionDirectory { get; private set; }

    /// <summary>The current application settings.</summary>
    public AppSettings Settings { get; private set; }

    /// <summary>Persists <paramref name="settings"/>, updates <see cref="Settings"/>, and rebuilds the HTTP client so TLS changes apply immediately.</summary>
    public void SaveSettings(AppSettings settings)
    {
        // Preserve the remembered collection (the settings dialog doesn't edit it).
        var merged = settings with
        {
            LastCollectionDirectory = Settings.LastCollectionDirectory,
            LastCollectionIsBruno = Settings.LastCollectionIsBruno,
        };

        _settingsStore.Save(merged);
        Settings = merged;
        _sender?.Set(new HttpClientSender(TlsHandlerFactory.CreateClient(merged.ToTlsOptions())));

        if (!_llmHostProvided)
            Editor.LlmService = new OpenAiCompatibleLlmService(merged.Llm);
    }

    private void RememberLastCollection(string directory, bool isBruno)
    {
        Settings = Settings with { LastCollectionDirectory = directory, LastCollectionIsBruno = isBruno };
        _settingsStore.Save(Settings);
    }

    /// <summary>The request editor shown beside the tree.</summary>
    public RequestEditorViewModel Editor { get; }

    /// <summary>Root nodes of the loaded collection.</summary>
    public ObservableCollection<CollectionNodeViewModel> Nodes { get; } = [];

    /// <summary>The collection's environments (e.g. Local / UAT / Prod).</summary>
    public ObservableCollection<ApiEnvironment> Environments { get; } = [];

    /// <summary>Request send history, newest first (backed by the virtualized data grid).</summary>
    public InMemoryDataProvider<HistoryEntry> History { get; } = new InMemoryDataProvider<HistoryEntry>();

    [ObservableProperty]
    private bool _showHistory;

    /// <summary>
    /// Whether the embedded menu bar is shown. Hosts that integrate the workspace's
    /// commands into their own menu system (e.g. NVS) set this to false.
    /// </summary>
    [ObservableProperty]
    private bool _isMenuVisible = true;

    private void RecordHistory(HistoryEntry entry)
    {
        _historyStore.Append(entry);
        History.Insert(0, entry);
    }

    /// <summary>Loads a history entry's method and URL back into the editor.</summary>
    public void ReplayHistory(HistoryEntry entry)
    {
        Editor.Method = entry.Method;
        Editor.Url = entry.Url;
    }

    [ObservableProperty]
    private CollectionNodeViewModel? _selectedNode;

    [ObservableProperty]
    private ApiEnvironment? _selectedEnvironment;

    partial void OnSelectedEnvironmentChanged(ApiEnvironment? value)
        => Editor.Variables = value?.ToVariableMap() ?? new Dictionary<string, string>();

    private void SetEnvironments(IReadOnlyList<ApiEnvironment> environments)
    {
        Environments.Clear();
        foreach (var environment in environments)
            Environments.Add(environment);
        SelectedEnvironment = Environments.FirstOrDefault();
    }

    /// <summary>Loads the collection rooted at <paramref name="directory"/> into the tree.</summary>
    public void LoadCollection(string directory)
    {
        var collection = _store.Load(directory);
        Nodes.Clear();
        Nodes.Add(BuildRoot(collection, directory));
        CollectionDirectory = directory;
        SetEnvironments(_environmentStore.Load(directory));
        RememberLastCollection(directory, isBruno: false);
    }

    /// <summary>Imports a Bruno (<c>.bru</c>) collection folder into the tree. Saving a request converts it to the native format.</summary>
    public void ImportBrunoCollection(string directory)
    {
        var collection = BrunoImporter.ImportCollection(directory);
        Nodes.Clear();
        Nodes.Add(BuildRoot(collection, directory));
        CollectionDirectory = directory;
        SetEnvironments(BrunoImporter.ImportEnvironments(directory));
        RememberLastCollection(directory, isBruno: true);
    }

    /// <summary>Persists the edited <paramref name="environments"/> to the open collection's folder (deleting removed ones).</summary>
    public void SaveEnvironments(IReadOnlyList<ApiEnvironment> environments)
    {
        if (CollectionDirectory is null)
            return;

        var keep = environments.Select(e => e.Name).ToHashSet();
        foreach (var existing in Environments.ToList())
        {
            if (!keep.Contains(existing.Name))
                _environmentStore.Delete(existing.Name, CollectionDirectory);
        }

        foreach (var environment in environments)
            _environmentStore.Save(environment, CollectionDirectory);

        SetEnvironments(environments);
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
        var root = CollectionNodeViewModel.Folder(collection.Name, directory);
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
            var folderPath = Path.Combine(directory, folder.Name);
            var folderNode = CollectionNodeViewModel.Folder(folder.Name, folderPath);
            AddChildren(folderNode, folder.Folders, folder.Requests, folderPath);
            node.Children.Add(folderNode);
        }
    }

    /// <summary>Creates a new request in the target node's folder (or the collection root) and reloads.</summary>
    public void AddRequest(CollectionNodeViewModel? target)
    {
        var directory = target?.Directory ?? CollectionDirectory;
        if (directory is null)
            return;

        var name = "New Request";
        for (var n = 2; _store.RequestExists(name, directory); n++)
            name = $"New Request {n}";

        _store.SaveRequest(new ApiRequest { Name = name, Url = "https://" }, directory);
        Reload();
    }

    /// <summary>Creates a new sub-folder in the target node's folder (or the collection root) and reloads.</summary>
    public void AddFolder(CollectionNodeViewModel? target)
    {
        var directory = target?.Directory ?? CollectionDirectory;
        if (directory is null)
            return;

        var name = "New Folder";
        for (var n = 2; Directory.Exists(Path.Combine(directory, name)); n++)
            name = $"New Folder {n}";

        _store.CreateFolder(directory, name);
        Reload();
    }

    /// <summary>Renames a request or folder node on disk and reloads.</summary>
    public void RenameNode(CollectionNodeViewModel node, string newName)
    {
        if (CollectionDirectory is null || node.Directory is null || string.IsNullOrWhiteSpace(newName))
            return;

        if (node is { IsRequest: true, Request: { } request })
        {
            _store.DeleteRequest(request.Name, node.Directory);
            _store.SaveRequest(request with { Name = newName }, node.Directory);
        }
        else
        {
            _store.RenameFolder(node.Directory, newName);
        }

        Reload();
    }

    /// <summary>Deletes a request or folder node from disk and reloads.</summary>
    public void DeleteNode(CollectionNodeViewModel node)
    {
        if (CollectionDirectory is null || node.Directory is null)
            return;

        if (node is { IsRequest: true, Request: { } request })
            _store.DeleteRequest(request.Name, node.Directory);
        else
            _store.DeleteFolder(node.Directory);

        Reload();
    }

    private void Reload()
    {
        if (CollectionDirectory is not null)
            LoadCollection(CollectionDirectory);
    }
}
