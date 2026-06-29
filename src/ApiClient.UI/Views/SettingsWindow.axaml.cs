using System.Collections.Generic;
using System.Linq;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ApiClient.UI.Views;

/// <summary>The settings dialog. Reports <see cref="Saved"/> when the user confirms.</summary>
public partial class SettingsWindow : Window
{
    /// <summary>Whether the user clicked OK.</summary>
    public bool Saved { get; private set; }

    /// <summary>The editable settings view model.</summary>
    public SettingsViewModel ViewModel { get; }

    /// <summary>Design-time constructor.</summary>
    public SettingsWindow()
        : this(new AppSettings())
    {
    }

    /// <summary>Creates the dialog seeded from <paramref name="settings"/>.</summary>
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        ViewModel = new SettingsViewModel(settings);
        DataContext = ViewModel;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Saved = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();

    private async void OnBrowseCertificate(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select client certificate",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Certificates") { Patterns = ["*.pfx", "*.p12"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            },
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
            ViewModel.ClientCertificatePath = path;
    }
}

