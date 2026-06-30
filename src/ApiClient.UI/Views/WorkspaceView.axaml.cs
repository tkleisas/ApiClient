using System.Linq;
using System.Threading.Tasks;
using ApiClient.Core.Model;
using ApiClient.UI;
using ApiClient.UI.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaVirtualDataGrid.Columns;

namespace ApiClient.UI.Views;

/// <summary>
/// The standalone shell view: a collection explorer tree beside the embeddable
/// <see cref="ApiClientView"/>. The folder picker lives here (a UI concern); the view
/// model is told only the chosen path.
/// </summary>
public partial class WorkspaceView : UserControl
{
    /// <summary>Initializes the view.</summary>
    public WorkspaceView()
    {
        InitializeComponent();

        HistoryGrid.Columns.Add(VirtualDataGridTextColumn.Create<HistoryEntry>("Time", e => e.Timestamp.ToLocalTime().ToString("HH:mm:ss")));
        HistoryGrid.Columns.Add(VirtualDataGridTextColumn.Create<HistoryEntry>("Method", e => e.Method));
        HistoryGrid.Columns.Add(VirtualDataGridTextColumn.Create<HistoryEntry>("Status", e => e.Status));
        HistoryGrid.Columns.Add(VirtualDataGridTextColumn.Create<HistoryEntry>("ms", e => e.ElapsedMs));
        HistoryGrid.Columns.Add(VirtualDataGridTextColumn.Create<HistoryEntry>("Size", e => e.SizeBytes));
        HistoryGrid.Columns.Add(VirtualDataGridTextColumn.Create<HistoryEntry>("URL", e => e.Url));
        HistoryGrid.RowDoubleClick += OnHistoryRowDoubleClick;
    }

    private void OnHistoryRowDoubleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel workspace && HistoryGrid.SelectedItem is HistoryEntry entry)
            workspace.ReplayHistory(entry);
    }

    private async void OnOpenCollection(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel workspace && await PickFolderAsync("Open collection folder") is { } path)
            workspace.LoadCollection(path);
    }

    private async void OnImportBruno(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel workspace && await PickFolderAsync("Import Bruno collection folder") is { } path)
            workspace.ImportBrunoCollection(path);
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        return string.IsNullOrEmpty(path) ? null : path;
    }

    private void OnAbout(object? sender, RoutedEventArgs e)
    {
        var about = new AboutWindow();
        if (TopLevel.GetTopLevel(this) is Window owner)
            about.ShowDialog(owner);
        else
            about.Show();
    }

    private async void OnEnvironments(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel workspace)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new EnvironmentsWindow(workspace.Environments);

        if (owner is not null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (dialog.Saved)
            workspace.SaveEnvironments(dialog.ViewModel.ToEnvironments());
    }

    private async void OnSettings(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel workspace)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new SettingsWindow(workspace.Settings);

        if (owner is not null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (dialog.Saved)
        {
            var updated = dialog.ViewModel.ToSettings();
            workspace.SaveSettings(updated);
            AppearanceService.Apply(updated, owner);
        }
    }

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private static CollectionNodeViewModel? NodeFrom(object? sender)
        => (sender as Control)?.DataContext as CollectionNodeViewModel;

    private void OnNewRequest(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel workspace)
            workspace.AddRequest(NodeFrom(sender));
    }

    private void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel workspace)
            workspace.AddFolder(NodeFrom(sender));
    }

    private async void OnRenameNode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel workspace || NodeFrom(sender) is not { } node)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new TextPromptWindow("Rename", "New name:", node.Title);
        if (owner is not null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (dialog.Confirmed && !string.IsNullOrWhiteSpace(dialog.Value))
            workspace.RenameNode(node, dialog.Value.Trim());
    }

    private async void OnDeleteNode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel workspace || NodeFrom(sender) is not { } node)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new ConfirmWindow("Delete", $"Delete '{node.Title}'? This cannot be undone.");
        if (owner is not null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (dialog.Confirmed)
            workspace.DeleteNode(node);
    }
}
