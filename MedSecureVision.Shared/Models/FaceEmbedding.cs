namespace MedSecureVision.Shared.Models;

public class FaceEmbedding
{
    public float[] Vector { get; set; } = Array.Empty<float>();
    public float Confidence { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class FaceComparisonResult
{
    public float Similarity { get; set; }
    public bool Match { get; set; }
    public float Threshold { get; set; } = 0.6f;
}


