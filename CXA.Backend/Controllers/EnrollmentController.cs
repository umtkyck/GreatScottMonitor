using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CXA.Backend.Services;
using CXA.Backend.Data;
using CXA.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CXA.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<EnrollmentController> _logger;

    public EnrollmentController(
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IAuditLogService auditLogService,
        ILogger<EnrollmentController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartEnrollment([FromBody] StartEnrollmentRequest request)
    {
        try
        {
            var userId = Guid.Parse(request.UserId);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Create enrollment session
            return Ok(new { enrollmentId = Guid.NewGuid().ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting enrollment");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("upload-template")]
    public async Task<ActionResult<EnrollmentResponse>> UploadTemplate([FromBody] EnrollmentRequest request)
    {
        try
        {
            var userId = Guid.Parse(request.UserId);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound(new EnrollmentResponse
                {
                    Success = false,
                    Error = "User not found"
                });
            }

            // Encrypt face templates
            var encryptedTemplates = new List<byte[]>();
            foreach (var embedding in request.FaceEmbeddings)
            {
                var embeddingBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(embedding);
                var encrypted = await _encryptionService.EncryptAsync(embeddingBytes, userId.ToString());
                encryptedTemplates.Add(encrypted);
            }

            // Store templates
            foreach (var encryptedTemplate in encryptedTemplates)
            {
                var template = new CXA.Backend.Models.FaceTemplate
                {
                    UserId = userId,
                    EncryptedTemplate = encryptedTemplate,
                    QualityScore = request.QualityScore,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FaceTemplates.Add(template);
            }

            user.EnrolledAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log enrollment
            await _auditLogService.LogEventAsync(new CXA.Backend.Models.AuditLog
            {
                EventType = "enrollment",
                UserId = userId,
                Result = "success",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    template_count = encryptedTemplates.Count,
                    quality_score = request.QualityScore
                })
            });

            return Ok(new EnrollmentResponse
            {
                Success = true,
                EnrollmentId = Guid.NewGuid().ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading template");
            return StatusCode(500, new EnrollmentResponse
            {
                Success = false,
                Error = "Internal server error"
            });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteEnrollment([FromBody] CompleteEnrollmentRequest request)
    {
        try
        {
            // Finalize enrollment
            return Ok(new { message = "Enrollment completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing enrollment");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class StartEnrollmentRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class CompleteEnrollmentRequest
{
    public string EnrollmentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}






