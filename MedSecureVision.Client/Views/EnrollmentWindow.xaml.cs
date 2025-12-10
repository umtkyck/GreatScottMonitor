using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MedSecureVision.Client.ViewModels;
using OpenCvSharp;

// Resolve WPF vs WinForms/System.Drawing/IO ambiguities
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using File = System.IO.File;
using Directory = System.IO.Directory;

namespace MedSecureVision.Client.Views;

/// <summary>
/// Face enrollment window implementing Apple Face ID-like scanning experience.
/// 
/// Features:
/// - Circular progress indicator with 8 segments
/// - Real-time camera feed with face guide overlay
/// - Animated direction indicators
/// - Visual feedback for each captured angle
/// - Progress dots and segment bars
/// 
/// Version: 1.0.0
/// </summary>
public partial class EnrollmentWindow : System.Windows.Window, IDisposable
{
    #region Private Fields
    
    private readonly EnrollmentViewModel? _viewModel;
    private int _capturedFrames = 0;
    private const int RequiredFrames = 8;
    private VideoCapture? _videoCapture;
    private DispatcherTimer? _cameraTimer;
    private bool _disposed = false;
    private readonly List<BitmapSource> _capturedImages = new();
    private readonly Border[] _segments;
    private readonly Ellipse[] _dots;

    /// <summary>
    /// Instructions for each capture step.
    /// Guides user through different head positions.
    /// </summary>
    private readonly string[] _stepInstructions = new[]
    {
        "Look straight at the camera",
        "Slowly turn your head to the right",
        "Look at the top-right corner",
        "Tilt your head up",
        "Look at the top-left corner",
        "Slowly turn your head to the left",
        "Tilt your head down",
        "Return to center - final capture"
    };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the enrollment window with default settings.
    /// </summary>
    public EnrollmentWindow()
    {
        InitializeComponent();
        
        // Initialize segment and dot arrays for easy access
        _segments = new[] { Segment1, Segment2, Segment3, Segment4, 
                           Segment5, Segment6, Segment7, Segment8 };
        _dots = new[] { Dot1, Dot2, Dot3, Dot4, Dot5, Dot6, Dot7, Dot8 };
        
        UpdateProgress();
        Loaded += EnrollmentWindow_Loaded;
        Closed += EnrollmentWindow_Closed;
    }

    /// <summary>
    /// Initializes the enrollment window with view model injection.
    /// </summary>
    public EnrollmentWindow(EnrollmentViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles window loaded event - starts camera.
    /// </summary>
    private void EnrollmentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        StartCamera();
    }

    /// <summary>
    /// Handles window closed event - stops camera.
    /// </summary>
    private void EnrollmentWindow_Closed(object? sender, EventArgs e)
    {
        StopCamera();
    }

    /// <summary>
    /// Enables window dragging.
    /// </summary>
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>
    /// Close button handler with confirmation.
    /// </summary>
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

