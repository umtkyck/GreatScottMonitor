using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CXA.Client.Constants;
using CXA.Client.Helpers;
using CXA.Client.ViewModels;
using CXA.Client.Views;
using OpenCvSharp;

// Resolve WPF vs WinForms/System.Drawing ambiguities
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using MessageBox = System.Windows.MessageBox;

namespace CXA.Client;

/// <summary>
/// Main authentication window for CXA.
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
            
            // Initial state check
            if (viewModel.AuthenticationState == "NotEnrolled")
                UpdateUIForNotEnrolled();
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

            // Configure camera using constants
            _videoCapture.Set(VideoCaptureProperties.FrameWidth, AppConstants.CameraFrameWidth);
            _videoCapture.Set(VideoCaptureProperties.FrameHeight, AppConstants.CameraFrameHeight);
            _videoCapture.Set(VideoCaptureProperties.Fps, AppConstants.CameraFps);

            // Start capture timer
            _cameraTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AppConstants.CameraTimerIntervalMs)
            };
            _cameraTimer.Tick += CameraTimer_Tick;
            _cameraTimer.Start();
            _cameraStarted = true;

            // Only set text if not overridden by VM
            if (StatusTitle.Text == "Looking for face...")
            {
                StatusTitle.Text = "Looking for face...";
                StatusDescription.Text = "Position your face within the frame";
            }
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
                ImageHelper.FlipHorizontal(frame);

                var bitmapSource = ImageHelper.MatToBitmapSource(frame);
                if (bitmapSource != null)
                {
                    CameraFeed.Source = bitmapSource;
                }
            }
        }
        catch
        {
            // Ignore individual frame capture errors to maintain smooth operation
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
                case "LockedOut":
                    UpdateUIForLockedOut();
                    break;
                case "NotEnrolled":
                    UpdateUIForNotEnrolled();
                    break;
            }
        });
    }

    private void UpdateUIForSearching()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.SearchingColor);
        StatusTitle.Text = "Looking for face...";
        StatusDescription.Text = "Position your face within the frame";
        UpdateFaceGuideColor(AppConstants.SearchingColorHex);
    }

    private void UpdateUIForPositioning()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.WarningColor);
        StatusTitle.Text = "Adjusting...";
        StatusDescription.Text = "Move closer and center your face";
        UpdateFaceGuideColor(AppConstants.WarningColorHex);
    }

    private void UpdateUIForVerifying()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.VerifyingColor);
        StatusTitle.Text = "Verifying identity...";
        StatusDescription.Text = "Please hold still";
        UpdateFaceGuideColor(AppConstants.VerifyingColorHex);
    }

    private void UpdateUIForLockedOut()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.ErrorColor);
        StatusTitle.Text = "Biometrics Locked";
        StatusDescription.Text = "Too many attempts. Use PIN to unlock.";
        UpdateFaceGuideColor(AppConstants.ErrorColorHex);
        StopCamera(); // Security measure
    }

    private void UpdateUIForNotEnrolled()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.WarningColor);
        StatusTitle.Text = "Not Enrolled";
        StatusDescription.Text = "Please setup Face ID from Dashboard";
        UpdateFaceGuideColor(AppConstants.WarningColorHex);
    }

    private void UpdateFaceGuideColor(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        
        FaceGuideGlow.Fill = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5, RadiusY = 0.5,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(80, color.R, color.G, color.B), 0.7),
                new GradientStop(color, 1)
            }
        };
    }

    private void PlaySuccessAnimation()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.SuccessColor);
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

        UpdateFaceGuideColor(AppConstants.SuccessColorHex);
    }

    private void PlayFailureAnimation()
    {
        StatusDot.Fill = new SolidColorBrush(AppConstants.ErrorColor);
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
        // Shake the whole circular mask container if possible, but FaceGuideGlow is simpler
        FaceGuideGlow.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);

        UpdateFaceGuideColor(AppConstants.ErrorColorHex);
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
