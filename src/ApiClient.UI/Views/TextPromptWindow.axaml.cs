using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ApiClient.UI.Views;

/// <summary>A minimal text-input dialog (used for renaming). Reports <see cref="Confirmed"/> on OK.</summary>
public partial class TextPromptWindow : Window
{
    /// <summary>Whether the user clicked OK.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>The entered text.</summary>
    public string Value => Input.Text ?? string.Empty;

    /// <summary>Design-time constructor.</summary>
    public TextPromptWindow()
        : this("Rename", "New name:", string.Empty)
    {
    }

    /// <summary>Creates the prompt with a title, label, and initial value.</summary>
    public TextPromptWindow(string title, string prompt, string initial)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        Input.Text = initial;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
