using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MedSecureVision.Client.Constants;
using MedSecureVision.Client.Services;
using MedSecureVision.Client.ViewModels;
using MedSecureVision.Client.Views;

// Resolve WPF vs WinForms ambiguities
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace MedSecureVision.Client;

/// <summary>
/// Application entry point for MedSecure Vision.
/// Manages application lifecycle, dependency injection, system tray integration,
/// and background service operation.
/// </summary>
/// <remarks>
/// Version: 1.0.0
/// Features:
/// - Single instance enforcement via Mutex
/// - System tray integration for background operation
/// - Dependency injection for all services
/// - HIPAA-compliant biometric authentication
/// </remarks>
public partial class App : Application
{
    private IHost? _host;
    private Mutex? _mutex;
    private SystemTrayService? _trayService;
    private MainWindow? _mainWindow;
    private readonly IEnrollmentPathService _pathService = new EnrollmentPathService();

    /// <summary>
    /// Application startup handler.
    /// Initializes services, system tray, and shows main window.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // ═══════════════════════════════════════════════════════════════
        // SINGLE INSTANCE CHECK
        // Prevents multiple instances of the application from running
        // ═══════════════════════════════════════════════════════════════
        _mutex = new Mutex(true, AppConstants.SingleInstanceMutexName, out bool createdNew);
        
        if (!createdNew)
        {
            MessageBox.Show(
                "MedSecure Vision is already running.\nCheck your system tray.", 
                "Already Running", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // ═══════════════════════════════════════════════════════════════
        // DEPENDENCY INJECTION SETUP
        // Configure all services, view models, and options
        // ═══════════════════════════════════════════════════════════════
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // HTTP Client for backend API calls
                services.AddHttpClient();
                
                // Core Services
                services.AddSingleton<IEnrollmentPathService, EnrollmentPathService>();
                services.AddSingleton<IFaceServiceClient, FaceServiceClient>();
                services.AddSingleton<ICameraService, CameraService>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IPresenceMonitorService, PresenceMonitorService>();
                services.AddSingleton<ISessionLockService, SessionLockService>();
                services.AddSingleton<ICloudAuthService, CloudAuthService>();
                services.AddSingleton<IFallbackAuthService, FallbackAuthService>();

                // ViewModels
                services.AddTransient<AuthenticationViewModel>();
                services.AddTransient<EnrollmentViewModel>();

                // Main Window
                services.AddSingleton<MainWindow>();

                // Configuration
                services.Configure<FaceServiceOptions>(
                    context.Configuration.GetSection("FaceService"));
                services.Configure<BackendApiOptions>(
                    context.Configuration.GetSection("BackendApi"));
            })
            .Build();

        await _host.StartAsync();

        // ═══════════════════════════════════════════════════════════════
        // SYSTEM TRAY INITIALIZATION
        // Creates tray icon with menu for background operation
        // ═══════════════════════════════════════════════════════════════
        _trayService = new SystemTrayService(
            showMainWindow: ShowMainWindow,
            showDashboard: ShowDashboard,
            onServiceStateChanged: OnServiceStateChanged
        );

        // Check initial enrollment status
        _trayService.SetEnrollmentStatus(_pathService.HasEnrollment());

        // ═══════════════════════════════════════════════════════════════
        // MAIN WINDOW
        // Show the authentication window
        // ═══════════════════════════════════════════════════════════════
        _mainWindow = _host.Services.GetRequiredService<MainWindow>();
        _mainWindow.Show();
        
        // Show startup notification
        _trayService.ShowNotification(
            "MedSecure Vision Started",
            "Face authentication service is running.\nRight-click tray icon for options.");
    }

    /// <summary>
    /// Shows the main authentication window.
    /// Brings existing window to front if already open.
    /// </summary>
    private void ShowMainWindow()
    {
        Dispatcher.Invoke(() =>
        {
            if (_mainWindow == null)
            {
                _mainWindow = _host?.Services.GetRequiredService<MainWindow>();
            }

            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }
        });
    }

    /// <summary>
    /// Shows the dashboard window for face management.
    /// </summary>
    private void ShowDashboard()
    {
        Dispatcher.Invoke(() =>
        {
            var dashboard = new DashboardWindow();
            dashboard.Show();
        });
    }

    /// <summary>
    /// Handles service state changes from the system tray.
    /// </summary>
    private void OnServiceStateChanged(bool isRunning)
    {
        if (isRunning)
        {
            // Restart camera capture
            Dispatcher.Invoke(() =>
            {
                if (_mainWindow?.DataContext is AuthenticationViewModel vm)
                {
                    // Restart authentication
                        vm.RestartAuthentication();
                }
            });
        }
        else
        {
            // Stop camera capture
            Dispatcher.Invoke(() =>
            {
                var cameraService = _host?.Services.GetService<ICameraService>();
                _ = cameraService?.StopCaptureAsync();
            });
        }
    }

    /// <summary>
    /// Application exit handler.
    /// Cleans up resources, stops services, and releases mutex.
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        // Dispose system tray
        _trayService?.Dispose();

        // Release mutex
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        
        // Stop host
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }
}
