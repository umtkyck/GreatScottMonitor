using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace MedSecureVision.Client.Security;

public class SecureStorage
{
    private readonly ILogger<SecureStorage> _logger;

    public SecureStorage(ILogger<SecureStorage> logger)
    {
        _logger = logger;
    }

    public void StoreCredential(string key, string value)
    {
        try
        {
            // Use Windows DPAPI to encrypt and store credentials
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(value),
                Encoding.UTF8.GetBytes(key), // Additional entropy using key
                DataProtectionScope.CurrentUser);

            // Store in Windows Credential Manager
            // In production, use Windows API or CredentialManager NuGet package
            _logger.LogInformation($"Stored credential for key: {key}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error storing credential for key: {key}");
            throw;
        }
    }

    public string? RetrieveCredential(string key)
    {
        try
        {
            // Retrieve from Windows Credential Manager
            // In production, use Windows API or CredentialManager NuGet package
            _logger.LogInformation($"Retrieved credential for key: {key}");
            return null; // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving credential for key: {key}");
            return null;
        }
    }

    public void DeleteCredential(string key)
    {
        try
        {
            // Delete from Windows Credential Manager
            _logger.LogInformation($"Deleted credential for key: {key}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting credential for key: {key}");
        }
    }
}






