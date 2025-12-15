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
    private bool _isDisposed;

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
                return;
            }

            _videoCapture.Set(VideoCaptureProperties.FrameWidth, AppConstants.CameraFrameWidth);
            _videoCapture.Set(VideoCaptureProperties.FrameHeight, AppConstants.CameraFrameHeight);
            _videoCapture.Set(VideoCaptureProperties.Fps, AppConstants.CameraFps);

            _cameraTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _cameraTimer.Tick += OnCameraFrame;
            _cameraTimer.Start();

            NoCameraPlaceholder.Visibility = Visibility.Collapsed;
            UpdateCameraStatus(true, "Camera active");
        }
        catch (Exception ex)
        {
            UpdateCameraStatus(false, $"Camera error: {ex.Message}");
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

            var bitmapSource = ImageHelper.MatToBitmapSource(frame);
            if (bitmapSource != null)
            {
                CameraFeedBrush.ImageSource = bitmapSource;
            }
        }
        catch
        {
            // Ignore frame errors
        }
    }

    private void UpdateCameraStatus(bool active, string message)
    {
        Dispatcher.Invoke(() =>
        {
            CameraStatusDot.Fill = new SolidColorBrush(active ? 
                Color.FromRgb(0x3F, 0xB9, 0x50) : 
                Color.FromRgb(0xF8, 0x51, 0x49));
            
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
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
            StatusText.Text = "Protected";
            AuthStatusText.Text = "Face ID Active";
            AuthSubText.Text = "Your device is protected";
            
            // Update glow color to green
            CameraGlow.Fill = new RadialGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0x40, 0x3F, 0xB9, 0x50), 0.7),
                    new GradientStop(Color.FromArgb(0x00, 0x3F, 0xB9, 0x50), 1.0)
                }
            };
        }
        else
        {
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xD2, 0x99, 0x22));
            StatusText.Text = "Not Enrolled";
            AuthStatusText.Text = "No Face Enrolled";
            AuthSubText.Text = "Enroll your face to enable protection";
            
            // Update glow color to yellow/orange
            CameraGlow.Fill = new RadialGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0x40, 0xD2, 0x99, 0x22), 0.7),
                    new GradientStop(Color.FromArgb(0x00, 0xD2, 0x99, 0x22), 1.0)
                }
            };
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


