namespace CXA.Client.Services;

public interface ICloudAuthService
{
    Task<bool> AuthenticateAsync();
    Task<string?> GetAccessTokenAsync();
    Task RefreshTokenAsync();
    Task LogoutAsync();
    bool IsAuthenticated { get; }
}






