namespace MedSecureVision.Backend.Services;

public interface IFaceVerificationService
{
    Task<FaceVerificationResult> VerifyFaceAsync(float[] embedding, float threshold = 0.6f);
}

public class FaceVerificationResult
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public float ConfidenceScore { get; set; }
    public string? SessionToken { get; set; }
}


