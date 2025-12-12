namespace MedSecureVision.Shared.Models;

public enum PresenceState
{
    NoFace,
    Authenticated,
    UnauthorizedFace,
    MultipleFaces,
    CameraError
}

public class PresenceCheckResult
{
    public PresenceState State { get; set; }
    public TimeSpan? AbsenceDuration { get; set; }
    public float? SimilarityScore { get; set; }
    public int FaceCount { get; set; }
}


