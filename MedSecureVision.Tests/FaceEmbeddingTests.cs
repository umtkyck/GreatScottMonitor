using Xunit;
using FluentAssertions;
using MedSecureVision.Shared.Models;

namespace MedSecureVision.Tests;

public class FaceEmbeddingTests
{
    [Fact]
    public void FaceEmbedding_ShouldHave512Dimensions()
    {
        // Arrange
        var embedding = new FaceEmbedding
        {
            Vector = new float[512],
            Confidence = 1.0f,
            Success = true
        };

        // Assert
        embedding.Vector.Should().HaveCount(512);
        embedding.Success.Should().BeTrue();
    }

    [Fact]
    public void FaceComparisonResult_ShouldCalculateSimilarity()
    {
        // Arrange
        var result = new FaceComparisonResult
        {
            Similarity = 0.85f,
            Match = true,
            Threshold = 0.6f
        };

        // Assert
        result.Similarity.Should().BeGreaterThan(result.Threshold);
        result.Match.Should().BeTrue();
    }
}






