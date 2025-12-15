using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CXA.Backend.Models;

public class AuditLog
{
    [Key]
    public Guid LogId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // authentication, enrollment, lock, unlock, admin_action, security_alert

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; }

    [MaxLength(256)]
    public string? WorkstationId { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(20)]
    public string? Result { get; set; } // success, failure, timeout

    public float? ConfidenceScore { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public Guid? SessionId { get; set; }

    public string? Metadata { get; set; } // JSON metadata
}

