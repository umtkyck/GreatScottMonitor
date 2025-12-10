using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MedSecureVision.Client.Services;
using MedSecureVision.Client.ViewModels;

namespace MedSecureVision.Client;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
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
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}

