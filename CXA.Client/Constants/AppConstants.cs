using Color = System.Windows.Media.Color;

namespace CXA.Client.Constants;

/// <summary>
/// Application-wide constants for CXA.
/// Centralizes colors, paths, dimensions, and configuration values.
/// </summary>
public static class AppConstants
{
    #region Version Information

    /// <summary>
    /// Current application version (Release 1, Milestone 1).
    /// </summary>
    public const string AppVersion = "R1M1";

    /// <summary>
    /// Application name displayed in UI.
    /// </summary>
    public const string AppName = "CXA";

    /// <summary>
    /// Full application title with version.
    /// </summary>
    public const string AppTitle = "CXA - Biometric Authentication";

    #endregion

    #region Colors - Status Indicators

    /// <summary>
    /// Blue color for "Searching" state (#58A6FF).
    /// </summary>
    public static readonly Color SearchingColor = Color.FromRgb(0x58, 0xA6, 0xFF);
    public const string SearchingColorHex = "#58A6FF";

    /// <summary>
    /// Yellow/amber color for "Positioning" or "Warning" state (#D29922).
    /// </summary>
    public static readonly Color WarningColor = Color.FromRgb(0xD2, 0x99, 0x22);
    public const string WarningColorHex = "#D29922";

    /// <summary>
    /// Purple color for "Verifying" state (#A371F7).
    /// </summary>
    public static readonly Color VerifyingColor = Color.FromRgb(0xA3, 0x71, 0xF7);
    public const string VerifyingColorHex = "#A371F7";

    /// <summary>
    /// Green color for "Success" state (#3FB950).
    /// </summary>
    public static readonly Color SuccessColor = Color.FromRgb(0x3F, 0xB9, 0x50);
    public const string SuccessColorHex = "#3FB950";

    /// <summary>
    /// Red color for "Error" or "Failure" state (#F85149).
    /// </summary>
    public static readonly Color ErrorColor = Color.FromRgb(0xF8, 0x51, 0x49);
    public const string ErrorColorHex = "#F85149";

    #endregion

    #region Colors - UI Theme (GitHub Dark)

    /// <summary>
    /// Primary background color (#0D1117).
    /// </summary>
    public static readonly Color BackgroundPrimary = Color.FromRgb(0x0D, 0x11, 0x17);
    
    /// <summary>
    /// Secondary background color (#161B22).
    /// </summary>
    public static readonly Color BackgroundSecondary = Color.FromRgb(0x16, 0x1B, 0x22);
    
    /// <summary>
    /// Tertiary background color (#21262D).
    /// </summary>
    public static readonly Color BackgroundTertiary = Color.FromRgb(0x21, 0x26, 0x2D);
    
    /// <summary>
    /// Border color (#30363D).
    /// </summary>
    public static readonly Color BorderColor = Color.FromRgb(0x30, 0x36, 0x3D);

    /// <summary>
    /// Primary text color (#F0F6FC).
    /// </summary>
    public static readonly Color TextPrimary = Color.FromRgb(0xF0, 0xF6, 0xFC);
    
    /// <summary>
    /// Secondary/muted text color (#8B949E).
    /// </summary>
    public static readonly Color TextSecondary = Color.FromRgb(0x8B, 0x94, 0x9E);

    #endregion

    #region Camera Configuration

    /// <summary>
    /// Default camera frame width.
    /// </summary>
    public const int CameraFrameWidth = 640;

    /// <summary>
    /// Default camera frame height.
    /// </summary>
    public const int CameraFrameHeight = 480;

    /// <summary>
    /// Default camera frames per second.
    /// </summary>
    public const int CameraFps = 30;

    /// <summary>
    /// Camera timer interval in milliseconds (~30 FPS).
    /// </summary>
    public const int CameraTimerIntervalMs = 33;

    #endregion

    #region Authentication Configuration

    /// <summary>
    /// Maximum failed authentication attempts before lockout.
    /// </summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>
    /// Detection timer interval in milliseconds.
    /// </summary>
    public const int DetectionTimerIntervalMs = 500;

    /// <summary>
    /// Delay after successful authentication in milliseconds.
    /// </summary>
    public const int SuccessDelayMs = 2000;

    /// <summary>
    /// Delay after failed authentication in milliseconds.
    /// </summary>
    public const int FailureDelayMs = 2000;

    #endregion

    #region Enrollment Configuration

    /// <summary>
    /// Duration of one enrollment scan in seconds.
    /// </summary>
    public const double EnrollmentScanDurationSeconds = 4.0;

    /// <summary>
    /// Enrollment data file name.
    /// </summary>
    public const string EnrollmentFileName = "enrollment.dat";

    /// <summary>
    /// Faces subdirectory name.
    /// </summary>
    public const string FacesDirectoryName = "Faces";

    /// <summary>
    /// Application data folder name.
    /// </summary>
    public const string AppDataFolderName = "CXA";

    #endregion

    #region Face Detection Thresholds

    /// <summary>
    /// Minimum confidence for valid face detection.
    /// </summary>
    public const float MinFaceConfidence = 0.5f;

    /// <summary>
    /// Good confidence threshold for face positioning.
    /// </summary>
    public const float GoodFaceConfidence = 0.7f;

    /// <summary>
    /// Minimum face width in pixels.
    /// </summary>
    public const int MinFaceWidth = 100;

    /// <summary>
    /// Maximum face width in pixels.
    /// </summary>
    public const int MaxFaceWidth = 500;

    /// <summary>
    /// Default face comparison threshold.
    /// </summary>
    public const float FaceComparisonThreshold = 0.6f;

    #endregion

    #region IPC Configuration

    /// <summary>
    /// Default named pipe name for face service.
    /// </summary>
    public const string DefaultPipeName = @"\\.\pipe\CXAFaceService";

    /// <summary>
    /// Default IPC timeout in milliseconds.
    /// </summary>
    public const int IpcTimeoutMs = 2000;

    /// <summary>
    /// Ping timeout in milliseconds.
    /// </summary>
    public const int PingTimeoutMs = 500;

    /// <summary>
    /// Buffer size for IPC communication (1MB).
    /// </summary>
    public const int IpcBufferSize = 1024 * 1024;

    #endregion

    #region Mutex

    /// <summary>
    /// Mutex name for single instance enforcement.
    /// </summary>
    public const string SingleInstanceMutexName = "CXA_SingleInstance_Mutex";

    #endregion
}

