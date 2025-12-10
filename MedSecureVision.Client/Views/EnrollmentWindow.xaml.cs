using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MedSecureVision.Client.Services;
using MedSecureVision.Client.ViewModels;
using OpenCvSharp;

namespace MedSecureVision.Client.Views;

/// <summary>
/// Face enrollment window for registering user biometric data.
/// Guides users through a multi-step face capture process.
/// </summary>
public partial class EnrollmentWindow : System.Windows.Window, IDisposable
{
    private readonly EnrollmentViewModel? _viewModel;
    private int _capturedFrames = 0;
    private const int RequiredFrames = 8;
    private VideoCapture? _videoCapture;
    private DispatcherTimer? _cameraTimer;
    private bool _disposed = false;
    private List<BitmapSource> _capturedImages = new();

    private readonly string[] _stepInstructions = new[]
    {
        "Look straight at the camera",
        "Turn your head slightly left",
        "Turn your head slightly right",
        "Tilt your head up slightly",
        "Tilt your head down slightly",
        "Move closer to the camera",
        "Move back from the camera",
        "Final capture - look straight"
    };

    /// <summary>
    /// Default constructor for design-time and standalone use.
    /// </summary>
    public EnrollmentWindow()
    {
        InitializeComponent();
        UpdateProgress();
        Loaded += EnrollmentWindow_Loaded;
        Closed += EnrollmentWindow_Closed;
    }

    /// <summary>
    /// Constructor with dependency injection for enrollment view model.
    /// </summary>
    public EnrollmentWindow(EnrollmentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        UpdateProgress();
        Loaded += EnrollmentWindow_Loaded;
        Closed += EnrollmentWindow_Closed;
    }

    private void EnrollmentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Start camera
        StartCamera();
    }

    private void EnrollmentWindow_Closed(object? sender, EventArgs e)
    {
        StopCamera();
    }

    private void StartCamera()
    {
        try
        {
            _videoCapture = new VideoCapture(0);
            if (!_videoCapture.IsOpened())
            {
                StatusMessage.Text = "Camera not available";
                return;
            }

            _videoCapture.Set(VideoCaptureProperties.FrameWidth, 640);
            _videoCapture.Set(VideoCaptureProperties.FrameHeight, 480);
            _videoCapture.Set(VideoCaptureProperties.Fps, 30);

            _cameraTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _cameraTimer.Tick += CameraTimer_Tick;
            _cameraTimer.Start();
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Camera error: {ex.Message}";
        }
    }

    private void StopCamera()
    {
        _cameraTimer?.Stop();
        _cameraTimer = null;
        _videoCapture?.Release();
        _videoCapture?.Dispose();
        _videoCapture = null;
    }

    private void CameraTimer_Tick(object? sender, EventArgs e)
    {
        if (_videoCapture == null || !_videoCapture.IsOpened())
            return;

        try
        {
            using var frame = new Mat();
            if (_videoCapture.Read(frame) && !frame.Empty())
            {
                // Mirror the image
                Cv2.Flip(frame, frame, FlipMode.Y);
                
                var bitmapSource = MatToBitmapSource(frame);
                if (bitmapSource != null)
                {
                    CameraFeed.Source = bitmapSource;
                }
            }
        }
        catch { }
    }

    private BitmapSource? MatToBitmapSource(Mat mat)
    {
        if (mat == null || mat.Empty())
            return null;

        try
        {
            int width = mat.Width;
            int height = mat.Height;
            int stride = (width * 3 + 3) & ~3;

            byte[] pixels = new byte[height * stride];
            for (int y = 0; y < height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(mat.Ptr(y), pixels, y * stride, width * 3);
            }

            var bitmapSource = BitmapSource.Create(
                width, height, 96, 96,
                PixelFormats.Bgr24, null, pixels, stride);
            bitmapSource.Freeze();
            return bitmapSource;
        }
        catch
        {
            return null;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capturedFrames > 0)
        {
            var result = MessageBox.Show(
                "You have captured frames. Are you sure you want to cancel enrollment?",
                "Cancel Enrollment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        DialogResult = false;
        Close();
    }

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureButton.IsEnabled = false;

        // Play capture flash animation
        await PlayCaptureFlashAsync();

        // Capture current frame
        if (CameraFeed.Source is BitmapSource currentFrame)
        {
            _capturedImages.Add(currentFrame);
            _capturedFrames++;
            UpdateProgress();
            
            // Play success sound or visual feedback
            ShowMessage($"Captured! ({_capturedFrames}/{RequiredFrames})", true);
        }
        else
        {
            ShowMessage("Could not capture frame. Please try again.", false);
        }

        // Small delay before re-enabling
        await Task.Delay(500);
        CaptureButton.IsEnabled = true;
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capturedFrames < RequiredFrames)
        {
            ShowMessage($"Please capture {RequiredFrames - _capturedFrames} more frames.", false);
            return;
        }

        CompleteButton.IsEnabled = false;
        CompleteButton.Content = "Saving...";
        ShowMessage("Processing face data...", true);

        // Simulate processing
        await Task.Delay(1500);
        
        ShowMessage("Face enrolled successfully!", true);
        UpdateFaceGuideColor("#3FB950");
        
        await Task.Delay(1000);
        
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseButton_Click(sender, e);
    }

    private void UpdateProgress()
    {
        int currentStep = Math.Min(_capturedFrames + 1, RequiredFrames);
        StepTitle.Text = $"Step {currentStep} of {RequiredFrames}";
        StepDescription.Text = _stepInstructions[Math.Min(_capturedFrames, RequiredFrames - 1)];
        ProgressText.Text = $"{_capturedFrames}/{RequiredFrames}";

        double progress = (double)_capturedFrames / RequiredFrames;
        ProgressRing.StrokeDashArray = new DoubleCollection { progress * 4, 4 };

        if (_capturedFrames >= RequiredFrames)
        {
            CompleteButton.IsEnabled = true;
            CompleteButton.Opacity = 1;
            StatusMessage.Text = "Ready to complete enrollment!";
            UpdateFaceGuideColor("#3FB950");
        }
        else
        {
            StatusMessage.Text = _stepInstructions[_capturedFrames];
        }

        ShowDirectionArrow(_capturedFrames);
    }

    private void ShowDirectionArrow(int step)
    {
        switch (step)
        {
            case 1:
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(180, 15, 15);
                break;
            case 2:
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(0, 15, 15);
                break;
            case 3:
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(-90, 15, 15);
                break;
            case 4:
                DirectionArrow.Opacity = 1;
                DirectionArrow.RenderTransform = new RotateTransform(90, 15, 15);
                break;
            default:
                DirectionArrow.Opacity = 0;
                break;
        }
    }

    private void UpdateFaceGuideColor(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        FaceGuide.Stroke = new SolidColorBrush(color);
        if (FaceGuide.Effect is DropShadowEffect shadow)
        {
            shadow.Color = color;
        }
    }

    private async Task PlayCaptureFlashAsync()
    {
        var fadeIn = new DoubleAnimation(0, 0.8, TimeSpan.FromMilliseconds(50));
        var fadeOut = new DoubleAnimation(0.8, 0, TimeSpan.FromMilliseconds(200));

        CaptureFlash.BeginAnimation(OpacityProperty, fadeIn);
        await Task.Delay(50);
        CaptureFlash.BeginAnimation(OpacityProperty, fadeOut);
        await Task.Delay(200);
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = new SolidColorBrush(
            isSuccess ? Color.FromRgb(0x3F, 0xB9, 0x50) : Color.FromRgb(0xF8, 0x51, 0x49));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopCamera();
            GC.SuppressFinalize(this);
        }
    }
}
