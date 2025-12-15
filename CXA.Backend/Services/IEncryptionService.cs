namespace CXA.Backend.Services;

public interface IEncryptionService
{
    Task<byte[]> EncryptAsync(byte[] data, string userId);
    Task<byte[]> DecryptAsync(byte[] encryptedData, string userId);
}






