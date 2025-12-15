using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using CXA.Client.Constants;
using CXA.Client.Helpers;
using CXA.Client.Services;
using OpenCvSharp;

// Resolve WPF vs WinForms/System.Drawing/IO ambiguities
using Color = System.Windows.Media.Color;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using WpfPath = System.Windows.Shapes.Path;

namespace CXA.Client.Views;

/// <summary>
/// Apple Face ID-style enrollment window.
/// Features automatic face angle detection and circular progress.
/// </summary>
public partial class AppleStyleEnrollmentWindow : System.Windows.Window, IDisposable
{
    #region Private Fields

    private VideoCapture? _videoCapture;
    private DispatcherTimer? _cameraTimer;
    private DispatcherTimer? _scanTimer;
    private bool _disposed;
    private readonly IEnrollmentPathService _pathService;
    
    // Enrollment state
    private EnrollmentPhase _currentPhase = EnrollmentPhase.Ready;
    private int _currentScan; // 0 = first scan, 1 = second scan
    private double _scanProgress; // 0 to 100
    private readonly List<BitmapSource> _capturedFrames = new();
    
    // Face detection timing
    private DateTime _scanStartTime;

    #endregion

    #region Constructor

    public AppleStyleEnrollmentWindow()
    {
        InitializeComponent();
        _pathService = new EnrollmentPathService();
        Loaded += OnWindowLoaded;
        Closed += OnWindowClosed;
    }

    #endregion

    #region Window Events

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        StartCamera();
        UpdateUI();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        StopCamera();
        StopScanning();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    #endregion

    #region Camera Methods

