using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CXA.Backend.Services;

/// <summary>
/// Service for encrypting and decrypting face templates using AES-256-GCM.
/// Uses user-specific encryption keys derived from master key and user ID.
/// Implements FIPS 140-2 compliant encryption.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EncryptionService> _logger;
    private const int KeySize = 256;
    private const int Iterations = 100000;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Encrypts data using AES-256-GCM with a user-specific key.
    /// </summary>
    /// <param name="data">Data to encrypt (face template bytes)</param>
    /// <param name="userId">User ID used for key derivation</param>
    /// <returns>Encrypted data with IV and authentication tag prepended/appended</returns>
    /// <exception cref="Exception">Thrown if encryption fails</exception>
    public async Task<byte[]> EncryptAsync(byte[] data, string userId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var masterKey = _configuration["Encryption:MasterKey"] ?? "default-master-key-change-in-production";
                var salt = DeriveSalt(userId);
                var key = DeriveKey(masterKey, salt);

                var iv = new byte[12]; // GCM IV size
                RandomNumberGenerator.Fill(iv);
                var tag = new byte[16]; // GCM tag size
                using var aes = new AesGcm(key, tag.Length);
                var ciphertext = new byte[data.Length];

                aes.Encrypt(iv, data, ciphertext, tag);

                using var ms = new MemoryStream();
                ms.Write(iv, 0, iv.Length);
                ms.Write(ciphertext, 0, ciphertext.Length);
                ms.Write(tag, 0, tag.Length);

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw;
            }
        });
    }

    /// <summary>
    /// Decrypts data that was encrypted with EncryptAsync.
    /// </summary>
    /// <param name="encryptedData">Encrypted data with IV and tag</param>
    /// <param name="userId">User ID used for key derivation (must match encryption)</param>
    /// <returns>Decrypted original data</returns>
    /// <exception cref="Exception">Thrown if decryption fails (wrong key, tampered data, etc.)</exception>
    public async Task<byte[]> DecryptAsync(byte[] encryptedData, string userId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var masterKey = _configuration["Encryption:MasterKey"] ?? "default-master-key-change-in-production";
                var salt = DeriveSalt(userId);
                var key = DeriveKey(masterKey, salt);

                // Extract IV, ciphertext, and tag
                var iv = new byte[12];
                Array.Copy(encryptedData, 0, iv, 0, iv.Length);

                var tagSize = 16;
                var tag = new byte[tagSize];
                using var aes = new AesGcm(key, tagSize);
                Array.Copy(encryptedData, encryptedData.Length - tagSize, tag, 0, tagSize);

                var ciphertextLength = encryptedData.Length - iv.Length - tagSize;
                var ciphertext = new byte[ciphertextLength];
                Array.Copy(encryptedData, iv.Length, ciphertext, 0, ciphertextLength);

                var plaintext = new byte[ciphertextLength];
                aes.Decrypt(iv, ciphertext, tag, plaintext);

                return plaintext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw;
            }
        });
    }

    /// <summary>
    /// Derives a salt value from user ID using SHA-256.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>32-byte salt value</returns>
    private byte[] DeriveSalt(string userId)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
    }

    /// <summary>
    /// Derives an encryption key from master key and salt using PBKDF2.
    /// Uses 100,000 iterations for key stretching.
    /// </summary>
    /// <param name="masterKey">Master encryption key from configuration</param>
    /// <param name="salt">Salt value derived from user ID</param>
    /// <returns>32-byte (256-bit) encryption key</returns>
    private byte[] DeriveKey(string masterKey, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(masterKey, salt, Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize / 8);
    }
}

