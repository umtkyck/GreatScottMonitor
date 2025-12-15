using System.Windows.Media.Imaging;
using CXA.Shared.Models;

namespace CXA.Client.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> AuthenticateAsync(BitmapSource frame, DetectedFace face);
    Task<EnrollmentResponse> EnrollAsync(string userId, List<BitmapSource> frames);
}






