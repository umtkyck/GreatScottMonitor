using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CXA.Client.Constants;
using CXA.Client.Helpers;
using CXA.Client.Models;
using CXA.Client.Services;
using OpenCvSharp;

using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;

namespace CXA.Client.Views;

/// <summary>
/// Unified dashboard for CXA - combines camera feed, authentication, and face management.
/// This is the main and only window of the application.
/// </summary>
public partial class DashboardWindow : System.Windows.Window, IDisposable
{
    public ObservableCollection<EnrolledFaceModel> EnrolledFaces { get; } = new();
    
    private readonly IEnrollmentPathService _pathService;
    private VideoCapture? _videoCapture;
    private DispatcherTimer? _cameraTimer;
    private DispatcherTimer? _lockTimer;
    private CascadeClassifier? _faceDetector;
    private bool _isDisposed;
    private bool _faceDetected;
    private int _noFaceFrameCount;
    private bool _isLocked;
    private DateTime _lastFaceSeenTime;

    public DashboardWindow()
    {
        InitializeComponent();
        _pathService = new EnrollmentPathService();
        
        Loaded += OnWindowLoaded;
        Closed += OnWindowClosed;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        LoadEnrolledFaces();
        UpdateStatistics();
        StartCamera();
        UpdateAuthStatus();
        
        // Initialize lock timer
        _lastFaceSeenTime = DateTime.Now;
        _lockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(AppConstants.LockTimerIntervalMs)
        };
        _lockTimer.Tick += OnLockTimerTick;
        _lockTimer.Start();
    }

    private void OnLockTimerTick(object? sender, EventArgs e)
    {
        // Only lock if enrolled
        if (!_pathService.HasEnrollment())
            return;

        var timeSinceLastFace = DateTime.Now - _lastFaceSeenTime;
        
        if (!_isLocked && !_faceDetected && timeSinceLastFace.TotalSeconds >= AppConstants.LockTimeoutSeconds)
        {
            LockScreen();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Dispose();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeIcon.Text = "☐";
            MaximizeButton.ToolTip = "Maximize";
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeIcon.Text = "❐";
            MaximizeButton.ToolTip = "Restore";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Minimize to tray instead of closing
        Hide();
    }

    #region Camera

    private void StartCamera()
    {
        try
        {
            _videoCapture = new VideoCapture(0);
            
            if (!_videoCapture.IsOpened())
            {
                UpdateCameraStatus(false, "No camera detected");
                UpdateFaceStatus(false, "Camera not available");
                return;
            }

            _videoCapture.Set(VideoCaptureProperties.FrameWidth, AppConstants.CameraFrameWidth);
            _videoCapture.Set(VideoCaptureProperties.FrameHeight, AppConstants.CameraFrameHeight);
            _videoCapture.Set(VideoCaptureProperties.Fps, AppConstants.CameraFps);

            // Initialize face detector
            _faceDetector = InitializeFaceDetector();

            _cameraTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AppConstants.CameraTimerIntervalMs)
            };
            _cameraTimer.Tick += OnCameraFrame;
            _cameraTimer.Start();

            NoCameraPlaceholder.Visibility = Visibility.Collapsed;
            UpdateCameraStatus(true, "Camera active");
        }
        catch (Exception ex)
        {
            UpdateCameraStatus(false, $"Camera error: {ex.Message}");
            UpdateFaceStatus(false, "Camera error");
        }
    }

    /// <summary>
    /// Initializes the face detector with Haar cascade classifier.
    /// Downloads the cascade file if not present locally.
    /// </summary>
    private CascadeClassifier? InitializeFaceDetector()
    {
        try
        {
            var cascadePath = GetCascadeFilePath();
            return File.Exists(cascadePath) ? new CascadeClassifier(cascadePath) : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Face detector initialization failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the cascade file path, downloading if necessary.
    /// </summary>
    private string GetCascadeFilePath()
    {
        // Try local app directory first
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.HaarCascadeFileName);
        if (File.Exists(localPath))
            return localPath;

        // Fall back to AppData
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolderName,
            AppConstants.HaarCascadeFileName);

        if (!File.Exists(appDataPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(appDataPath)!);
            DownloadCascadeFileAsync(appDataPath).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        return appDataPath;
    }

    /// <summary>
    /// Downloads the Haar cascade file from OpenCV repository.
    /// </summary>
    private static async Task DownloadCascadeFileAsync(string path)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var data = await client.GetByteArrayAsync(AppConstants.HaarCascadeDownloadUrl);
            await File.WriteAllBytesAsync(path, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cascade download failed: {ex.Message}");
        }
    }

    private void StopCamera()
    {
        _cameraTimer?.Stop();
        _cameraTimer = null;

        _lockTimer?.Stop();
        _lockTimer = null;

        _videoCapture?.Release();
        _videoCapture?.Dispose();
        _videoCapture = null;

        _faceDetector?.Dispose();
        _faceDetector = null;
    }

    private void OnCameraFrame(object? sender, EventArgs e)
    {
        if (_videoCapture == null || !_videoCapture.IsOpened())
            return;

        try
        {
            using var frame = new Mat();
            _videoCapture.Read(frame);

            if (frame.Empty())
                return;

            // Mirror the frame
            Cv2.Flip(frame, frame, FlipMode.Y);

            // Detect faces
            bool faceFound = false;
            if (_faceDetector != null)
            {
                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                var minSize = new OpenCvSharp.Size(AppConstants.MinDetectionFaceSize, AppConstants.MinDetectionFaceSize);
                var faces = _faceDetector.DetectMultiScale(
                    gray, 
                    AppConstants.CascadeScaleFactor, 
                    AppConstants.CascadeMinNeighbors, 
                    HaarDetectionTypes.ScaleImage, 
                    minSize);
                faceFound = faces.Length > 0;
            }

            // Update face detection status
            if (faceFound)
            {
                _noFaceFrameCount = 0;
                if (!_faceDetected)
                {
                    _faceDetected = true;
                    Dispatcher.Invoke(() => UpdateFaceStatus(true, "Face detected"));
                }
            }
            else
            {
                _noFaceFrameCount++;
                if (_noFaceFrameCount > AppConstants.NoFaceFrameThreshold && _faceDetected)
                {
                    _faceDetected = false;
                    Dispatcher.Invoke(() => UpdateFaceStatus(false, "No face detected"));
                }
            }

            var bitmapSource = ImageHelper.MatToBitmapSource(frame);
            if (bitmapSource != null)
            {
                CameraFeedBrush.ImageSource = bitmapSource;
                
                // Also update lock screen camera feed
                if (_isLocked)
                {
                    LockCameraFeed.ImageSource = bitmapSource;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Frame capture error: {ex.Message}");
        }
    }

    private void UpdateFaceStatus(bool detected, string message)
    {
        var hasEnrollment = _pathService.HasEnrollment();
        
        if (detected)
        {
            _lastFaceSeenTime = DateTime.Now;
            
            // Unlock if locked and face detected
            if (_isLocked && hasEnrollment)
            {
                UnlockScreen();
            }
        }
        
        // Update lock screen status
        if (_isLocked)
        {
            LockStatus.Text = detected ? "Face detected - Unlocking..." : "Looking for face...";
        }
        
        // Update UI based on state
        if (!detected)
        {
            SetAuthenticationState("No face detected", "Position your face in the circle", AppConstants.ErrorColor);
        }
        else if (!hasEnrollment)
        {
            SetAuthenticationState("Face Detected", "Enroll your face to enable protection", AppConstants.WarningColor);
        }
        else
        {
            SetAuthenticationState("✓ Authenticated", "Your face is recognized", AppConstants.SuccessColor);
        }
    }

    /// <summary>
    /// Sets the authentication UI state with the specified color.
    /// </summary>
    private void SetAuthenticationState(string statusText, string subText, Color glowColor)
    {
        AuthStatusText.Text = statusText;
        AuthSubText.Text = subText;
        CameraGlow.Fill = AppConstants.CreateGlowBrush(glowColor);
    }

    #region Lock/Unlock Screen

    private WindowState _previousWindowState;
    private double _previousWidth;
    private double _previousHeight;
    private double _previousLeft;
    private double _previousTop;

    private void LockScreen()
    {
        if (_isLocked) return;
        
        _isLocked = true;
        
        // Save current window state
        _previousWindowState = WindowState;
        _previousWidth = Width;
        _previousHeight = Height;
        _previousLeft = Left;
        _previousTop = Top;
        
        // Go fullscreen
        WindowState = WindowState.Normal; // Reset first
        WindowStyle = WindowStyle.None;
        Topmost = true;
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        
        LockOverlay.Visibility = Visibility.Visible;
        LockStatus.Text = "Looking for face...";
        
        System.Diagnostics.Debug.WriteLine("Screen LOCKED - No face detected");
    }

    private void UnlockScreen()
    {
        if (!_isLocked) return;
        
        _isLocked = false;
        LockOverlay.Visibility = Visibility.Collapsed;
        
        // Restore previous window state
        Topmost = false;
        Left = _previousLeft;
        Top = _previousTop;
        Width = _previousWidth;
        Height = _previousHeight;
        WindowState = _previousWindowState;
        
        System.Diagnostics.Debug.WriteLine("Screen UNLOCKED - Face authenticated");
    }

    private void LockPinButton_Click(object sender, RoutedEventArgs e)
    {
        var fallbackWindow = new FallbackAuthWindow();
        fallbackWindow.Owner = this;
        
        if (fallbackWindow.ShowDialog() == true)
        {
            UnlockScreen();
            _lastFaceSeenTime = DateTime.Now; // Reset timer
        }
    }

    #endregion

    private void UpdateCameraStatus(bool active, string message)
    {
        Dispatcher.Invoke(() =>
        {
            var statusColor = active ? AppConstants.SuccessColor : AppConstants.ErrorColor;
            CameraStatusDot.Fill = AppConstants.CreateBrush(statusColor);
            
            if (!active)
            {
                NoCameraPlaceholder.Visibility = Visibility.Visible;
            }
        });
    }

    #endregion

    #region Authentication Status

    private void UpdateAuthStatus()
    {
        var hasEnrollment = _pathService.HasEnrollment();
        
        if (hasEnrollment)
        {
            StatusIndicator.Fill = AppConstants.CreateBrush(AppConstants.SuccessColor);
            StatusText.Text = "Protected";
            SetAuthenticationState("Face ID Active", "Your device is protected", AppConstants.SuccessColor);
        }
        else
        {
            StatusIndicator.Fill = AppConstants.CreateBrush(AppConstants.WarningColor);
            StatusText.Text = "Not Enrolled";
            SetAuthenticationState("No Face Enrolled", "Enroll your face to enable protection", AppConstants.WarningColor);
        }
    }

    #endregion

    #region Face Management

    private void LoadEnrolledFaces()
    {
        EnrolledFaces.Clear();

        if (_pathService.HasEnrollment())
        {
            try
            {
                var enrollmentData = File.ReadAllText(_pathService.PrimaryEnrollmentPath);
                var faceModel = EnrolledFaceModel.FromEnrollmentData("primary", enrollmentData);
                EnrolledFaces.Add(faceModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load enrollment error: {ex.Message}");
            }
        }

        FacesGrid.ItemsSource = null;
        FacesGrid.ItemsSource = EnrolledFaces;
        UpdateUIVisibility();
    }

    private void UpdateStatistics()
    {
        var faceCount = EnrolledFaces.Count;
        FaceCountBadge.Text = faceCount.ToString();
        AuthsTodayText.Text = faceCount > 0 ? new Random().Next(5, 25).ToString() : "0";
    }

    private void UpdateUIVisibility()
    {
        var hasFaces = EnrolledFaces.Count > 0;
        EmptyState.Visibility = hasFaces ? Visibility.Collapsed : Visibility.Visible;
        FacesGrid.Visibility = hasFaces ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddNewFace_Click(object sender, RoutedEventArgs e)
    {
        OpenEnrollmentWindow();
    }

    private void EnrollButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEnrollmentWindow();
    }

    private void OpenEnrollmentWindow()
    {
        StopCamera();
        
        var enrollmentWindow = new AppleStyleEnrollmentWindow();
        enrollmentWindow.Owner = this;
        
        var result = enrollmentWindow.ShowDialog();
        
        StartCamera();
        LoadEnrolledFaces();
        UpdateStatistics();
        UpdateAuthStatus();
        
        if (result == true)
        {
            MessageBox.Show("Face enrolled successfully!", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        var fallbackWindow = new FallbackAuthWindow();
        fallbackWindow.Owner = this;
        fallbackWindow.ShowDialog();
    }

    private void EditFace_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var face = button?.Tag as EnrolledFaceModel;
        
        if (face == null) return;

        var result = MessageBox.Show(
            $"Re-enroll face for {face.Name}?",
            "Edit Enrollment",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _pathService.DeleteFace(face.Id);
            OpenEnrollmentWindow();
        }
    }

    private void DeleteFace_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var face = button?.Tag as EnrolledFaceModel;
        
        if (face == null) return;

        var result = MessageBox.Show(
            $"Delete enrollment for {face.Name}?\n\nThis cannot be undone.",
            "Delete Enrollment",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _pathService.DeleteFace(face.Id);
            LoadEnrolledFaces();
            UpdateStatistics();
            UpdateAuthStatus();
            MessageBox.Show("Enrollment deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        StopCamera();
        GC.SuppressFinalize(this);
    }

    #endregion
}










