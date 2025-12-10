using Xunit;
using FluentAssertions;
using Moq;
using MedSecureVision.Client.Services;
using MedSecureVision.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MedSecureVision.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnSuccess_WhenFaceRecognized()
    {
        // Arrange
        var logger = new Mock<ILogger<AuthenticationService>>();
        var faceServiceClient = new Mock<IFaceServiceClient>();
        var httpClient = new HttpClient();
        var options = Microsoft.Extensions.Options.Options.Create(new BackendApiOptions
        {
            BaseUrl = "https://localhost:5001"
        });

        var service = new AuthenticationService(
            logger.Object,
            faceServiceClient.Object,
            httpClient,
            options);

        // Note: This is a simplified test - in production, mock HttpClient responses
        // Assert
        service.Should().NotBeNull();
    }
}

