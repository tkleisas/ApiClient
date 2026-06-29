using System.Linq;
using ApiClient.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

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
    }

    private async void OnOpenCollection(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel workspace)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open collection folder",
            AllowMultiple = false,
        });

        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
            workspace.LoadCollection(path);
    }
}
