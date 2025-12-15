using System.ComponentModel.DataAnnotations;

namespace MedSecureVision.Backend.Models;

public class User
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Role { get; set; }

    [MaxLength(256)]
    public string? Auth0UserId { get; set; }

    public DateTime? EnrolledAt { get; set; }

    public DateTime? LastActive { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, suspended

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}






