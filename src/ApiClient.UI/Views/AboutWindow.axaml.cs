using ApiClient.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ApiClient.UI.Views;

/// <summary>The About dialog: shows the running version (from the git tag) and project info.</summary>
public partial class AboutWindow : Window
{
    /// <summary>Initializes the dialog and fills in the version.</summary>
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"ApiClient {BuildInfo.Version}";
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
