using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MedSecureVision.Client.Services;
using MedSecureVision.Client.ViewModels;

namespace MedSecureVision.Client;

public partial class App : Application
{
    private IHost? _host;
    private Mutex? _mutex;
    private const string MutexName = "MedSecureVision_SingleInstance_Mutex";

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Prevent multiple instances
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);
        
        if (!createdNew)
        {
            // Another instance is already running
            MessageBox.Show("MedSecure Vision is already running.", "Already Running", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // HttpClient for backend API calls
                services.AddHttpClient();
                
                // Services
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

                // Configuration
                services.Configure<FaceServiceOptions>(
                    context.Configuration.GetSection("FaceService"));
                services.Configure<BackendApiOptions>(
                    context.Configuration.GetSection("BackendApi"));
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = new MainWindow();
        mainWindow.DataContext = _host.Services.GetRequiredService<AuthenticationViewModel>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Release mutex
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
