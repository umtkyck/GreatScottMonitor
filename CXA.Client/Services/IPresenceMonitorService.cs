using CXA.Shared.Models;

namespace CXA.Client.Services;

public interface IPresenceMonitorService
{
    event EventHandler<PresenceCheckResult>? PresenceChanged;
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    void SetAuthenticatedUserEmbedding(float[] embedding);
    TimeSpan? AbsenceDuration { get; }
}






