using ApiClient.Core.Model;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ApiClient.UI;

/// <summary>Applies <see cref="AppSettings"/> appearance choices to the live Avalonia app/window.</summary>
public static class AppearanceService
{
    /// <summary>Applies theme to the application and font to <paramref name="window"/> (if provided).</summary>
    public static void Apply(AppSettings settings, Window? window)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = settings.Theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }

        if (window is null)
            return;

        if (settings.FontSize > 0)
            window.FontSize = settings.FontSize;

        if (!string.IsNullOrWhiteSpace(settings.FontFamily))
            window.FontFamily = new FontFamily(settings.FontFamily);
    }
}
