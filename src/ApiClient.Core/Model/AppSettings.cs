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

    /// <summary>The base UI font size in points. Defaults to 12 (a compact, dense layout).</summary>
    public double FontSize { get; init; } = 12;

    /// <summary>Accent color as a hex string (e.g. <c>#0078D4</c>); empty uses the system/theme default.</summary>
    public string AccentColor { get; init; } = string.Empty;

    /// <summary>The last collection folder opened, reopened on startup; empty if none.</summary>
    public string LastCollectionDirectory { get; init; } = string.Empty;

    /// <summary>Whether <see cref="LastCollectionDirectory"/> was opened as a Bruno collection.</summary>
    public bool LastCollectionIsBruno { get; init; }

    /// <summary>When true, server certificate validation errors are ignored (e.g. self-signed certs in dev).</summary>
    public bool AllowInvalidServerCertificates { get; init; }

    /// <summary>Path to a client certificate (.pfx) for mutual TLS; empty to disable.</summary>
    public string ClientCertificatePath { get; init; } = string.Empty;

    /// <summary>
    /// Password for the client certificate, if any. NOTE: stored in plain text in the local
    /// settings file — a known limitation; prefer passwordless certs or leave empty.
    /// </summary>
    public string ClientCertificatePassword { get; init; } = string.Empty;

    /// <summary>Projects the TLS-related settings into <see cref="TlsOptions"/> for the HTTP layer.</summary>
    public TlsOptions ToTlsOptions() => new TlsOptions
    {
        AllowInvalidServerCertificates = AllowInvalidServerCertificates,
        ClientCertificatePath = string.IsNullOrEmpty(ClientCertificatePath) ? null : ClientCertificatePath,
        ClientCertificatePassword = string.IsNullOrEmpty(ClientCertificatePassword) ? null : ClientCertificatePassword,
    };
}
