using MedSecureVision.Backend.Models;

namespace MedSecureVision.Backend.Services;

public interface IAuditLogService
{
    Task LogEventAsync(AuditLog auditLog);
    Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string? eventType = null, Guid? userId = null);
    Task<byte[]> ExportAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null);
}


