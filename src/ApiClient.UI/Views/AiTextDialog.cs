using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ApiClient.UI.Views;

/// <summary>
/// Simple read-only text dialog for AI output (e.g. response analysis). The caller shows
/// it non-modally, then fills in the result with <see cref="SetResult"/> or <see cref="SetError"/>.
/// </summary>
public partial class AiTextDialog : Window
{
    private readonly TextBox _bodyBox;

    /// <summary>Creates the dialog with the given window title.</summary>
    public AiTextDialog(string title)
    {
        Title = title;
        Width = 560;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _bodyBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Text = "Working…",
            Margin = new(0, 0, 0, 8)
        };

        var closeButton = new Button { Content = "Close", Padding = new(16, 8), HorizontalAlignment = HorizontalAlignment.Right };
        closeButton.Click += (_, _) => Close();

        Content = new Grid
        {
            Margin = new(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children =
            {
                Placed(new ScrollViewer { Content = _bodyBox }, 0),
                Placed(closeButton, 1)
            }
        };
    }

    /// <summary>Shows the completed result text.</summary>
    public void SetResult(string text)
    {
        _bodyBox.Text = text;
        _bodyBox.ClearValue(TextBox.ForegroundProperty);
    }

    /// <summary>Shows an error message in red.</summary>
    public void SetError(string message)
    {
        _bodyBox.Text = message;
        _bodyBox.Foreground = Brushes.Red;
    }

    private static Control Placed(Control control, int row)
    {
        Grid.SetRow(control, row);
        return control;
    }
}
