using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace MedSecureVision.Client.Services;

public class CertificatePinningHandler : HttpClientHandler
{
    private readonly ILogger<CertificatePinningHandler> _logger;
    private readonly string _expectedThumbprint;

    public CertificatePinningHandler(ILogger<CertificatePinningHandler> logger, string expectedThumbprint)
    {
        _logger = logger;
        _expectedThumbprint = expectedThumbprint;
        ServerCertificateCustomValidationCallback = ValidateCertificate;
    }

    private bool ValidateCertificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        if (certificate == null)
        {
            _logger.LogWarning("Certificate is null");
            return false;
        }

        // Check certificate thumbprint
        var thumbprint = certificate.Thumbprint?.Replace(" ", "").ToUpperInvariant();
        var expected = _expectedThumbprint.Replace(" ", "").ToUpperInvariant();

        if (thumbprint != expected)
        {
            _logger.LogWarning($"Certificate thumbprint mismatch. Expected: {expected}, Got: {thumbprint}");
            return false;
        }

        // Additional validation: check certificate chain
        if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
        {
            _logger.LogWarning($"SSL policy errors: {sslPolicyErrors}");
            return false;
        }

        return true;
    }
}

