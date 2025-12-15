using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CXA.Backend.Services;
using CXA.Shared.Models;

namespace CXA.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IFaceVerificationService _faceVerificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IFaceVerificationService faceVerificationService,
        IAuditLogService auditLogService,
        ILogger<AuthenticationController> logger)
    {
        _faceVerificationService = faceVerificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpPost("verify")]
    public async Task<ActionResult<AuthenticationResponse>> Verify([FromBody] AuthenticationRequest request)
    {
        try
        {
            var workstationId = request.WorkstationId ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            var result = await _faceVerificationService.VerifyFaceAsync(
                request.FaceEmbedding,
                threshold: 0.6f);

            // Create audit log entry
            await _auditLogService.LogEventAsync(new CXA.Backend.Models.AuditLog
            {
                EventType = "authentication",
                UserId = result.UserId,
                WorkstationId = workstationId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Result = result.Success ? "success" : "failure",
                ConfidenceScore = result.ConfidenceScore,
                FailureReason = result.Success ? null : "Face not recognized",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    embedding_length = request.FaceEmbedding.Length,
                    threshold = 0.6f
                })
            });

            if (result.Success)
            {
                return Ok(new AuthenticationResponse
                {
                    Success = true,
                    UserId = result.UserId?.ToString(),
                    UserName = result.UserName,
                    ConfidenceScore = result.ConfidenceScore,
                    SessionToken = result.SessionToken
                });
            }

            return Ok(new AuthenticationResponse
            {
                Success = false,
                Error = "Face not recognized"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during face verification");
            return StatusCode(500, new AuthenticationResponse
            {
                Success = false,
                Error = "Internal server error"
            });
        }
    }
}






