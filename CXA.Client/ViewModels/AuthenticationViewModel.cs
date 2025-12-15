using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CXA.Client.Constants;
using CXA.Client.Services;
using CXA.Shared.Models;
using Microsoft.Extensions.Logging;

namespace CXA.Client.ViewModels;

/// <summary>
/// ViewModel for the main authentication window.
/// Manages camera feed, face detection, and authentication flow.
/// </summary>
public class AuthenticationViewModel : INotifyPropertyChanged
{
    private readonly IFaceServiceClient _faceServiceClient;
    private readonly ICameraService _cameraService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IEnrollmentPathService _pathService;
    private readonly ILogger<AuthenticationViewModel>? _logger;
    private readonly DispatcherTimer _detectionTimer;

    private string _authenticationState = "Searching";
    private string _statusMessage = "Initializing camera...";
    private string _statusText = "Ready";
    private BitmapSource? _cameraFeed;
    private bool _scanningLineVisible = true;
    private bool _showSuccessCheckmark;
    private bool _isCameraInitialized;
    private bool _isFaceServiceAvailable;
    private bool _hasEnrolledFaces;
    
    // Security & Lockout
    private int _failedAttempts;
    private bool _isLockedOut;

    public event EventHandler<string>? AuthenticationStateChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public string AppVersion => AppConstants.AppVersion;

    public AuthenticationViewModel(
        IFaceServiceClient faceServiceClient,
        ICameraService cameraService,
        IAuthenticationService authenticationService,
        ILogger<AuthenticationViewModel>? logger = null)
    {
        _faceServiceClient = faceServiceClient ?? throw new ArgumentNullException(nameof(faceServiceClient));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _pathService = new EnrollmentPathService();
        _logger = logger;

        _detectionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(AppConstants.DetectionTimerIntervalMs)
        };
        _detectionTimer.Tick += OnDetectionTimerTick;
        
        // Check for enrollment on startup
        CheckEnrollment();

