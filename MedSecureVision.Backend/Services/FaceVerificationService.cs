using MedSecureVision.Backend.Data;
using MedSecureVision.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MedSecureVision.Backend.Services;

/// <summary>
/// Service for verifying face embeddings against enrolled templates.
/// Uses cosine similarity to match faces with a configurable threshold.
/// </summary>
public class FaceVerificationService : IFaceVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<FaceVerificationService> _logger;

    public FaceVerificationService(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<FaceVerificationService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Verifies a face embedding against all enrolled templates in the database.
    /// </summary>
    /// <param name="embedding">512-dimensional face embedding to verify</param>
    /// <param name="threshold">Similarity threshold (default 0.6). Values above threshold are considered a match.</param>
    /// <returns>FaceVerificationResult with user information if match found, or failure status</returns>
    public async Task<FaceVerificationResult> VerifyFaceAsync(float[] embedding, float threshold = 0.6f)
    {
        try
        {
            // Get all active users with templates
            var templates = await _context.FaceTemplates
                .Include(t => t.User)
                .Where(t => t.User != null && t.User.Status == "active")
                .ToListAsync();

            float bestSimilarity = 0.0f;
            FaceTemplate? bestMatch = null;

            foreach (var template in templates)
            {
                try
                {
                    // Decrypt template
                    var decryptedBytes = await _encryptionService.DecryptAsync(
                        template.EncryptedTemplate,
                        template.UserId.ToString());

                    var storedEmbedding = JsonSerializer.Deserialize<float[]>(decryptedBytes);
                    if (storedEmbedding == null)
                        continue;

                    // Calculate cosine similarity
                    var similarity = CalculateCosineSimilarity(embedding, storedEmbedding);

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestMatch = template;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error processing template {template.TemplateId}");
                    continue;
                }
            }

            if (bestMatch != null && bestSimilarity > threshold)
            {
                return new FaceVerificationResult
                {
                    Success = true,
                    UserId = bestMatch.UserId,
                    UserName = bestMatch.User?.Name,
                    ConfidenceScore = bestSimilarity,
                    SessionToken = Guid.NewGuid().ToString()
                };
            }

            return new FaceVerificationResult
            {
                Success = false,
                ConfidenceScore = bestSimilarity
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying face");
            return new FaceVerificationResult { Success = false };
        }
    }

    /// <summary>
    /// Calculates cosine similarity between two face embedding vectors.
    /// </summary>
    /// <param name="vec1">First embedding vector</param>
    /// <param name="vec2">Second embedding vector</param>
    /// <returns>Cosine similarity value between 0 and 1</returns>
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

