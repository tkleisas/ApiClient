using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class TlsHandlerFactoryTests
{
    [Fact]
    public void Default_options_keep_standard_server_validation()
    {
        using var handler = TlsHandlerFactory.CreateHandler(new TlsOptions());

        Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback);
    }

    [Fact]
    public void Allowing_invalid_certificates_installs_a_permissive_callback()
    {
        using var handler = TlsHandlerFactory.CreateHandler(new TlsOptions { AllowInvalidServerCertificates = true });

        var callback = handler.SslOptions.RemoteCertificateValidationCallback;
        Assert.NotNull(callback);
        Assert.True(callback!(this, null, null, SslPolicyErrors.RemoteCertificateNotAvailable));
    }

    [Fact]
    public void No_client_certificate_by_default()
    {
        using var handler = TlsHandlerFactory.CreateHandler(new TlsOptions());

        Assert.True(handler.SslOptions.ClientCertificates is null || handler.SslOptions.ClientCertificates.Count == 0);
    }

    [Fact]
    public void Client_certificate_is_loaded_from_a_pfx_file()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pfx");
        CreateTestPfx(path, "pw");
        try
        {
            using var handler = TlsHandlerFactory.CreateHandler(new TlsOptions
            {
                ClientCertificatePath = path,
                ClientCertificatePassword = "pw",
            });

            Assert.NotNull(handler.SslOptions.ClientCertificates);
            Assert.Single(handler.SslOptions.ClientCertificates!);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void CreateTestPfx(string path, string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=apiclient-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }
}
