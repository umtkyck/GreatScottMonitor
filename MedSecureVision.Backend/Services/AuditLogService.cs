using MedSecureVision.Backend.Data;
using MedSecureVision.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MedSecureVision.Backend.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogEventAsync(AuditLog auditLog)
    {
        try
        {
            auditLog.Timestamp = DateTime.UtcNow;
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event");
            // Don't throw - audit logging should not break the application
        }
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? eventType = null,
        Guid? userId = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(log => log.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(log => log.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(log => log.EventType == eventType);
        }

        if (userId.HasValue)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(1000) // Limit results
            .ToListAsync();
    }

    public async Task<byte[]> ExportAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var logs = await GetAuditLogsAsync(startDate, endDate);

        var csv = new StringBuilder();
        csv.AppendLine("LogId,EventType,Timestamp,UserId,WorkstationId,IpAddress,Result,ConfidenceScore,FailureReason,SessionId");

        foreach (var log in logs)
        {
            csv.AppendLine($"{log.LogId},{log.EventType},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.UserId},{log.WorkstationId},{log.IpAddress},{log.Result},{log.ConfidenceScore},{log.FailureReason},{log.SessionId}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}






