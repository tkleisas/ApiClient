using ApiClient.Core.Llm;
using ApiClient.Core.Model;
using ApiClient.UI.ViewModels;
using Xunit;

namespace ApiClient.UI.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void Empty_font_family_is_shown_as_Default()
    {
        var vm = new SettingsViewModel(new AppSettings { FontFamily = string.Empty });

        Assert.Equal("Default", vm.FontFamily);
        Assert.Equal(string.Empty, vm.ToSettings().FontFamily);
    }

    [Fact]
    public void Round_trips_chosen_values()
    {
        var vm = new SettingsViewModel(new AppSettings { Theme = AppTheme.Dark, FontFamily = "Inter", FontSize = 16 });

        var settings = vm.ToSettings();

        Assert.Equal(AppTheme.Dark, settings.Theme);
        Assert.Equal("Inter", settings.FontFamily);
        Assert.Equal(16, settings.FontSize);
    }

    [Fact]
    public void Round_trips_accent_color()
    {
        var vm = new SettingsViewModel(new AppSettings { AccentColor = "#2E7D32" });

        Assert.Equal("#2E7D32", vm.ToSettings().AccentColor);

        vm.SetAccentCommand.Execute("#C62828");
        Assert.Equal("#C62828", vm.ToSettings().AccentColor);
    }

    [Fact]
    public void Round_trips_tls_options()
    {
        var vm = new SettingsViewModel(new AppSettings
        {
            AllowInvalidServerCertificates = true,
            ClientCertificatePath = @"C:\certs\client.pfx",
            ClientCertificatePassword = "secret",
        });

        var settings = vm.ToSettings();

        Assert.True(settings.AllowInvalidServerCertificates);
        Assert.Equal(@"C:\certs\client.pfx", settings.ClientCertificatePath);
        Assert.Equal("secret", settings.ClientCertificatePassword);
    }

    [Fact]
    public void Llm_thinking_fields_default_to_off_and_medium()
    {
        var vm = new SettingsViewModel(new AppSettings());

        Assert.False(vm.LlmThinkingMode);
        Assert.Equal("medium", vm.LlmThinkingEffort);
    }

    [Fact]
    public void Round_trips_llm_thinking_fields()
    {
        var vm = new SettingsViewModel(new AppSettings
        {
            Llm = new LlmSettings
            {
                Enabled = true,
                Endpoint = "https://example.test/v1",
                ApiKey = "key",
                Model = "deepseek-chat",
                Temperature = 0.7,
                ThinkingMode = true,
                ThinkingEffort = "high",
            },
        });

        Assert.True(vm.LlmThinkingMode);
        Assert.Equal("high", vm.LlmThinkingEffort);

        var settings = vm.ToSettings();

        Assert.True(settings.Llm.Enabled);
        Assert.Equal("deepseek-chat", settings.Llm.Model);
        Assert.True(settings.Llm.ThinkingMode);
        Assert.Equal("high", settings.Llm.ThinkingEffort);
    }
}
