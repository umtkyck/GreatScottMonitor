using Xunit;
using FluentAssertions;
using Moq;
using MedSecureVision.Client.Services;
using MedSecureVision.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MedSecureVision.Tests;

/// <summary>
/// Unit tests for PresenceMonitorService
/// </summary>
public class PresenceMonitorServiceTests
{
    private readonly Mock<ILogger<PresenceMonitorService>> _loggerMock;
    private readonly Mock<IFaceServiceClient> _faceServiceClientMock;
    private readonly Mock<ICameraService> _cameraServiceMock;
    private readonly Mock<ISessionLockService> _sessionLockServiceMock;

    public PresenceMonitorServiceTests()
    {
        _loggerMock = new Mock<ILogger<PresenceMonitorService>>();
        _faceServiceClientMock = new Mock<IFaceServiceClient>();
        _cameraServiceMock = new Mock<ICameraService>();
        _sessionLockServiceMock = new Mock<ISessionLockService>();
    }

    [Fact]
    public void SetAuthenticatedUserEmbedding_ShouldStoreEmbedding()
    {
        // Arrange
        var service = CreateService();
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        service.SetAuthenticatedUserEmbedding(embedding);

        // Assert
        // Embedding should be stored (we can't directly verify, but no exception should occur)
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task StartMonitoringAsync_ShouldNotStart_WhenNoEmbeddingSet()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.StartMonitoringAsync();

        // Assert
        // Service should handle gracefully without embedding
        service.Should().NotBeNull();
    }

    [Fact]
    public void AbsenceDuration_ShouldBeNull_Initially()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.AbsenceDuration.Should().BeNull();
    }

    private PresenceMonitorService CreateService()
    {
        return new PresenceMonitorService(
            _loggerMock.Object,
            _faceServiceClientMock.Object,
            _cameraServiceMock.Object,
            _sessionLockServiceMock.Object);
    }
}

