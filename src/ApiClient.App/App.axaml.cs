using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
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
            var window = new MainWindow { DataContext = workspace };
            desktop.MainWindow = window;
            AppearanceService.Apply(workspace.Settings, window);
        }

        base.OnFrameworkInitializationCompleted();
    }
}