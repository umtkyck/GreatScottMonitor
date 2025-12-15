using Xunit;
using FluentAssertions;
using Moq;
using MedSecureVision.Client.Services;
using MedSecureVision.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace MedSecureVision.Tests;

/// <summary>
/// Unit tests for FaceServiceClient
/// </summary>
public class FaceServiceClientTests
{
    private readonly Mock<ILogger<FaceServiceClient>> _loggerMock;
    private readonly FaceServiceOptions _options;

    public FaceServiceClientTests()
    {
        _loggerMock = new Mock<ILogger<FaceServiceClient>>();
        _options = new FaceServiceOptions
        {
            PipeName = @"\\.\pipe\MedSecureFaceService"
        };
    }

    [Fact]
    public async Task DetectFacesAsync_ShouldReturnFailure_WhenServiceUnavailable()
    {
        // Arrange
        var options = Options.Create(_options);
        var client = new FaceServiceClient(_loggerMock.Object, options);

        // Act
        var result = await client.DetectFacesAsync(CreateTestBitmap());

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractEmbeddingAsync_ShouldReturnFailure_WhenServiceUnavailable()
    {
        // Arrange
        var options = Options.Create(_options);
        var client = new FaceServiceClient(_loggerMock.Object, options);

        // Act
        var result = await client.ExtractEmbeddingAsync(CreateTestBitmap());

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompareEmbeddingsAsync_ShouldCalculateCosineSimilarity()
    {
        // Arrange
        var options = Options.Create(_options);
        var client = new FaceServiceClient(_loggerMock.Object, options);
        
        // Create two similar embeddings (normalized)
        var embedding1 = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };
        var embedding2 = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };

        // Act
        var result = await client.CompareEmbeddingsAsync(embedding1, embedding2, 0.6f);

        // Assert
        result.Similarity.Should().BeApproximately(1.0f, 0.01f);
        result.Match.Should().BeTrue();
    }

    [Fact]
    public async Task CompareEmbeddingsAsync_ShouldReturnNoMatch_WhenSimilarityBelowThreshold()
    {
        // Arrange
        var options = Options.Create(_options);
        var client = new FaceServiceClient(_loggerMock.Object, options);
        
        // Create orthogonal embeddings
        var embedding1 = new float[] { 1.0f, 0.0f, 0.0f, 0.0f };
        var embedding2 = new float[] { 0.0f, 1.0f, 0.0f, 0.0f };

        // Act
        var result = await client.CompareEmbeddingsAsync(embedding1, embedding2, 0.6f);

        // Assert
        result.Similarity.Should().BeApproximately(0.0f, 0.01f);
        result.Match.Should().BeFalse();
    }

    [Fact]
    public async Task IsConnectedAsync_ShouldReturnFalse_WhenServiceUnavailable()
    {
        // Arrange
        var options = Options.Create(_options);
        var client = new FaceServiceClient(_loggerMock.Object, options);

        // Act
        var result = await client.IsConnectedAsync();

        // Assert
        result.Should().BeFalse();
    }

    private BitmapSource CreateTestBitmap()
    {
        var width = 640;
        var height = 480;
        var stride = width * 3; // 3 bytes per pixel (BGR)
        var pixels = new byte[width * height * 3];
        
        return BitmapSource.Create(
            width, height,
            96, 96, // DPI
            PixelFormats.Bgr24,
            null,
            pixels,
            stride);
    }
}