        // Initialize asynchronously
        _ = InitializeAsync();
    }

    /// <summary>
    /// Checks if any faces are enrolled.
    /// </summary>
    private void CheckEnrollment()
    {
        _hasEnrolledFaces = _pathService.HasEnrollment();
    }

    /// <summary>
    /// Restarts the authentication process.
    /// Called when service is restarted from system tray.
    /// </summary>
    public void RestartAuthentication()
    {
        _failedAttempts = 0;
        _isLockedOut = false;
        CheckEnrollment();
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initialize camera and face service.
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            if (_isLockedOut)
            {
                StatusMessage = "Biometrics locked. Use PIN.";
                return;
            }

            if (!_hasEnrolledFaces)
            {
                StatusMessage = "No faces enrolled.";
                AuthenticationState = "NotEnrolled";
                return;
            }

            StatusMessage = "Initializing camera...";
            
            // Initialize camera
            await _cameraService.InitializeAsync();
            _cameraService.FrameCaptured += OnFrameCaptured;
            await _cameraService.StartCaptureAsync();
            _isCameraInitialized = true;
            
            _logger?.LogInformation("Camera initialized successfully");

            // Check if face service is available
            try
            {
                _isFaceServiceAvailable = await _faceServiceClient.IsServiceAvailableAsync();
                if (_isFaceServiceAvailable)
                {
                    _logger?.LogInformation("Face service is available");
                }
                else
                {
                    _logger?.LogWarning("Face service is not available - running in demo mode");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Face service check failed - running in demo mode");
                _isFaceServiceAvailable = false;
            }

            // Start detection timer
            _detectionTimer.Start();
            
            AuthenticationState = "Searching";
            StatusMessage = "Looking for face...";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize camera");
            StatusMessage = "Camera not available";
            StatusText = $"Error: {ex.Message}";
        }
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

    /// <summary>
    /// Handle frame captured event from camera service.
    /// </summary>
    private void OnFrameCaptured(object? sender, BitmapSource frame)
    {
        CameraFeed = frame;
    }

    /// <summary>
    /// Detection timer tick - runs face detection and authentication.
    /// </summary>
    private async void OnDetectionTimerTick(object? sender, EventArgs e)
    {
        if (_isLockedOut)
        {
            AuthenticationState = "LockedOut";
            StatusMessage = "Biometrics locked out.";
            _detectionTimer.Stop();
            return;
        }

        if (AuthenticationState == "Success" || AuthenticationState == "Verifying")
            return;

        if (!_hasEnrolledFaces)
        {
            AuthenticationState = "NotEnrolled";
            StatusMessage = "No faces enrolled";
            return;
        }

        if (!_isCameraInitialized)
        {
            StatusMessage = "Camera not initialized";
            return;
        }

        try
        {
            var frame = await _cameraService.GetCurrentFrameAsync();
            if (frame == null)
            {
                AuthenticationState = "Searching";
                StatusMessage = "No camera feed";
                return;
            }

            // If face service is available, use it for detection
            if (_isFaceServiceAvailable)
            {
                await RunFaceDetectionAsync(frame);
            }
            else
            {
                // Demo mode - just show the camera feed
                AuthenticationState = "Searching";
                StatusMessage = "Position your face within the frame";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in detection timer");
            StatusText = $"Error: {ex.Message}";
            AuthenticationState = "Searching";
        }
    }

    /// <summary>
    /// Run face detection and authentication.
    /// </summary>
    private async Task RunFaceDetectionAsync(BitmapSource frame)
    {
        try
        {
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
            StatusMessage = "Verifying identity...";
            ScanningLineVisible = false;

            var authResult = await _authenticationService.AuthenticateAsync(frame, face);
            
            if (authResult.Success)
            {
                _failedAttempts = 0; // Reset failures on success
                AuthenticationState = "Success";
                StatusMessage = $"Welcome, {authResult.UserName}!";
                ShowSuccessCheckmark = true;
                
                // Transition after delay
                await Task.Delay(AppConstants.SuccessDelayMs);
            }
            else
            {
                _failedAttempts++;
                AuthenticationState = "Failure";
                StatusMessage = authResult.Error ?? "Face not recognized";
                
                if (_failedAttempts >= AppConstants.MaxFailedAttempts)
                {
                    _isLockedOut = true;
                    StatusMessage = "Too many attempts. Use PIN.";
                    _detectionTimer.Stop();
                    // Don't restart searching loop
                }
                else
                {
                    await Task.Delay(AppConstants.FailureDelayMs);
                    AuthenticationState = "Searching";
                    StatusMessage = "Please try again";
                    ScanningLineVisible = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Face detection failed");
            AuthenticationState = "Searching";
            StatusMessage = "Detection error - retrying...";
        }
    }

    /// <summary>
    /// Check if face is properly positioned within the frame.
    /// </summary>
    /// <param name="face">The detected face to evaluate.</param>
    /// <returns>True if face is properly positioned for authentication.</returns>
    private static bool IsFaceProperlyPositioned(DetectedFace face)
    {
        return face.Confidence > AppConstants.GoodFaceConfidence && 
               face.Width >= AppConstants.MinFaceWidth && 
               face.Width <= AppConstants.MaxFaceWidth;
    }

    /// <summary>
    /// Get positioning feedback message based on face detection.
    /// </summary>
    /// <param name="face">The detected face to evaluate.</param>
    /// <returns>A user-friendly positioning message.</returns>
    private static string GetPositioningMessage(DetectedFace face)
    {
        if (face.Confidence < AppConstants.MinFaceConfidence)
            return "Face unclear - adjust lighting";
        if (face.Width < AppConstants.MinFaceWidth)
            return "Move closer to the camera";
        if (face.Width > AppConstants.MaxFaceWidth)
            return "Move back from the camera";
        return "Center your face in the frame";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
