namespace MedSecureVision.Backend.Services;

public interface IAuth0Service
{
    Task<string> RefreshTokenAsync();
    Task<object> GetUserProfileAsync(string userId);
}






