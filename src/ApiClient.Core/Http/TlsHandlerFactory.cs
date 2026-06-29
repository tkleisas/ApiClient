using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using ApiClient.Core.Model;

namespace ApiClient.Core.Http;

/// <summary>
/// Builds the HTTP message handler / client that requests are sent through, configured for
/// the given <see cref="TlsOptions"/> (server-certificate validation and an optional client
/// certificate). Isolated here so TLS behavior is explicit and testable.
/// </summary>
public static class TlsHandlerFactory
{
    /// <summary>Creates a <see cref="SocketsHttpHandler"/> configured for <paramref name="options"/>.</summary>
    public static SocketsHttpHandler CreateHandler(TlsOptions options)
    {
        var handler = new SocketsHttpHandler();

        if (options.AllowInvalidServerCertificates)
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

        if (!string.IsNullOrEmpty(options.ClientCertificatePath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                options.ClientCertificatePath, options.ClientCertificatePassword);
            handler.SslOptions.ClientCertificates ??= new X509CertificateCollection();
            handler.SslOptions.ClientCertificates.Add(certificate);
        }

        return handler;
    }

    /// <summary>Creates an <see cref="HttpClient"/> over a handler configured for <paramref name="options"/>.</summary>
    public static HttpClient CreateClient(TlsOptions options) => new HttpClient(CreateHandler(options));
}
