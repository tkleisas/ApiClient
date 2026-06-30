using System.Collections.Generic;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ApiClient.UI.Views;

/// <summary>The environments editor dialog. Reports <see cref="Saved"/> when the user confirms.</summary>
public partial class EnvironmentsWindow : Window
{
    /// <summary>Whether the user clicked Save.</summary>
    public bool Saved { get; private set; }

    /// <summary>The editor view model.</summary>
    public EnvironmentsEditorViewModel ViewModel { get; }

    /// <summary>Design-time constructor.</summary>
    public EnvironmentsWindow()
        : this([])
    {
    }

    /// <summary>Creates the dialog seeded from existing environments.</summary>
    public EnvironmentsWindow(IEnumerable<ApiEnvironment> environments)
    {
        InitializeComponent();
        ViewModel = new EnvironmentsEditorViewModel(environments);
        DataContext = ViewModel;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        Saved = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
