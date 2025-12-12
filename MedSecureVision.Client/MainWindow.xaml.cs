using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MedSecureVision.Client.ViewModels;
using MedSecureVision.Client.Views;
using OpenCvSharp;

// Resolve WPF vs WinForms/System.Drawing ambiguities
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using MessageBox = System.Windows.MessageBox;

namespace MedSecureVision.Client;

/// <summary>
/// Main authentication window for MedSecure Vision.
/// Provides Face ID-like biometric authentication experience.
/// </summary>
public partial class MainWindow : System.Windows.Window, IDisposable
{
    private VideoCapture? _videoCapture;
    private DispatcherTimer? _cameraTimer;
    private bool _disposed = false;
    private bool _cameraStarted = false;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Start camera directly
        StartCamera();
        
        // Setup ViewModel events if available
        if (DataContext is AuthenticationViewModel viewModel)
        {
            viewModel.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        StopCamera();
    }

    #region Camera Methods

    /// <summary>
    /// Start camera capture for the main window.
    /// </summary>
    private void StartCamera()
    {
        if (_cameraStarted) return;

        try
        {
            _videoCapture = new VideoCapture(0);
            if (!_videoCapture.IsOpened())
            {
                StatusTitle.Text = "Camera not available";
                StatusDescription.Text = "Please check your camera connection";
                return;
            }

            // Configure camera
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
            _cameraStarted = true;

            StatusTitle.Text = "Looking for face...";
            StatusDescription.Text = "Position your face within the frame";
        }
        catch (Exception ex)
        {
            StatusTitle.Text = "Camera Error";
            StatusDescription.Text = ex.Message;
        }
    }

    /// <summary>
    /// Stop camera capture.
    /// </summary>
    private void StopCamera()
    {
        _cameraTimer?.Stop();
        _cameraTimer = null;
        _videoCapture?.Release();
        _videoCapture?.Dispose();
        _videoCapture = null;
        _cameraStarted = false;
    }

    /// <summary>
    /// Camera timer tick - capture and display frame.
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
    /// Convert OpenCV Mat to WPF BitmapSource.
    /// </summary>
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

    #endregion

    #region Authentication State

    private void OnAuthenticationStateChanged(object? sender, string state)
    {
        Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case "Searching":
                    UpdateUIForSearching();
                    break;
                case "Positioning":
                    UpdateUIForPositioning();
                    break;
                case "Verifying":
                    UpdateUIForVerifying();
                    break;
                case "Success":
                    PlaySuccessAnimation();
                    break;
                case "Failure":
                    PlayFailureAnimation();
                    break;
            }
        });
    }

    private void UpdateUIForSearching()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
        StatusTitle.Text = "Looking for face...";
        StatusDescription.Text = "Position your face within the frame";
        UpdateFaceGuideColor("#58A6FF");
    }

    private void UpdateUIForPositioning()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xD2, 0x99, 0x22));
        StatusTitle.Text = "Adjusting...";
        StatusDescription.Text = "Move closer and center your face";
        UpdateFaceGuideColor("#D29922");
    }

    private void UpdateUIForVerifying()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xA3, 0x71, 0xF7));
        StatusTitle.Text = "Verifying identity...";
        StatusDescription.Text = "Please hold still";
        UpdateFaceGuideColor("#A371F7");
    }

    private void UpdateFaceGuideColor(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        
        FaceGuide.Stroke = new LinearGradientBrush(
            color,
            Color.FromRgb(0xA3, 0x71, 0xF7),
            new Point(0, 0),
            new Point(1, 1));
            
        if (FaceGuide.Effect is DropShadowEffect shadow)
        {
            shadow.Color = color;
        }
    }

    private void PlaySuccessAnimation()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
        StatusTitle.Text = "Welcome!";
        StatusDescription.Text = "Authentication successful";

        SuccessCheckmark.Visibility = Visibility.Visible;

        var scaleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
        {
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 5 }
        };

        var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));

        var scaleTransform = new ScaleTransform(0, 0);
        SuccessCheckmark.RenderTransform = scaleTransform;
        SuccessCheckmark.RenderTransformOrigin = new Point(0.5, 0.5);

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        SuccessCheckmark.BeginAnimation(OpacityProperty, opacityAnimation);

        UpdateFaceGuideColor("#3FB950");
    }

    private void PlayFailureAnimation()
    {
        StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
        StatusTitle.Text = "Not recognized";
        StatusDescription.Text = "Please try again or use PIN";

        var shakeAnimation = new DoubleAnimationUsingKeyFrames();
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(0)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-15, TimeSpan.FromMilliseconds(50)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(15, TimeSpan.FromMilliseconds(100)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-15, TimeSpan.FromMilliseconds(150)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(15, TimeSpan.FromMilliseconds(200)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(-10, TimeSpan.FromMilliseconds(250)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(10, TimeSpan.FromMilliseconds(300)));
        shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(350)));

        var transform = new TranslateTransform();
        FaceGuide.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);

        UpdateFaceGuideColor("#F85149");
    }

    #endregion

    #region Window Event Handlers

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Hide to tray instead of closing
        Hide();
    }

    #endregion

    #region Action Buttons

    private void FallbackAuth_Click(object sender, RoutedEventArgs e)
    {
        var fallbackWindow = new FallbackAuthWindow();
        fallbackWindow.Owner = this;
        fallbackWindow.ShowDialog();
    }

    private void EnrollFace_Click(object sender, RoutedEventArgs e)
    {
        // Stop main camera while enrolling
        StopCamera();
        
        var enrollmentWindow = new EnrollmentWindow();
        enrollmentWindow.Owner = this;
        var result = enrollmentWindow.ShowDialog();
        
        // Restart camera
        StartCamera();
        
        if (result == true)
        {
            StatusTitle.Text = "Face enrolled!";
            StatusDescription.Text = "You can now authenticate with your face";
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
        }
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        var dashboard = new DashboardWindow();
        dashboard.Show();
    }

    #endregion

    #region IDisposable

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
