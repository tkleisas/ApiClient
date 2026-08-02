using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ApiClient.UI.ViewModels;

namespace ApiClient.UI.Views;

/// <summary>
/// The embeddable API client control: a request editor + response viewer. Host it in a
/// standalone window or as a panel inside another Avalonia app (e.g. the nvs IDE) by
/// adding it to the visual tree and assigning a <see cref="ViewModels.RequestEditorViewModel"/>
/// as its <c>DataContext</c>.
/// </summary>
public partial class ApiClientView : UserControl
{
    /// <summary>Initializes the control.</summary>
    public ApiClientView()
    {
        InitializeComponent();
    }

    private async void OnAiBuildRequest(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RequestEditorViewModel vm || vm.LlmService is null)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AiRequestDialog(vm.LlmService);

        if (owner is not null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (dialog.Result is not null)
            vm.LoadFrom(dialog.Result);
    }

    private async void OnAiAnalyzeResponse(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RequestEditorViewModel vm)
            return;

        var dialog = new AiTextDialog("Response Analysis");
        var owner = TopLevel.GetTopLevel(this) as Window;

        if (owner is not null)
            dialog.Show(owner);
        else
            dialog.Show();

        try
        {
            var analysis = await vm.AnalyzeResponseAsync();
            dialog.SetResult(analysis);
        }
        catch (Exception ex)
        {
            dialog.SetError(ex.Message);
        }
    }
}
