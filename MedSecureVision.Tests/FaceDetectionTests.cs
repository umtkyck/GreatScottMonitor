using Xunit;
using FluentAssertions;
using MedSecureVision.Shared.Models;

namespace MedSecureVision.Tests;

public class FaceDetectionTests
{
    [Fact]
    public void FaceDetectionResult_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var result = new FaceDetectionResult
        {
            Success = true,
            Faces = new List<DetectedFace>
            {
                new DetectedFace
                {
                    X = 100,
                    Y = 150,
                    Width = 200,
                    Height = 250,
                    Confidence = 0.95f
                }
            }
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Faces.Should().HaveCount(1);
        result.Faces[0].Confidence.Should().Be(0.95f);
    }

    [Fact]
    public void DetectedFace_ShouldHaveValidCoordinates()
    {
        // Arrange & Act
        var face = new DetectedFace
        {
            X = 100,
            Y = 150,
            Width = 200,
            Height = 250
        };

        // Assert
        face.X.Should().Be(100);
        face.Y.Should().Be(150);
        face.Width.Should().Be(200);
        face.Height.Should().Be(250);
    }
}

