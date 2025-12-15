using Xunit;
using FluentAssertions;
using CXA.Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace CXA.Tests;

/// <summary>
/// Unit tests for EncryptionService
/// </summary>
public class EncryptionServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<EncryptionService>> _loggerMock;

    public EncryptionServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Encryption:MasterKey"]).Returns("test-master-key-12345-67890-abcdef");
        _loggerMock = new Mock<ILogger<EncryptionService>>();
    }

    [Fact]
    public async Task EncryptDecrypt_ShouldRoundTripCorrectly()
    {
        // Arrange
        var service = new EncryptionService(_configMock.Object, _loggerMock.Object);
        var originalData = Encoding.UTF8.GetBytes("Test data to encrypt");
        var userId = "test-user-123";

        // Act
        var encrypted = await service.EncryptAsync(originalData, userId);
        var decrypted = await service.DecryptAsync(encrypted, userId);

        // Assert
        decrypted.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task Encrypt_ShouldProduceDifferentOutput_ForSameInput()
    {
        // Arrange
        var service = new EncryptionService(_configMock.Object, _loggerMock.Object);
        var data = Encoding.UTF8.GetBytes("Test data");
        var userId = "test-user-123";

        // Act
        var encrypted1 = await service.EncryptAsync(data, userId);
        var encrypted2 = await service.EncryptAsync(data, userId);

        // Assert - Should be different due to random IV
        encrypted1.Should().NotBeEquivalentTo(encrypted2);
    }

    [Fact]
    public async Task Decrypt_ShouldFail_WithWrongUserId()
    {
        // Arrange
        var service = new EncryptionService(_configMock.Object, _loggerMock.Object);
        var data = Encoding.UTF8.GetBytes("Test data");
        var userId1 = "user-1";
        var userId2 = "user-2";

        // Act
        var encrypted = await service.EncryptAsync(data, userId1);
        
        // Assert - Should throw or return invalid data with wrong user ID
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await service.DecryptAsync(encrypted, userId2);
        });
    }

    [Fact]
    public async Task Encrypt_ShouldHandleEmptyData()
    {
        // Arrange
        var service = new EncryptionService(_configMock.Object, _loggerMock.Object);
        var emptyData = Array.Empty<byte>();
        var userId = "test-user-123";

        // Act
        var encrypted = await service.EncryptAsync(emptyData, userId);
        var decrypted = await service.DecryptAsync(encrypted, userId);

        // Assert
        decrypted.Should().BeEquivalentTo(emptyData);
    }

    [Fact]
    public async Task Encrypt_ShouldHandleLargeData()
    {
        // Arrange
        var service = new EncryptionService(_configMock.Object, _loggerMock.Object);
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeData);
        var userId = "test-user-123";

        // Act
        var encrypted = await service.EncryptAsync(largeData, userId);
        var decrypted = await service.DecryptAsync(encrypted, userId);

        // Assert
        decrypted.Should().BeEquivalentTo(largeData);
    }
}
