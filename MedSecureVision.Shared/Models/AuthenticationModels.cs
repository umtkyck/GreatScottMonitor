namespace MedSecureVision.Shared.Models;

public class AuthenticationRequest
{
    public float[] FaceEmbedding { get; set; } = Array.Empty<float>();
    public string WorkstationId { get; set; } = string.Empty;
}

public class AuthenticationResponse
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public float ConfidenceScore { get; set; }
    public string? SessionToken { get; set; }
    public string? Error { get; set; }
}

public class EnrollmentRequest
{
    public string UserId { get; set; } = string.Empty;
    public List<float[]> FaceEmbeddings { get; set; } = new();
    public float QualityScore { get; set; }
}

public class EnrollmentResponse
{
    public bool Success { get; set; }
    public string? EnrollmentId { get; set; }
    public string? Error { get; set; }
}

