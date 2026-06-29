using ApiClient.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiClient.UI.ViewModels;

/// <summary>Editable view of <see cref="AppSettings"/> for the settings dialog.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private const string DefaultFont = "Default";

    /// <summary>The selectable themes.</summary>
    public AppTheme[] Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    /// <summary>The selectable font families. <c>"Default"</c> means use the app default.</summary>
    public string[] FontFamilies { get; } = [DefaultFont, "Inter", "Segoe UI", "Cascadia Code", "Consolas"];

    [ObservableProperty]
    private AppTheme _theme;

    [ObservableProperty]
    private string _fontFamily = DefaultFont;

    [ObservableProperty]
    private double _fontSize = 14;

    /// <summary>Design-time constructor.</summary>
    public SettingsViewModel()
    {
    }

    /// <summary>Creates the view model from existing settings.</summary>
    public SettingsViewModel(AppSettings settings)
    {
        Theme = settings.Theme;
        FontFamily = string.IsNullOrEmpty(settings.FontFamily) ? DefaultFont : settings.FontFamily;
        FontSize = settings.FontSize;
    }

    /// <summary>Produces an <see cref="AppSettings"/> from the current edits.</summary>
    public AppSettings ToSettings() => new AppSettings
    {
        Theme = Theme,
        FontFamily = FontFamily == DefaultFont ? string.Empty : FontFamily,
        FontSize = FontSize,
    };
}