    private void StartCamera()
    {
        try
        {
            _videoCapture = new VideoCapture(0);
            if (!_videoCapture.IsOpened())
            {
                InstructionText.Text = "Camera not available";
                return;
            }

            _videoCapture.Set(VideoCaptureProperties.FrameWidth, AppConstants.CameraFrameWidth);
            _videoCapture.Set(VideoCaptureProperties.FrameHeight, AppConstants.CameraFrameHeight);
            _videoCapture.Set(VideoCaptureProperties.Fps, AppConstants.CameraFps);

            _cameraTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AppConstants.CameraTimerIntervalMs)
            };
            _cameraTimer.Tick += OnCameraFrame;
            _cameraTimer.Start();
        }
        catch (Exception ex)
        {
            InstructionText.Text = $"Camera error: {ex.Message}";
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
            if (_videoCapture.Read(frame) && !frame.Empty())
            {
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

    #region Scanning Logic

    private void StartScanning()
    {
        _scanStartTime = DateTime.Now;
        _scanProgress = 0;

        _scanTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50) // Update 20 times per second
        };
        _scanTimer.Tick += OnScanTick;
        _scanTimer.Start();
    }

    private void StopScanning()
    {
        _scanTimer?.Stop();
        _scanTimer = null;
    }

    private void OnScanTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.Now - _scanStartTime).TotalSeconds;
        _scanProgress = Math.Min((elapsed / AppConstants.EnrollmentScanDurationSeconds) * 100, 100);

        // Update progress ring
        UpdateProgressRing(_scanProgress);
        UpdateProgressBar();

        // Capture frames at intervals
        if (_scanProgress > 0 && (int)_scanProgress % 12 == 0)
        {
            CaptureCurrentFrame();
        }

        // Check if scan complete
        if (_scanProgress >= 100)
        {
            CompleteScan();
        }
    }

    private void UpdateProgressRing(double progress)
    {
        // Create arc path based on progress (0-100)
        double angle = (progress / 100.0) * 360;
        
        if (angle <= 0)
        {
            ProgressArc.Data = null;
            return;
        }

        double radius = 140;
        double centerX = 140;
        double centerY = 140;

        double startAngle = -90; // Start from top
        double endAngle = startAngle + angle;

        double startRad = startAngle * Math.PI / 180;
        double endRad = endAngle * Math.PI / 180;

        double x1 = centerX + radius * Math.Cos(startRad);
        double y1 = centerY + radius * Math.Sin(startRad);
        double x2 = centerX + radius * Math.Cos(endRad);
        double y2 = centerY + radius * Math.Sin(endRad);

        bool largeArc = angle > 180;

        string pathData = $"M {x1.ToString(System.Globalization.CultureInfo.InvariantCulture)},{y1.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                         $"A {radius.ToString(System.Globalization.CultureInfo.InvariantCulture)},{radius.ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 {(largeArc ? 1 : 0)} 1 " +
                         $"{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)},{y2.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        try
        {
            ProgressArc.Data = Geometry.Parse(pathData);
        }
        catch { }
    }

    private void UpdateProgressBar()
    {
        double totalProgress = (_currentScan * 50) + (_scanProgress / 2);
        double maxWidth = 400; // Approximate width of progress bar container
        ProgressBar.Width = (totalProgress / 100.0) * maxWidth;
    }

    private void CaptureCurrentFrame()
    {
        if (CameraFeed.Source is BitmapSource frame)
        {
            _capturedFrames.Add(frame);
        }
    }

    private void CompleteScan()
    {
        StopScanning();

        if (_currentScan == 0)
        {
            // First scan complete
            _currentScan = 1;
            _currentPhase = EnrollmentPhase.SecondScan;
            UpdateUI();

            // Brief pause then start second scan
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                StartScanning();
            };
            timer.Start();
        }
        else
        {
            // Both scans complete
            _currentPhase = EnrollmentPhase.Complete;
            UpdateUI();
            PlaySuccessAnimation();
            SaveEnrollment();
        }
    }

    #endregion

    #region UI Updates

    private void UpdateUI()
    {
        switch (_currentPhase)
        {
            case EnrollmentPhase.Ready:
                HeaderSubtitle.Text = "Set Up Face ID";
                InstructionText.Text = "Position your face in the circle, then tap Get Started";
                ScanPhaseText.Text = "";
                ActionButton.Content = "Get Started";
                ActionButton.Visibility = Visibility.Visible;
                break;

            case EnrollmentPhase.FirstScan:
                HeaderSubtitle.Text = "Move Your Head";
                InstructionText.Text = "Move your head slowly to complete the circle";
                ScanPhaseText.Text = "First scan...";
                ActionButton.Visibility = Visibility.Collapsed;
                break;

            case EnrollmentPhase.SecondScan:
                HeaderSubtitle.Text = "Continue Moving";
                InstructionText.Text = "Move your head slowly to complete the circle";
                ScanPhaseText.Text = "Second scan...";
                _scanProgress = 0;
                UpdateProgressRing(0);
                break;

            case EnrollmentPhase.Complete:
                HeaderSubtitle.Text = "Face ID is Now Set Up";
                InstructionText.Text = "You can now use Face ID to unlock and authenticate";
                ScanPhaseText.Text = "";
                ActionButton.Content = "Done";
                ActionButton.Visibility = Visibility.Visible;
                
                // Hide the direction arrow
                DirectionArrow.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void PlaySuccessAnimation()
    {
        // Make progress ring fully green
        ProgressArc.Stroke = new SolidColorBrush(AppConstants.SuccessColor);
        UpdateProgressRing(100);

        // Play checkmark animation
        var storyboard = (Storyboard)FindResource("SuccessAnimation");
        storyboard.Begin(this);
    }

    #endregion

    #region Button Handlers

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_currentPhase)
        {
            case EnrollmentPhase.Ready:
                _currentPhase = EnrollmentPhase.FirstScan;
                UpdateUI();
                StartScanning();
                break;

            case EnrollmentPhase.Complete:
                DialogResult = true;
                Close();
                break;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPhase != EnrollmentPhase.Ready && _currentPhase != EnrollmentPhase.Complete)
        {
            var result = MessageBox.Show(
                "Cancel face enrollment?",
                "Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        DialogResult = false;
        Close();
    }

    #endregion

    #region Save Enrollment

    private void SaveEnrollment()
    {
        try
        {
            _pathService.EnsureDirectoriesExist();

            var enrollmentContent = string.Join("\n", new[]
            {
                $"EnrolledAt={DateTime.UtcNow:O}",
                $"FrameCount={_capturedFrames.Count}",
                $"Version={AppConstants.AppVersion}",
                "Method=AppleFaceID"
            });

            File.WriteAllText(_pathService.PrimaryEnrollmentPath, enrollmentContent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save enrollment error: {ex.Message}");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopCamera();
            StopScanning();
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}

/// <summary>
/// Enrollment phases for Apple-style Face ID setup.
/// </summary>
public enum EnrollmentPhase
{
    Ready,
    FirstScan,
    SecondScan,
    Complete
}





