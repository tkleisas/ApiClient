using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml;
using ApiClient.App.Views;
using ApiClient.UI;
using ApiClient.UI.ViewModels;

namespace ApiClient.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var workspace = new WorkspaceViewModel();
            // Apply theme + control font size before building the window so controls pick it up.
            AppearanceService.Apply(workspace.Settings, null);
            var window = new MainWindow { DataContext = workspace };
            desktop.MainWindow = window;
            AppearanceService.Apply(workspace.Settings, window);
            ReopenLastCollection(workspace);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ReopenLastCollection(WorkspaceViewModel workspace)
    {
        var last = workspace.Settings.LastCollectionDirectory;
        if (string.IsNullOrEmpty(last) || !Directory.Exists(last))
            return;

        try
        {
            if (workspace.Settings.LastCollectionIsBruno)
                workspace.ImportBrunoCollection(last);
            else
                workspace.LoadCollection(last);
        }
        catch (Exception ex)
        {
            Program.Logger.Error("Failed to reopen last collection", ex);
        }
    }
}