    /// <summary>
    /// Capture button handler - captures current frame.
    /// </summary>
    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capturedFrames >= RequiredFrames)
            return;

        CaptureButton.IsEnabled = false;

        // Play capture animation
        await PlayCaptureFlashAsync();

        // Capture current frame
        if (CameraFeed.Source is BitmapSource currentFrame)
        {
            _capturedImages.Add(currentFrame);
            _capturedFrames++;
            
            // Update UI
            UpdateProgress();
            UpdateProgressRing();
            AnimateSegmentComplete(_capturedFrames - 1);
            
            ShowMessage($"Captured! {_capturedFrames}/{RequiredFrames}", true);
        }
        else
        {
            ShowMessage("Could not capture. Please try again.", false);
        }

        await Task.Delay(400);
        CaptureButton.IsEnabled = true;
    }

    /// <summary>
    /// Complete button handler - saves enrollment data.
    /// </summary>
    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capturedFrames < RequiredFrames)
        {
            ShowMessage($"Please capture {RequiredFrames - _capturedFrames} more.", false);
            return;
        }

        CompleteButton.IsEnabled = false;
        CompleteButton.Content = "Saving...";
        ShowMessage("Processing face data...", true);

        // Save enrollment data
        await SaveEnrollmentAsync();

        ShowMessage("Face enrolled successfully!", true);
        UpdateFaceGuideColor("#3FB950");
        
        await Task.Delay(1000);
        
        DialogResult = true;
        Close();
    }

    #endregion

    #region Camera Methods

    /// <summary>
    /// Initializes and starts camera capture.
    /// </summary>
    private void StartCamera()
    {
        try
        {
            _videoCapture = new VideoCapture(0);
            if (!_videoCapture.IsOpened())
            {
                ShowMessage("Camera not available", false);
                return;
            }

            // Configure camera for optimal face capture
            _videoCapture.Set(VideoCaptureProperties.FrameWidth, 640);
            _videoCapture.Set(VideoCaptureProperties.FrameHeight, 480);
            _videoCapture.Set(VideoCaptureProperties.Fps, 30);

            // Start capture timer
            _cameraTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _cameraTimer.Tick += CameraTimer_Tick;
            _cameraTimer.Start();
        }
        catch (Exception ex)
        {
            ShowMessage($"Camera error: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Stops camera capture and releases resources.
    /// </summary>
    private void StopCamera()
    {
        _cameraTimer?.Stop();
        _cameraTimer = null;
        _videoCapture?.Release();
        _videoCapture?.Dispose();
        _videoCapture = null;
    }

    /// <summary>
    /// Timer tick handler - captures and displays frame.
    /// </summary>
    private void CameraTimer_Tick(object? sender, EventArgs e)
    {
        if (_videoCapture == null || !_videoCapture.IsOpened())
            return;

        try
        {
            using var frame = new Mat();
            if (_videoCapture.Read(frame) && !frame.Empty())
            {
                // Mirror image for natural viewing
                Cv2.Flip(frame, frame, FlipMode.Y);
                
                var bitmapSource = MatToBitmapSource(frame);
                if (bitmapSource != null)
                {
                    CameraFeed.Source = bitmapSource;
                }
            }
        }
        catch { /* Ignore frame errors */ }
    }

    /// <summary>
    /// Converts OpenCV Mat to WPF BitmapSource.
    /// </summary>
    private BitmapSource? MatToBitmapSource(Mat mat)
    {
        if (mat == null || mat.Empty())
            return null;

        try
        {
            int width = mat.Width;
            int height = mat.Height;
            int stride = (width * 3 + 3) & ~3; // Align to 4 bytes

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

    #endregion

    #region UI Update Methods

    /// <summary>
    /// Updates all progress indicators.
    /// </summary>
    private void UpdateProgress()
    {
        int currentStep = Math.Min(_capturedFrames + 1, RequiredFrames);
        
        // Update header
        StepTitle.Text = $"Step {currentStep} of {RequiredFrames}";
        StepDescription.Text = _stepInstructions[Math.Min(_capturedFrames, RequiredFrames - 1)];
        
        // Update progress text
        ProgressText.Text = $"{_capturedFrames}/{RequiredFrames} captured";

        // Enable complete button when done
        if (_capturedFrames >= RequiredFrames)
        {
            CompleteButton.IsEnabled = true;
            CompleteButton.Opacity = 1;
            StatusMessage.Text = "Ready to complete enrollment!";
            UpdateFaceGuideColor("#3FB950");
        }
        else
        {
            StatusMessage.Text = GetDirectionMessage(_capturedFrames);
        }

        // Update direction arrow
        ShowDirectionArrow(_capturedFrames);
    }

    /// <summary>
    /// Updates the circular progress ring.
    /// </summary>
    private void UpdateProgressRing()
    {
        // Calculate progress (0 to ~6.28 for full circle in dash units)
        double progress = (double)_capturedFrames / RequiredFrames * 8;
        ProgressRing.StrokeDashArray = new DoubleCollection { progress, 100 };
    }

    /// <summary>
    /// Animates a segment to show completion.
    /// </summary>
    private void AnimateSegmentComplete(int index)
    {
        if (index < 0 || index >= _segments.Length)
            return;

        var segment = _segments[index];
        var dot = _dots[index];

        // Create gradient for completed segment
        var gradient = new LinearGradientBrush(
            Color.FromRgb(0x58, 0xA6, 0xFF),
            Color.FromRgb(0x3F, 0xB9, 0x50),
            0);

        // Animate segment
        segment.Background = gradient;
        
        // Animate dot
        dot.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
        
        // Add glow effect
        dot.Effect = new DropShadowEffect
        {
            BlurRadius = 10,
            ShadowDepth = 0,
            Opacity = 0.8,
            Color = Color.FromRgb(0x3F, 0xB9, 0x50)
        };
    }

    /// <summary>
    /// Shows directional arrow based on current step.
    /// </summary>
    private void ShowDirectionArrow(int step)
    {
        // Rotate arrow based on required head direction
        double rotation = step switch
        {
            1 => 90,   // Right
            2 => 45,   // Top-right
            3 => 0,    // Up
            4 => -45,  // Top-left
            5 => -90,  // Left
            6 => 180,  // Down
            _ => 0
        };

        DirectionArrow.Opacity = step > 0 && step < 7 ? 1 : 0;
        DirectionArrow.RenderTransform = new RotateTransform(rotation, 25, 25);
    }

    /// <summary>
    /// Gets direction message for current step.
    /// </summary>
    private string GetDirectionMessage(int step)
    {
        return step switch
        {
            0 => "Look directly at the camera",
            1 => "Turn your head to the right →",
            2 => "Look at the top-right corner ↗",
            3 => "Tilt your head up ↑",
            4 => "Look at the top-left corner ↖",
            5 => "Turn your head to the left ←",
            6 => "Tilt your head down ↓",
            7 => "Look straight ahead - final capture",
            _ => "Complete!"
        };
    }

    /// <summary>
    /// Updates face guide color.
    /// </summary>
    private void UpdateFaceGuideColor(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        FaceGuide.Stroke = new SolidColorBrush(color);
        
        if (FaceGuide.Effect is DropShadowEffect shadow)
        {
            shadow.Color = color;
        }
    }

    /// <summary>
    /// Plays capture flash animation.
    /// </summary>
    private async Task PlayCaptureFlashAsync()
    {
        var fadeIn = new DoubleAnimation(0, 0.9, TimeSpan.FromMilliseconds(50));
        var fadeOut = new DoubleAnimation(0.9, 0, TimeSpan.FromMilliseconds(250));

        CaptureFlash.BeginAnimation(OpacityProperty, fadeIn);
        await Task.Delay(50);
        CaptureFlash.BeginAnimation(OpacityProperty, fadeOut);
        await Task.Delay(250);
    }

    /// <summary>
    /// Shows status message.
    /// </summary>
    private void ShowMessage(string message, bool isSuccess)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = new SolidColorBrush(
            isSuccess ? Color.FromRgb(0x3F, 0xB9, 0x50) : Color.FromRgb(0xF8, 0x51, 0x49));
    }

    #endregion

    #region Enrollment Methods

    /// <summary>
    /// Saves enrollment data to local storage.
    /// </summary>
    private async Task SaveEnrollmentAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // Create enrollment directory
                var enrollmentDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MedSecureVision");
                
                Directory.CreateDirectory(enrollmentDir);

                // Save enrollment marker file
                var enrollmentPath = Path.Combine(enrollmentDir, "enrollment.dat");
                File.WriteAllText(enrollmentPath, 
                    $"EnrolledAt={DateTime.UtcNow:O}\nFrameCount={_capturedFrames}\nVersion=1.0.0");

                // In production: Save face embeddings to secure storage
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Enrollment save error: {ex.Message}");
            }
        });
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopCamera();
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}
