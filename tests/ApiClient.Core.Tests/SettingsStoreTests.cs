using System;
using System.IO;
using ApiClient.Core.Model;
using ApiClient.Core.Storage;
using Xunit;

namespace ApiClient.Core.Tests;

public class SettingsStoreTests
{
    private static string TempFile() => Path.Combine(
        Path.GetTempPath(), "ApiClientSettingsTests", Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public void Load_returns_defaults_when_the_file_is_missing()
    {
        var settings = new SettingsStore(TempFile()).Load();

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(14, settings.FontSize);
        Assert.Equal(string.Empty, settings.FontFamily);
    }

    [Fact]
    public void Save_then_load_round_trips_the_settings()
    {
        var path = TempFile();
        try
        {
            new SettingsStore(path).Save(new AppSettings
            {
                Theme = AppTheme.Dark,
                FontFamily = "Inter",
                FontSize = 16,
            });

            var loaded = new SettingsStore(path).Load();

            Assert.Equal(AppTheme.Dark, loaded.Theme);
            Assert.Equal("Inter", loaded.FontFamily);
            Assert.Equal(16, loaded.FontSize);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_returns_defaults_for_an_invalid_file()
    {
        var path = TempFile();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ not valid json");
        try
        {
            var settings = new SettingsStore(path).Load();

            Assert.Equal(AppTheme.System, settings.Theme);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
