using System.Windows.Media.Imaging;
using MedSecureVision.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MedSecureVision.Client.Services;

/// <summary>
/// Service for monitoring user presence in front of the camera.
/// Continuously checks for the authenticated user's face and locks the session
/// if the user leaves or an unauthorized face is detected.
/// </summary>
public class PresenceMonitorService : IPresenceMonitorService
{
    private readonly ILogger<PresenceMonitorService> _logger;
    private readonly IFaceServiceClient _faceServiceClient;
    private readonly ICameraService _cameraService;
    private readonly ISessionLockService _sessionLockService;
    private readonly DispatcherTimer _monitorTimer;

    private float[]? _authenticatedUserEmbedding;
    private DateTime? _absenceStartTime;
    private TimeSpan _absenceThreshold = TimeSpan.FromSeconds(5);
    private bool _isMonitoring = false;

    public event EventHandler<PresenceCheckResult>? PresenceChanged;

    public TimeSpan? AbsenceDuration => _absenceStartTime.HasValue 
        ? DateTime.UtcNow - _absenceStartTime.Value 
        : null;

    public PresenceMonitorService(
        ILogger<PresenceMonitorService> logger,
        IFaceServiceClient faceServiceClient,
        ICameraService cameraService,
        ISessionLockService sessionLockService)
    {
        _logger = logger;
        _faceServiceClient = faceServiceClient;
        _cameraService = cameraService;
        _sessionLockService = sessionLockService;

        _monitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200) // Check every 200ms
        };
        _monitorTimer.Tick += OnMonitorTimerTick;
    }

    /// <summary>
    /// Sets the face embedding of the authenticated user for presence monitoring.
    /// This should be called after successful authentication.
    /// </summary>
    /// <param name="embedding">512-dimensional face embedding vector of the authenticated user</param>
    public void SetAuthenticatedUserEmbedding(float[] embedding)
    {
        _authenticatedUserEmbedding = embedding;
        _absenceStartTime = null;
        _logger.LogInformation("Authenticated user embedding set");
    }

    /// <summary>
    /// Starts continuous presence monitoring.
    /// Checks for user presence every 200ms and locks session if user is absent.
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public Task StartMonitoringAsync()
    {
        if (_authenticatedUserEmbedding == null)
        {
            _logger.LogWarning("Cannot start monitoring without authenticated user embedding");
            return Task.CompletedTask;
        }

        _isMonitoring = true;
        _monitorTimer.Start();
        _logger.LogInformation("Presence monitoring started");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops presence monitoring.
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public Task StopMonitoringAsync()
    {
        _isMonitoring = false;
        _monitorTimer.Stop();
        _logger.LogInformation("Presence monitoring stopped");
        return Task.CompletedTask;
    }

    private async void OnMonitorTimerTick(object? sender, EventArgs e)
    {
        if (!_isMonitoring || _authenticatedUserEmbedding == null)
            return;

        try
        {
            var frame = await _cameraService.GetCurrentFrameAsync();
            if (frame == null)
            {
                HandleNoFace();
                return;
            }

            var detectionResult = await _faceServiceClient.DetectFacesAsync(frame);
            
            if (!detectionResult.Success || detectionResult.Faces.Count == 0)
            {
                HandleNoFace();
                return;
            }

            if (detectionResult.Faces.Count > 1)
            {
                HandleMultipleFaces();
                return;
            }

            // Single face detected - verify it's the authenticated user
            var face = detectionResult.Faces[0];
            var embeddingResult = await _faceServiceClient.ExtractEmbeddingAsync(frame, face);
            
            if (!embeddingResult.Success)
            {
                HandleNoFace();
                return;
            }

            // Compare with authenticated user embedding
            var comparison = await _faceServiceClient.CompareEmbeddingsAsync(
                embeddingResult.Vector, 
                _authenticatedUserEmbedding, 
                threshold: 0.5f); // Lower threshold for presence (not auth)

            if (comparison.Match)
            {
                // User is present
                _absenceStartTime = null;
                PresenceChanged?.Invoke(this, new PresenceCheckResult
                {
                    State = PresenceState.Authenticated,
                    SimilarityScore = comparison.Similarity,
                    FaceCount = 1
                });
            }
            else
            {
                // Different face detected
                HandleUnauthorizedFace(comparison.Similarity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in presence monitoring");
            HandleCameraError();
        }
    }

    private void HandleNoFace()
    {
        if (!_absenceStartTime.HasValue)
        {
            _absenceStartTime = DateTime.UtcNow;
        }
        else if (AbsenceDuration > _absenceThreshold)
        {
            _logger.LogWarning($"No face detected for {AbsenceDuration.Value.TotalSeconds} seconds - locking session");
            _sessionLockService.LockAsync("No face detected");
            
            PresenceChanged?.Invoke(this, new PresenceCheckResult
            {
                State = PresenceState.NoFace,
                AbsenceDuration = AbsenceDuration,
                FaceCount = 0
            });
        }
        else
        {
            PresenceChanged?.Invoke(this, new PresenceCheckResult
            {
                State = PresenceState.NoFace,
                AbsenceDuration = AbsenceDuration,
                FaceCount = 0
            });
        }
    }

    private void HandleUnauthorizedFace(float similarity)
    {
        _logger.LogWarning($"Unauthorized face detected (similarity: {similarity:F2}) - locking session");
        _sessionLockService.LockAsync("Unauthorized face detected");
        
        PresenceChanged?.Invoke(this, new PresenceCheckResult
        {
            State = PresenceState.UnauthorizedFace,
            SimilarityScore = similarity,
            FaceCount = 1
        });
    }

    private void HandleMultipleFaces()
    {
        _logger.LogWarning("Multiple faces detected - locking session");
        _sessionLockService.LockAsync("Multiple faces detected");
        
        PresenceChanged?.Invoke(this, new PresenceCheckResult
        {
            State = PresenceState.MultipleFaces,
            FaceCount = 2 // At least 2
        });
    }

    private void HandleCameraError()
    {
        PresenceChanged?.Invoke(this, new PresenceCheckResult
        {
            State = PresenceState.CameraError,
            FaceCount = 0
        });
    }
}

