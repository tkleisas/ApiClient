namespace ApiClient.Core.Model;

/// <summary>The application's color theme preference.</summary>
public enum AppTheme
{
    /// <summary>Follow the operating system's light/dark setting.</summary>
    System,

    /// <summary>Always use the light theme.</summary>
    Light,

    /// <summary>Always use the dark theme.</summary>
    Dark,
}

/// <summary>
/// User-configurable application settings (appearance, etc.), persisted as JSON. UI-free
/// so the core owns the schema; the UI maps these to Avalonia concepts when applying them.
/// </summary>
public record AppSettings
{
    /// <summary>Storage schema version. Currently <c>1</c>.</summary>
    public int Version { get; init; } = 1;

    /// <summary>The color theme. Defaults to <see cref="AppTheme.System"/>.</summary>
    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>The UI font family; empty means use the application default.</summary>
    public string FontFamily { get; init; } = string.Empty;

    /// <summary>The base UI font size in points. Defaults to 14.</summary>
    public double FontSize { get; init; } = 14;
}
