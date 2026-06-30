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

            // Drive the base control font size so all controls scale, not just inherited text.
            if (settings.FontSize > 0)
                app.Resources["ControlContentThemeFontSize"] = settings.FontSize;

            ApplyAccent(app, settings.AccentColor);
        }

        if (window is null)
            return;

        if (settings.FontSize > 0)
            window.FontSize = settings.FontSize;

        if (!string.IsNullOrWhiteSpace(settings.FontFamily))
            window.FontFamily = new FontFamily(settings.FontFamily);
    }

    private static void ApplyAccent(Application app, string accentHex)
    {
        if (string.IsNullOrWhiteSpace(accentHex) || !Color.TryParse(accentHex, out var accent))
            return;

        app.Resources["SystemAccentColor"] = accent;
        app.Resources["SystemAccentColorLight1"] = Blend(accent, Colors.White, 0.15);
        app.Resources["SystemAccentColorLight2"] = Blend(accent, Colors.White, 0.30);
        app.Resources["SystemAccentColorLight3"] = Blend(accent, Colors.White, 0.45);
        app.Resources["SystemAccentColorDark1"] = Blend(accent, Colors.Black, 0.15);
        app.Resources["SystemAccentColorDark2"] = Blend(accent, Colors.Black, 0.30);
        app.Resources["SystemAccentColorDark3"] = Blend(accent, Colors.Black, 0.45);
    }

    private static Color Blend(Color a, Color b, double t) => Color.FromArgb(
        a.A,
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));
}
