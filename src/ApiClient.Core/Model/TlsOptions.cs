namespace ApiClient.Core.Model;

/// <summary>
/// TLS options applied when sending requests: server-certificate validation behavior and
/// an optional client certificate for mutual TLS. UI-free so the HTTP layer can consume it
/// directly.
/// </summary>
public record TlsOptions
{
    /// <summary>
    /// When true, server certificate errors are ignored (e.g. self-signed certs in dev).
    /// Off by default — this disables a security check and should be used deliberately.
    /// </summary>
    public bool AllowInvalidServerCertificates { get; init; }

    /// <summary>Path to a client certificate (PKCS#12 / .pfx) for mutual TLS; null/empty to disable.</summary>
    public string? ClientCertificatePath { get; init; }

    /// <summary>Password for the client certificate file, if it is protected; otherwise null/empty.</summary>
    public string? ClientCertificatePassword { get; init; }
}
