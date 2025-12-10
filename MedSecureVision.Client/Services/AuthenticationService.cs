using System.Windows.Media.Imaging;
using MedSecureVision.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace MedSecureVision.Client.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IFaceServiceClient _faceServiceClient;
    private readonly HttpClient _httpClient;
    private readonly BackendApiOptions _options;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IFaceServiceClient faceServiceClient,
        HttpClient httpClient,
        IOptions<BackendApiOptions> options)
    {
        _logger = logger;
        _faceServiceClient = faceServiceClient;
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(BitmapSource frame, DetectedFace face)
    {
        try
        {
            // Extract face embedding
            var embeddingResult = await _faceServiceClient.ExtractEmbeddingAsync(frame, face);
            if (!embeddingResult.Success)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Error = embeddingResult.Error ?? "Failed to extract face embedding"
                };
            }

            // Send to backend for verification
            var request = new AuthenticationRequest
            {
                FaceEmbedding = embeddingResult.Vector,
                WorkstationId = Environment.MachineName
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/verify", request);
            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            return authResponse ?? new AuthenticationResponse
            {
                Success = false,
                Error = "Invalid response from server"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return new AuthenticationResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<EnrollmentResponse> EnrollAsync(string userId, List<BitmapSource> frames)
    {
        try
        {
            var embeddings = new List<float[]>();
            float totalQuality = 0.0f;

            foreach (var frame in frames)
            {
                var embeddingResult = await _faceServiceClient.ExtractEmbeddingAsync(frame);
                if (embeddingResult.Success)
                {
                    embeddings.Add(embeddingResult.Vector);
                    totalQuality += embeddingResult.Confidence;
                }
            }

            if (embeddings.Count == 0)
            {
                return new EnrollmentResponse
                {
                    Success = false,
                    Error = "No valid embeddings extracted"
                };
            }

            var request = new EnrollmentRequest
            {
                UserId = userId,
                FaceEmbeddings = embeddings,
                QualityScore = totalQuality / embeddings.Count
            };

            var response = await _httpClient.PostAsJsonAsync("/api/enrollment/upload-template", request);
            response.EnsureSuccessStatusCode();

            var enrollResponse = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
            return enrollResponse ?? new EnrollmentResponse
            {
                Success = false,
                Error = "Invalid response from server"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enrollment");
            return new EnrollmentResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

public class BackendApiOptions
{
    public string BaseUrl { get; set; } = "https://localhost:5001";
}

