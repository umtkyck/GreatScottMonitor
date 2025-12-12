using System.Collections.Generic;

namespace MedSecureVision.Shared.Models;

public class FaceDetectionResult
{
    public List<DetectedFace> Faces { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class DetectedFace
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Confidence { get; set; }
    public List<Landmark> Landmarks { get; set; } = new();
}

public class Landmark
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Type { get; set; } // 0=right eye, 1=left eye, 2=nose, 3=mouth, etc.
}


