using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;

namespace MedSecureVision.Client.Services;

public class CameraService : ICameraService
{
    private readonly ILogger<CameraService> _logger;
    private System.Drawing.Bitmap? _currentFrame;
    private readonly object _frameLock = new();
    private DispatcherTimer? _captureTimer;
    private int _selectedCameraIndex = 0;

    public event EventHandler<BitmapSource>? FrameCaptured;

    public CameraService(ILogger<CameraService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            // Check for available cameras
            var cameras = GetAvailableCameras();
            if (cameras.Count == 0)
            {
                _logger.LogWarning("No cameras found");
                throw new InvalidOperationException("No cameras available");
            }

            _selectedCameraIndex = cameras[0].Index;
            _logger.LogInformation($"Initialized camera service with {cameras.Count} camera(s)");
        });
    }

    public async Task StartCaptureAsync()
    {
        await Task.Run(() =>
        {
            _captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _captureTimer.Tick += OnCaptureTimerTick;
            _captureTimer.Start();
            _logger.LogInformation("Camera capture started");
        });
    }

    public Task StopCaptureAsync()
    {
        _captureTimer?.Stop();
        _captureTimer = null;
        _logger.LogInformation("Camera capture stopped");
        return Task.CompletedTask;
    }

    public async Task<BitmapSource?> GetCurrentFrameAsync()
    {
        return await Task.Run(() =>
        {
            lock (_frameLock)
            {
                if (_currentFrame == null)
                    return null;

                return ConvertBitmapToBitmapSource(_currentFrame);
            }
        });
    }

    public List<CameraInfo> GetAvailableCameras()
    {
        var cameras = new List<CameraInfo>();
        
        // Try to enumerate cameras (simplified - in production use DirectShow or MediaFoundation)
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var capture = new System.Drawing.Bitmap(640, 480);
                // In production, use actual camera enumeration
                cameras.Add(new CameraInfo
                {
                    Index = i,
                    Name = $"Camera {i}",
                    IsAvailable = true
                });
            }
            catch
            {
                break;
            }
        }

        return cameras;
    }

    public void SelectCamera(int cameraIndex)
    {
        _selectedCameraIndex = cameraIndex;
        _logger.LogInformation($"Selected camera {cameraIndex}");
    }

    private void OnCaptureTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var frame = CaptureFrame();
            if (frame != null)
            {
                lock (_frameLock)
                {
                    _currentFrame?.Dispose();
                    _currentFrame = frame;
                }

                var bitmapSource = ConvertBitmapToBitmapSource(frame);
                if (bitmapSource != null)
                {
                    FrameCaptured?.Invoke(this, bitmapSource);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing frame");
        }
    }

    private System.Drawing.Bitmap? CaptureFrame()
    {
        // Simplified frame capture - in production use DirectShow/MediaFoundation
        // For now, return a placeholder
        try
        {
            // This is a placeholder - actual implementation would use camera API
            var bitmap = new System.Drawing.Bitmap(640, 480);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.Black);
                g.DrawString("Camera Feed", 
                    new System.Drawing.Font("Arial", 20), 
                    System.Drawing.Brushes.White, 
                    new System.Drawing.PointF(200, 200));
            }
            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing frame from camera");
            return null;
        }
    }

    private BitmapSource? ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
    {
        try
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                PixelFormats.Bgr24,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            bitmapSource.Freeze();
            return bitmapSource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting bitmap to BitmapSource");
            return null;
        }
    }
}

