using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MedSecureVision.Client.Services;
using MedSecureVision.Shared.Models;

namespace MedSecureVision.Client.ViewModels;

public class AuthenticationViewModel : INotifyPropertyChanged
{
    private readonly IFaceServiceClient _faceServiceClient;
    private readonly ICameraService _cameraService;
    private readonly IAuthenticationService _authenticationService;
    private readonly DispatcherTimer _detectionTimer;

    private string _authenticationState = "Searching";
    private string _statusMessage = "Looking for face...";
    private string _statusText = "Ready";
    private BitmapSource? _cameraFeed;
    private bool _scanningLineVisible = true;
    private bool _showSuccessCheckmark = false;

    public event EventHandler<string>? AuthenticationStateChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public AuthenticationViewModel(
        IFaceServiceClient faceServiceClient,
        ICameraService cameraService,
        IAuthenticationService authenticationService)
    {
        _faceServiceClient = faceServiceClient;
        _cameraService = cameraService;
        _authenticationService = authenticationService;

        _detectionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _detectionTimer.Tick += OnDetectionTimerTick;
        
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await _cameraService.InitializeAsync();
        _cameraService.FrameCaptured += OnFrameCaptured;
        await _cameraService.StartCaptureAsync();
        _detectionTimer.Start();
    }

    public string AuthenticationState
    {
        get => _authenticationState;
        set
        {
            if (_authenticationState != value)
            {
                _authenticationState = value;
                OnPropertyChanged();
                AuthenticationStateChanged?.Invoke(this, value);
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public BitmapSource? CameraFeed
    {
        get => _cameraFeed;
        set
        {
            _cameraFeed = value;
            OnPropertyChanged();
        }
    }

    public bool ScanningLineVisible
    {
        get => _scanningLineVisible;
        set
        {
            _scanningLineVisible = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSuccessCheckmark
    {
        get => _showSuccessCheckmark;
        set
        {
            _showSuccessCheckmark = value;
            OnPropertyChanged();
        }
    }

    private void OnFrameCaptured(object? sender, BitmapSource frame)
    {
        CameraFeed = frame;
    }

    private async void OnDetectionTimerTick(object? sender, EventArgs e)
    {
        if (AuthenticationState == "Success" || AuthenticationState == "Verifying")
            return;

        try
        {
            var frame = await _cameraService.GetCurrentFrameAsync();
            if (frame == null)
            {
                AuthenticationState = "Searching";
                StatusMessage = "No camera feed";
                return;
            }

            // Detect faces
            var detectionResult = await _faceServiceClient.DetectFacesAsync(frame);
            
            if (!detectionResult.Success || detectionResult.Faces.Count == 0)
            {
                AuthenticationState = "Searching";
                StatusMessage = "Looking for face...";
                return;
            }

            if (detectionResult.Faces.Count > 1)
            {
                AuthenticationState = "Searching";
                StatusMessage = "Multiple faces detected. Please ensure only you are in frame.";
                return;
            }

            var face = detectionResult.Faces[0];
            
            // Check face position and size
            if (!IsFaceProperlyPositioned(face))
            {
                AuthenticationState = "Positioning";
                StatusMessage = GetPositioningMessage(face);
                return;
            }

            // Start authentication
            AuthenticationState = "Verifying";
            StatusMessage = "Verifying...";
            ScanningLineVisible = false;

            var authResult = await _authenticationService.AuthenticateAsync(frame, face);
            
            if (authResult.Success)
            {
                AuthenticationState = "Success";
                StatusMessage = $"Welcome, {authResult.UserName}!";
                ShowSuccessCheckmark = true;
                
                // Transition to desktop after delay
                await Task.Delay(800);
                // TODO: Navigate to main application
            }
            else
            {
                AuthenticationState = "Failure";
                StatusMessage = authResult.Error ?? "Face not recognized";
                await Task.Delay(2000);
                AuthenticationState = "Searching";
                StatusMessage = "Please try again";
                ScanningLineVisible = true;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            AuthenticationState = "Searching";
        }
    }

    private bool IsFaceProperlyPositioned(DetectedFace face)
    {
        // Check if face is centered and properly sized
        // This is a simplified check - in production, use actual frame dimensions
        return face.Confidence > 0.7f;
    }

    private string GetPositioningMessage(DetectedFace face)
    {
        // Simplified positioning feedback
        if (face.Confidence < 0.5f)
            return "Move closer";
        if (face.Width < 200)
            return "Move closer";
        if (face.Width > 400)
            return "Move back";
        return "Center your face";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

