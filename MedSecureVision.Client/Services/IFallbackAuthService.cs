namespace MedSecureVision.Client.Services;

public interface IFallbackAuthService
{
    Task<bool> AuthenticateWithPinAsync(string pin);
    Task<bool> AuthenticateWithWindowsHelloAsync();
    Task<bool> AuthenticateWithSmartCardAsync();
    int GetRemainingAttempts();
}

