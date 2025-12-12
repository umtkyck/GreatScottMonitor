using System.Windows.Media.Imaging;
using MedSecureVision.Shared.Models;

namespace MedSecureVision.Client.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> AuthenticateAsync(BitmapSource frame, DetectedFace face);
    Task<EnrollmentResponse> EnrollAsync(string userId, List<BitmapSource> frames);
}


