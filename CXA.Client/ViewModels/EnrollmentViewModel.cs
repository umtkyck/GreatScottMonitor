using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using CXA.Client.Services;
using CXA.Shared.Models;

namespace CXA.Client.ViewModels;

public class EnrollmentViewModel : INotifyPropertyChanged
{
    private readonly IFaceServiceClient _faceServiceClient;
    private readonly ICameraService _cameraService;
    private readonly IAuthenticationService _authenticationService;

    private string _currentStep = "Front";
    private int _capturedFrames = 0;
    private List<BitmapSource> _capturedFramesList = new();
    private string _statusMessage = "Position your face in the oval";

    public event PropertyChangedEventHandler? PropertyChanged;

    public EnrollmentViewModel(
        IFaceServiceClient faceServiceClient,
        ICameraService cameraService,
        IAuthenticationService authenticationService)
    {
        _faceServiceClient = faceServiceClient;
        _cameraService = cameraService;
        _authenticationService = authenticationService;
    }

    public string CurrentStep
    {
        get => _currentStep;
        set
        {
            _currentStep = value;
            OnPropertyChanged();
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

    public int CapturedFrames
    {
        get => _capturedFrames;
        set
        {
            _capturedFrames = value;
            OnPropertyChanged();
        }
    }

    public async Task<bool> CaptureFrameAsync()
    {
        var frame = await _cameraService.GetCurrentFrameAsync();
        if (frame == null)
            return false;

        // Validate frame quality
        var detectionResult = await _faceServiceClient.DetectFacesAsync(frame);
        if (!detectionResult.Success || detectionResult.Faces.Count != 1)
        {
            StatusMessage = "Please ensure only your face is visible";
            return false;
        }

        _capturedFramesList.Add(frame);
        CapturedFrames = _capturedFramesList.Count;
        return true;
    }

    public async Task<bool> CompleteEnrollmentAsync(string userId)
    {
        if (_capturedFramesList.Count < 5)
        {
            StatusMessage = "Please capture at least 5 frames";
            return false;
        }

        var result = await _authenticationService.EnrollAsync(userId, _capturedFramesList);
        return result.Success;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}






