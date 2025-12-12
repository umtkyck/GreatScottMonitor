using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Media.Imaging;
using MedSecureVision.Shared.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedSecureVision.Client.Services;

/// <summary>
/// Client for communicating with the Python face service via named pipes.
/// Handles face detection, embedding extraction, and comparison operations.
/// </summary>
public class FaceServiceClient : IFaceServiceClient, IDisposable
{
    private readonly ILogger<FaceServiceClient> _logger;
    private readonly FaceServiceOptions _options;
    private const int BufferSize = 1024 * 1024; // 1MB buffer for images
    
    private bool _disposed;

    public FaceServiceClient(
        ILogger<FaceServiceClient> logger,
        IOptions<FaceServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<FaceDetectionResult> DetectFacesAsync(BitmapSource frame)
    {
        try
        {
            var message = new IpcMessage
            {
                Command = IpcCommands.Detect,
                FrameData = ConvertFrameToBase64(frame)
            };

            var response = await SendMessageAsync(message);
            return ParseDetectionResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting faces");
            return new FaceDetectionResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<FaceEmbedding> ExtractEmbeddingAsync(BitmapSource frame, DetectedFace? face = null)
    {
        try
        {
            var parameters = new Dictionary<string, object>();
            if (face != null)
            {
                parameters["bbox"] = new[] { face.X, face.Y, face.Width, face.Height };
            }

            var message = new IpcMessage
            {
                Command = IpcCommands.ExtractEmbedding,
                FrameData = ConvertFrameToBase64(frame),
                Parameters = parameters
            };

            var response = await SendMessageAsync(message);
            return ParseEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting embedding");
            return new FaceEmbedding { Success = false, Error = ex.Message };
        }
    }

    public async Task<FaceComparisonResult> CompareEmbeddingsAsync(float[] embedding1, float[] embedding2, float threshold = 0.6f)
    {
        var similarity = CalculateCosineSimilarity(embedding1, embedding2);
        return await Task.FromResult(new FaceComparisonResult
        {
            Similarity = similarity,
            Match = similarity > threshold,
            Threshold = threshold
        });
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var message = new IpcMessage { Command = "PING" };
            var response = await SendMessageAsync(message, timeoutMs: 500);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        return await IsConnectedAsync();
    }

    private async Task<IpcResponse> SendMessageAsync(IpcMessage message, int timeoutMs = 2000)
    {
        var pipeName = _options.PipeName ?? @"\\.\pipe\MedSecureFaceService";
        using var client = new NamedPipeClientStream(".", pipeName.Replace(@"\\.\pipe\", ""), PipeDirection.InOut, PipeOptions.Asynchronous);

        await client.ConnectAsync(timeoutMs);

        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await client.WriteAsync(bytes, 0, bytes.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, Encoding.UTF8, false, BufferSize, leaveOpen: true);
        var responseJson = await reader.ReadToEndAsync();

        return JsonConvert.DeserializeObject<IpcResponse>(responseJson) 
            ?? new IpcResponse { Success = false, Error = "Empty response" };
    }

    private string ConvertFrameToBase64(BitmapSource frame)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = 80 }; 
        encoder.Frames.Add(BitmapFrame.Create(frame));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    private FaceDetectionResult ParseDetectionResponse(IpcResponse response)
    {
        if (!response.Success)
            return new FaceDetectionResult { Success = false, Error = response.Error };

        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(response.Data));
        var faces = new List<DetectedFace>();
        
        if (data != null && data.ContainsKey("faces"))
        {
            var facesArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(data["faces"]));
            if (facesArray != null)
            {
                foreach (var f in facesArray)
                {
                    faces.Add(new DetectedFace
                    {
                        X = Convert.ToInt32(f["x"]),
                        Y = Convert.ToInt32(f["y"]),
                        Width = Convert.ToInt32(f["width"]),
                        Height = Convert.ToInt32(f["height"]),
                        Confidence = Convert.ToSingle(f["confidence"])
                    });
                }
            }
        }
        return new FaceDetectionResult { Success = true, Faces = faces };
    }

    private FaceEmbedding ParseEmbeddingResponse(IpcResponse response)
    {
        if (!response.Success) return new FaceEmbedding { Success = false, Error = response.Error };

        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(response.Data));
        if (data != null && data.ContainsKey("embedding"))
        {
            var vec = JsonConvert.DeserializeObject<List<float>>(JsonConvert.SerializeObject(data["embedding"]));
            return new FaceEmbedding { Success = true, Vector = vec?.ToArray() ?? Array.Empty<float>() };
        }
        return new FaceEmbedding { Success = false, Error = "No embedding" };
    }

    private float CalculateCosineSimilarity(float[] vec1, float[] vec2)
    {
        if (vec1.Length != vec2.Length) return 0.0f;
        float dot = 0.0f, n1 = 0.0f, n2 = 0.0f;
        for (int i = 0; i < vec1.Length; i++)
        {
            dot += vec1[i] * vec2[i];
            n1 += vec1[i] * vec1[i];
            n2 += vec2[i] * vec2[i];
        }
        var den = (float)(Math.Sqrt(n1) * Math.Sqrt(n2));
        return den > 0 ? dot / den : 0.0f;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

public class FaceServiceOptions
{
    public string? PipeName { get; set; } = @"\\.\pipe\MedSecureFaceService";
}
