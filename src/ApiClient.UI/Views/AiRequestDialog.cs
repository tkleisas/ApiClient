using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ApiClient.Core.Llm;
using ApiClient.Core.Model;

namespace ApiClient.UI.Views;

/// <summary>
/// "Build request with AI" dialog: describe the request in natural language, preview the
/// generated definition, and apply it to the editor. Follows the code-built dialog idiom.
/// </summary>
public partial class AiRequestDialog : Window
{
    private readonly ILlmService _llm;
    private readonly TextBox _descriptionBox;
    private readonly TextBox _previewBox;
    private readonly TextBlock _statusText;
    private readonly Button _applyButton;
    private string _generatedJson = string.Empty;

    /// <summary>The parsed request when the user clicked Apply; otherwise null.</summary>
    public ApiRequest? Result { get; private set; }

    /// <summary>Creates the dialog over the given LLM service.</summary>
    public AiRequestDialog(ILlmService llm)
    {
        _llm = llm;

        Title = "Build Request with AI";
        Width = 520;
        Height = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _descriptionBox = new TextBox
        {
            AcceptsReturn = true,
            MinHeight = 60,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "e.g. \"Create a user on https://api.example.com with a JSON body containing name and email\"",
            Margin = new(0, 0, 0, 8)
        };

        var generateButton = new Button { Content = "Generate", Padding = new(16, 6), Margin = new(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Left };
        generateButton.Click += OnGenerate;

        _previewBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            FontFamily = new FontFamily("Cascadia Code,Consolas,monospace"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "The generated request definition appears here",
            Margin = new(0, 0, 0, 8)
        };

        _statusText = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 8) };

        _applyButton = new Button { Content = "Apply to editor", Padding = new(16, 8), IsEnabled = false };
        _applyButton.Click += (_, _) =>
        {
            try
            {
                Result = LlmPrompts.ParseGeneratedRequest(_generatedJson);
                Close();
            }
            catch (FormatException ex)
            {
                SetStatus(ex.Message, error: true);
            }
        };

        var cancelButton = new Button { Content = "Cancel", Padding = new(16, 8), Margin = new(8, 0, 0, 0) };
        cancelButton.Click += (_, _) => Close();

        Content = new Grid
        {
            Margin = new(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            Children =
            {
                Placed(_descriptionBox, 0),
                Placed(generateButton, 1),
                Placed(_previewBox, 2),
                Placed(_statusText, 3),
                Placed(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { _applyButton, cancelButton }
                }, 4)
            }
        };
    }

    private async void OnGenerate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_descriptionBox.Text))
            return;

        _applyButton.IsEnabled = false;
        SetStatus("Generating…", error: false);

        try
        {
            var (system, user) = LlmPrompts.BuildRequestFromDescription(_descriptionBox.Text.Trim());
            var reply = await _llm.ChatAsync(system, user);
            _generatedJson = LlmPrompts.ExtractCode(reply);
            _previewBox.Text = _generatedJson;

            var parsed = LlmPrompts.ParseGeneratedRequest(_generatedJson);
            SetStatus($"Generated: {parsed.Method} {parsed.Url}", error: false);
            _applyButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    private void SetStatus(string text, bool error)
    {
        _statusText.Text = text;
        _statusText.Foreground = error ? Brushes.Red : Brushes.Gray;
    }

    private static Control Placed(Control control, int row)
    {
        Grid.SetRow(control, row);
        return control;
    }
}
