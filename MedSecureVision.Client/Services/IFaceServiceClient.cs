using System.Windows.Media.Imaging;
using MedSecureVision.Shared.Models;

namespace MedSecureVision.Client.Services;

public interface IFaceServiceClient
{
    Task<FaceDetectionResult> DetectFacesAsync(BitmapSource frame);
    Task<FaceEmbedding> ExtractEmbeddingAsync(BitmapSource frame, DetectedFace? face = null);
    Task<FaceComparisonResult> CompareEmbeddingsAsync(float[] embedding1, float[] embedding2, float threshold = 0.6f);
    Task<bool> IsConnectedAsync();
}

