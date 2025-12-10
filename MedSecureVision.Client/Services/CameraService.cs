using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace MedSecureVision.Client.Services;

/// <summary>
/// Camera service that captures frames from the webcam using OpenCvSharp.
/// Provides real-time video feed for face detection and authentication.
/// </summary>
public class CameraService : ICameraService, IDisposable
{
    private readonly ILogger<CameraService> _logger;
    private VideoCapture? _videoCapture;
    private Mat? _currentFrame;
    private readonly object _frameLock = new();
    private DispatcherTimer? _captureTimer;
    private int _selectedCameraIndex = 0;
    private bool _isCapturing = false;
    private bool _disposed = false;

    public event EventHandler<BitmapSource>? FrameCaptured;

    public CameraService(ILogger<CameraService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the camera service and detect available cameras.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // Try to find available cameras
                var cameras = GetAvailableCameras();
                if (cameras.Count == 0)
                {
                    _logger.LogWarning("No cameras found");
                    throw new InvalidOperationException("No cameras available");
                }

                _selectedCameraIndex = cameras[0].Index;
                _logger.LogInformation($"Initialized camera service with {cameras.Count} camera(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing camera service");
                throw;
            }
        });
    }

    /// <summary>
    /// Start capturing frames from the selected camera.
    /// </summary>
    public async Task StartCaptureAsync()
    {
        if (_isCapturing)
        {
            _logger.LogWarning("Camera is already capturing");
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                // Initialize video capture
                _videoCapture = new VideoCapture(_selectedCameraIndex);
                
                if (!_videoCapture.IsOpened())
                {
                    _logger.LogError($"Failed to open camera at index {_selectedCameraIndex}");
                    throw new InvalidOperationException($"Failed to open camera {_selectedCameraIndex}");
                }

                // Set camera properties for optimal performance
                // Use 4:3 aspect ratio to better match the face guide oval
                _videoCapture.Set(VideoCaptureProperties.FrameWidth, 640);
                _videoCapture.Set(VideoCaptureProperties.FrameHeight, 480);
                _videoCapture.Set(VideoCaptureProperties.Fps, 30);

                _currentFrame = new Mat();
                _isCapturing = true;

                _logger.LogInformation($"Camera {_selectedCameraIndex} opened successfully at " +
                    $"{_videoCapture.Get(VideoCaptureProperties.FrameWidth)}x{_videoCapture.Get(VideoCaptureProperties.FrameHeight)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting camera capture");
                throw;
            }
        });

        // Start capture timer on UI thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _captureTimer.Tick += OnCaptureTimerTick;
            _captureTimer.Start();
            _logger.LogInformation("Camera capture timer started");
        });
    }

    /// <summary>
    /// Stop capturing frames and release camera resources.
    /// </summary>
    public Task StopCaptureAsync()
    {
        _isCapturing = false;

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            _captureTimer?.Stop();
            _captureTimer = null;
        });

        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = null;
        }

        _videoCapture?.Release();
        _videoCapture?.Dispose();
        _videoCapture = null;

        _logger.LogInformation("Camera capture stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the current frame as a BitmapSource for display.
    /// </summary>
    public async Task<BitmapSource?> GetCurrentFrameAsync()
    {
        return await Task.Run(() =>
        {
            lock (_frameLock)
            {
                if (_currentFrame == null || _currentFrame.Empty())
                    return null;

                try
                {
                    return MatToBitmapSource(_currentFrame);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error converting frame to BitmapSource");
                    return null;
                }
            }
        });
    }

    /// <summary>
    /// Get list of available cameras on the system.
    /// </summary>
    public List<CameraInfo> GetAvailableCameras()
    {
        var cameras = new List<CameraInfo>();

        // Try to enumerate cameras (check first 5 indices)
        for (int i = 0; i < 5; i++)
        {
            try
            {
                using var testCapture = new VideoCapture(i);
                if (testCapture.IsOpened())
                {
                    var width = testCapture.Get(VideoCaptureProperties.FrameWidth);
                    var height = testCapture.Get(VideoCaptureProperties.FrameHeight);
                    
                    cameras.Add(new CameraInfo
                    {
                        Index = i,
                        Name = $"Camera {i} ({width}x{height})",
                        IsAvailable = true
                    });
                    
                    _logger.LogInformation($"Found camera {i}: {width}x{height}");
                    testCapture.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Camera {i} not available: {ex.Message}");
            }
        }

        return cameras;
    }

    /// <summary>
    /// Select a different camera by index.
    /// </summary>
    public void SelectCamera(int cameraIndex)
    {
        if (_selectedCameraIndex != cameraIndex)
        {
            _selectedCameraIndex = cameraIndex;
            _logger.LogInformation($"Selected camera {cameraIndex}");

            // If currently capturing, restart with new camera
            if (_isCapturing)
            {
                _ = Task.Run(async () =>
                {
                    await StopCaptureAsync();
                    await StartCaptureAsync();
                });
            }
        }
    }

    /// <summary>
    /// Capture timer tick - reads a frame from the camera.
    /// </summary>
    private void OnCaptureTimerTick(object? sender, EventArgs e)
    {
        if (!_isCapturing || _videoCapture == null || !_videoCapture.IsOpened())
            return;

        try
        {
            var frame = new Mat();
            
            if (_videoCapture.Read(frame) && !frame.Empty())
            {
                // Flip horizontally for mirror effect (like a selfie camera)
                Cv2.Flip(frame, frame, FlipMode.Y);

                lock (_frameLock)
                {
                    _currentFrame?.Dispose();
                    _currentFrame = frame.Clone();
                }

                // Convert to BitmapSource and raise event
                try
                {
                    var bitmapSource = MatToBitmapSource(frame);
                    if (bitmapSource != null)
                    {
                        FrameCaptured?.Invoke(this, bitmapSource);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error converting frame for event");
                }
            }
            
            frame.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing frame");
        }
    }

    /// <summary>
    /// Convert OpenCV Mat to WPF BitmapSource.
    /// </summary>
    private BitmapSource? MatToBitmapSource(Mat mat)
    {
        if (mat == null || mat.Empty())
            return null;

        try
        {
            // Convert to BGR if necessary
            Mat displayMat = mat;
            if (mat.Channels() == 1)
            {
                displayMat = new Mat();
                Cv2.CvtColor(mat, displayMat, ColorConversionCodes.GRAY2BGR);
            }
            else if (mat.Channels() == 4)
            {
                displayMat = new Mat();
                Cv2.CvtColor(mat, displayMat, ColorConversionCodes.BGRA2BGR);
            }

            int width = displayMat.Width;
            int height = displayMat.Height;
            int stride = width * 3; // BGR = 3 bytes per pixel
            
            // Ensure stride is aligned to 4 bytes for WPF
            stride = (stride + 3) & ~3;

            byte[] pixels = new byte[height * stride];
            
            // Copy pixel data row by row
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(displayMat.Ptr(y), pixels, y * stride, width * 3);
            }

            var bitmapSource = BitmapSource.Create(
                width, height,
                96, 96, // DPI
                PixelFormats.Bgr24,
                null,
                pixels,
                stride);

            bitmapSource.Freeze();

            if (displayMat != mat)
            {
                displayMat.Dispose();
            }

            return bitmapSource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MatToBitmapSource");
            return null;
        }
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopCaptureAsync().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
