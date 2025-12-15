using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedSecureVision.Backend.Services;

namespace MedSecureVision.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? eventType,
        [FromQuery] Guid? userId)
    {
        try
        {
            var logs = await _auditLogService.GetAuditLogsAsync(startDate, endDate, eventType, userId);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var csvData = await _auditLogService.ExportAuditLogsAsync(startDate, endDate);
            return File(csvData, "text/csv", $"audit-logs-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}






