using Avalonia.Controls;

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
}
