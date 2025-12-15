using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CXA.Client.Constants;
using CXA.Client.Services;
using CXA.Client.Views;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CXA.Client;

/// <summary>
/// Application entry point for CXA.
/// Single-window application with unified dashboard.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private Mutex? _mutex;
    private SystemTrayService? _trayService;
    private DashboardWindow? _dashboard;
    private readonly IEnrollmentPathService _pathService = new EnrollmentPathService();

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Single instance check
        _mutex = new Mutex(true, AppConstants.SingleInstanceMutexName, out bool createdNew);
        
        if (!createdNew)
        {
            MessageBox.Show(
                $"{AppConstants.AppName} is already running.\nCheck your system tray.", 
                "Already Running", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Setup dependency injection
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                services.AddSingleton<IEnrollmentPathService, EnrollmentPathService>();
                services.AddSingleton<IFaceServiceClient, FaceServiceClient>();
                services.AddSingleton<ICameraService, CameraService>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IPresenceMonitorService, PresenceMonitorService>();
                services.AddSingleton<ISessionLockService, SessionLockService>();
                services.AddSingleton<ICloudAuthService, CloudAuthService>();
                services.AddSingleton<IFallbackAuthService, FallbackAuthService>();
                
                services.Configure<FaceServiceOptions>(
                    context.Configuration.GetSection("FaceService"));
                services.Configure<BackendApiOptions>(
                    context.Configuration.GetSection("BackendApi"));
            })
            .Build();

        await _host.StartAsync();

        // System tray for background operation
        _trayService = new SystemTrayService(
            showMainWindow: ShowDashboard,
            showDashboard: ShowDashboard,
            onServiceStateChanged: OnServiceStateChanged
        );

        _trayService.SetEnrollmentStatus(_pathService.HasEnrollment());

        // Show the unified dashboard
        _dashboard = new DashboardWindow();
        _dashboard.Show();
        
        _trayService.ShowNotification(
            $"{AppConstants.AppName} Started",
            "Biometric authentication is active.");
    }

    private void ShowDashboard()
    {
        Dispatcher.Invoke(() =>
        {
            if (_dashboard == null)
            {
                _dashboard = new DashboardWindow();
            }

            _dashboard.Show();
            _dashboard.WindowState = WindowState.Normal;
            _dashboard.Activate();
        });
    }

    private void OnServiceStateChanged(bool isRunning)
    {
        // Service state change handling
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }
}
