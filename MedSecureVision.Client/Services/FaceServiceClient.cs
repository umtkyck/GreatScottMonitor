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
public class FaceServiceClient : IFaceServiceClient
{
    private readonly ILogger<FaceServiceClient> _logger;
    private readonly FaceServiceOptions _options;
    private const int BufferSize = 65536; // Maximum message size for named pipe communication
    private const int TimeoutMs = 5000; // Connection timeout in milliseconds

    /// <summary>
    /// Initializes a new instance of the FaceServiceClient.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations</param>
    /// <param name="options">Configuration options including pipe name</param>
    public FaceServiceClient(
        ILogger<FaceServiceClient> logger,
        IOptions<FaceServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Detects faces in the provided frame using the Python face service.
    /// </summary>
    /// <param name="frame">Bitmap image frame to analyze</param>
    /// <returns>FaceDetectionResult containing detected faces with bounding boxes and landmarks</returns>
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
            return new FaceDetectionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Extracts a 512-dimensional face embedding vector from the provided frame.
    /// </summary>
    /// <param name="frame">Bitmap image frame containing a face</param>
    /// <param name="face">Optional detected face with bounding box to crop before extraction</param>
    /// <returns>FaceEmbedding containing the 512-dimensional vector representation</returns>
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
            return new FaceEmbedding
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Compares two face embeddings using cosine similarity.
    /// </summary>
    /// <param name="embedding1">First face embedding vector (512 dimensions)</param>
    /// <param name="embedding2">Second face embedding vector (512 dimensions)</param>
    /// <param name="threshold">Similarity threshold (default 0.6). Values above threshold are considered a match.</param>
    /// <returns>FaceComparisonResult with similarity score and match status</returns>
    public async Task<FaceComparisonResult> CompareEmbeddingsAsync(float[] embedding1, float[] embedding2, float threshold = 0.6f)
    {
        // This would typically be done on the Python side, but for now we'll do it client-side
        // In production, send both embeddings to Python service for comparison
        var similarity = CalculateCosineSimilarity(embedding1, embedding2);
        return new FaceComparisonResult
        {
            Similarity = similarity,
            Match = similarity > threshold,
            Threshold = threshold
        };
    }

    /// <summary>
    /// Checks if the face service is available and responding.
    /// </summary>
    /// <returns>True if service is connected and responding, false otherwise</returns>
    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var message = new IpcMessage { Command = "PING" };
            await SendMessageAsync(message, timeoutMs: 1000);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sends a message to the Python face service via named pipe.
    /// </summary>
    /// <param name="message">IPC message to send</param>
    /// <param name="timeoutMs">Connection timeout in milliseconds</param>
    /// <returns>IpcResponse from the service</returns>
    /// <exception cref="TimeoutException">Thrown when connection times out</exception>
    private async Task<IpcResponse> SendMessageAsync(IpcMessage message, int timeoutMs = TimeoutMs)
    {
        var pipeName = _options.PipeName ?? @"\\.\pipe\MedSecureFaceService";
        
        using var pipeClient = new NamedPipeClientStream(
            ".", 
            pipeName.Replace(@"\\.\pipe\", ""),
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await pipeClient.ConnectAsync(timeoutMs);
            
            var json = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await pipeClient.WriteAsync(bytes, 0, bytes.Length);
            await pipeClient.FlushAsync();

            // Read response
            var buffer = new byte[BufferSize];
            var bytesRead = await pipeClient.ReadAsync(buffer, 0, BufferSize);
            var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            return JsonConvert.DeserializeObject<IpcResponse>(responseJson) 
                ?? new IpcResponse { Success = false, Error = "Invalid response" };
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Pipe connection timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with face service");
            throw;
        }
    }

    /// <summary>
    /// Converts a BitmapSource to base64-encoded JPEG for transmission over named pipe.
    /// </summary>
    /// <param name="frame">Bitmap image to encode</param>
    /// <returns>Base64-encoded JPEG string</returns>
    private string ConvertFrameToBase64(BitmapSource frame)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
        encoder.Frames.Add(BitmapFrame.Create(frame));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    private FaceDetectionResult ParseDetectionResponse(IpcResponse response)
    {
        if (!response.Success)
        {
            return new FaceDetectionResult
            {
                Success = false,
                Error = response.Error
            };
        }

        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(response.Data));
        
        var faces = new List<DetectedFace>();
        if (data != null && data.ContainsKey("faces"))
        {
            var facesArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                JsonConvert.SerializeObject(data["faces"]));
            
            if (facesArray != null)
            {
                foreach (var faceDict in facesArray)
                {
                    faces.Add(new DetectedFace
                    {
                        X = Convert.ToInt32(faceDict["x"]),
                        Y = Convert.ToInt32(faceDict["y"]),
                        Width = Convert.ToInt32(faceDict["width"]),
                        Height = Convert.ToInt32(faceDict["height"]),
                        Confidence = Convert.ToSingle(faceDict["confidence"]),
                        Landmarks = ParseLandmarks(faceDict.ContainsKey("landmarks") ? faceDict["landmarks"] : null)
                    });
                }
            }
        }

        return new FaceDetectionResult
        {
            Success = true,
            Faces = faces
        };
    }

    private FaceEmbedding ParseEmbeddingResponse(IpcResponse response)
    {
        if (!response.Success)
        {
            return new FaceEmbedding
            {
                Success = false,
                Error = response.Error
            };
        }

        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(response.Data));
        
        if (data != null && data.ContainsKey("embedding"))
        {
            var embeddingArray = JsonConvert.DeserializeObject<List<float>>(
                JsonConvert.SerializeObject(data["embedding"]));
            
            return new FaceEmbedding
            {
                Success = true,
                Vector = embeddingArray?.ToArray() ?? Array.Empty<float>(),
                Confidence = data.ContainsKey("confidence") ? Convert.ToSingle(data["confidence"]) : 1.0f
            };
        }

        return new FaceEmbedding
        {
            Success = false,
            Error = "No embedding in response"
        };
    }

    private List<Landmark> ParseLandmarks(object? landmarksObj)
    {
        var landmarks = new List<Landmark>();
        if (landmarksObj == null) return landmarks;

        var landmarksArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
            JsonConvert.SerializeObject(landmarksObj));
        
        if (landmarksArray != null)
        {
            foreach (var lmDict in landmarksArray)
            {
                landmarks.Add(new Landmark
                {
                    X = Convert.ToInt32(lmDict["x"]),
                    Y = Convert.ToInt32(lmDict["y"]),
                    Type = Convert.ToInt32(lmDict["type"])
                });
            }
        }

        return landmarks;
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// Cosine similarity ranges from -1 to 1, where 1 indicates identical vectors.
    /// </summary>
    /// <param name="vec1">First vector</param>
    /// <param name="vec2">Second vector</param>
    /// <returns>Cosine similarity value between -1 and 1</returns>
    private float CalculateCosineSimilarity(float[] vec1, float[] vec2)
    {
        if (vec1.Length != vec2.Length)
            return 0.0f;

        float dotProduct = 0.0f;
        float norm1 = 0.0f;
        float norm2 = 0.0f;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            norm1 += vec1[i] * vec1[i];
            norm2 += vec2[i] * vec2[i];
        }

        var denominator = (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
        return denominator > 0 ? dotProduct / denominator : 0.0f;
    }
}

public class FaceServiceOptions
{
    public string? PipeName { get; set; } = @"\\.\pipe\MedSecureFaceService";
}

