using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ApiClient.UI.Views;

/// <summary>A minimal yes/no confirmation dialog. Reports <see cref="Confirmed"/> when accepted.</summary>
public partial class ConfirmWindow : Window
{
    /// <summary>Whether the user confirmed the action.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>Design-time constructor.</summary>
    public ConfirmWindow()
        : this("Confirm", "Are you sure?")
    {
    }

    /// <summary>Creates the dialog with a title and message.</summary>
    public ConfirmWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
