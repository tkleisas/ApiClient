using ApiClient.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private double _fontSize = 12;

    [ObservableProperty]
    private string _accentColor = string.Empty;

    /// <summary>Preset accent colors offered as swatches; empty string = system default.</summary>
    public string[] AccentPresets { get; } = ["", "#0078D4", "#2E7D32", "#6A1B9A", "#C62828", "#EF6C00", "#00838F"];

    [RelayCommand]
    private void SetAccent(string? hex) => AccentColor = hex ?? string.Empty;

    [ObservableProperty]
    private bool _allowInvalidServerCertificates;

    [ObservableProperty]
    private string _clientCertificatePath = string.Empty;

    [ObservableProperty]
    private string _clientCertificatePassword = string.Empty;

    [ObservableProperty]
    private bool _llmEnabled;

    [ObservableProperty]
    private string _llmEndpoint = string.Empty;

    [ObservableProperty]
    private string _llmApiKey = string.Empty;

    [ObservableProperty]
    private string _llmModel = string.Empty;

    [ObservableProperty]
    private double _llmTemperature;

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
        AccentColor = settings.AccentColor;
        AllowInvalidServerCertificates = settings.AllowInvalidServerCertificates;
        ClientCertificatePath = settings.ClientCertificatePath;
        ClientCertificatePassword = settings.ClientCertificatePassword;
        LlmEnabled = settings.Llm.Enabled;
        LlmEndpoint = settings.Llm.Endpoint;
        LlmApiKey = settings.Llm.ApiKey;
        LlmModel = settings.Llm.Model;
        LlmTemperature = settings.Llm.Temperature;
    }

    /// <summary>Produces an <see cref="AppSettings"/> from the current edits.</summary>
    public AppSettings ToSettings() => new AppSettings
    {
        Theme = Theme,
        FontFamily = FontFamily == DefaultFont ? string.Empty : FontFamily,
        FontSize = FontSize,
        AccentColor = AccentColor,
        AllowInvalidServerCertificates = AllowInvalidServerCertificates,
        ClientCertificatePath = ClientCertificatePath,
        ClientCertificatePassword = ClientCertificatePassword,
        Llm = new ApiClient.Core.Llm.LlmSettings
        {
            Enabled = LlmEnabled,
            Endpoint = LlmEndpoint,
            ApiKey = LlmApiKey,
            Model = LlmModel,
            Temperature = LlmTemperature,
        },
    };
}
