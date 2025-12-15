using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CXA.Client.Services;

public class SessionLockService : ISessionLockService
{
    private readonly ILogger<SessionLockService> _logger;

    public SessionLockService(ILogger<SessionLockService> logger)
    {
        _logger = logger;
    }

    public Task LockAsync(string reason)
    {
        _logger.LogInformation($"Locking session: {reason}");
        
        try
        {
            // Lock Windows workstation (equivalent to Win+L)
            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "user32.dll,LockWorkStation",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking workstation");
        }

        return Task.CompletedTask;
    }

    public Task UnlockAsync()
    {
        _logger.LogInformation("Unlocking session");
        // Unlock is handled by Windows login screen
        return Task.CompletedTask;
    }
}






