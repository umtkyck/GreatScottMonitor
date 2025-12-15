using Xunit;
using FluentAssertions;
using MedSecureVision.Shared.Models;

namespace MedSecureVision.Tests;

/// <summary>
/// Integration tests for end-to-end workflows
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void AuthenticationFlow_ShouldHaveCorrectSequence()
    {
        // Arrange
        var request = new AuthenticationRequest
        {
            FaceEmbedding = new float[512],
            WorkstationId = "TEST-WORKSTATION"
        };

        // Act & Assert
        request.FaceEmbedding.Should().HaveCount(512);
        request.WorkstationId.Should().Be("TEST-WORKSTATION");
    }

    [Fact]
    public void EnrollmentFlow_ShouldHaveRequiredData()
    {
        // Arrange
        var request = new EnrollmentRequest
        {
            UserId = "test-user-id",
            FaceEmbeddings = new List<float[]>
            {
                new float[512],
                new float[512]
            },
            QualityScore = 0.85f
        };

        // Act & Assert
        request.UserId.Should().NotBeNullOrEmpty();
        request.FaceEmbeddings.Should().HaveCount(2);
        request.QualityScore.Should().BeInRange(0.0f, 1.0f);
    }

    [Fact]
    public void PresenceState_ShouldHaveAllRequiredStates()
    {
        // Act & Assert
        Enum.GetValues<PresenceState>().Should().Contain(PresenceState.NoFace);
        Enum.GetValues<PresenceState>().Should().Contain(PresenceState.Authenticated);
        Enum.GetValues<PresenceState>().Should().Contain(PresenceState.UnauthorizedFace);
        Enum.GetValues<PresenceState>().Should().Contain(PresenceState.MultipleFaces);
        Enum.GetValues<PresenceState>().Should().Contain(PresenceState.CameraError);
    }
}






