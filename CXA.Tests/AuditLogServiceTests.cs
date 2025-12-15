using Xunit;
using FluentAssertions;
using CXA.Backend.Services;
using CXA.Backend.Data;
using CXA.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace CXA.Tests;

/// <summary>
/// Unit tests for AuditLogService
/// </summary>
public class AuditLogServiceTests
{
    [Fact]
    public async Task LogEventAsync_ShouldSaveAuditLog()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AuditLogService>>();
        var service = new AuditLogService(context, logger.Object);

        var auditLog = new AuditLog
        {
            EventType = "authentication",
            UserId = Guid.NewGuid(),
            Result = "success",
            Timestamp = DateTime.UtcNow
        };

        // Act
        await service.LogEventAsync(auditLog);
        await context.SaveChangesAsync();

        // Assert
        var savedLog = await context.AuditLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.EventType.Should().Be("authentication");
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldFilterByEventType()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AuditLogService>>();
        var service = new AuditLogService(context, logger.Object);

        // Add test data
        context.AuditLogs.Add(new AuditLog { EventType = "authentication", Timestamp = DateTime.UtcNow });
        context.AuditLogs.Add(new AuditLog { EventType = "enrollment", Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var logs = await service.GetAuditLogsAsync(eventType: "authentication");

        // Assert
        logs.Should().HaveCount(1);
        logs[0].EventType.Should().Be("authentication");
    }

    [Fact]
    public async Task ExportAuditLogsAsync_ShouldGenerateCSV()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<AuditLogService>>();
        var service = new AuditLogService(context, logger.Object);

        context.AuditLogs.Add(new AuditLog 
        { 
            EventType = "authentication", 
            Timestamp = DateTime.UtcNow,
            Result = "success"
        });
        await context.SaveChangesAsync();

        // Act
        var csvData = await service.ExportAuditLogsAsync();

        // Assert
        csvData.Should().NotBeEmpty();
        var csvString = System.Text.Encoding.UTF8.GetString(csvData);
        csvString.Should().Contain("LogId");
        csvString.Should().Contain("EventType");
        csvString.Should().Contain("authentication");
    }
}






