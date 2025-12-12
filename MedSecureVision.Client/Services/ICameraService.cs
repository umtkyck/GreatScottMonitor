using System.Windows.Media.Imaging;

namespace MedSecureVision.Client.Services;

public interface ICameraService
{
    event EventHandler<BitmapSource>? FrameCaptured;
    Task InitializeAsync();
    Task StartCaptureAsync();
    Task StopCaptureAsync();
    Task<BitmapSource?> GetCurrentFrameAsync();
    List<CameraInfo> GetAvailableCameras();
    void SelectCamera(int cameraIndex);
}

public class CameraInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}


