using ApiClient.Core;
using Avalonia.Controls;

namespace ApiClient.App.Views;

/// <summary>The standalone host window. Shows the running version in its title and hosts the embeddable API client control.</summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the window and sets its title to include the build version.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Title = $"ApiClient {BuildInfo.Version}";
    }
}
