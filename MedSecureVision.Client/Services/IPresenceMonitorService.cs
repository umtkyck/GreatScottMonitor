using MedSecureVision.Shared.Models;

namespace MedSecureVision.Client.Services;

public interface IPresenceMonitorService
{
    event EventHandler<PresenceCheckResult>? PresenceChanged;
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    void SetAuthenticatedUserEmbedding(float[] embedding);
    TimeSpan? AbsenceDuration { get; }
}






