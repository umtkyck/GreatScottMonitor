using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedSecureVision.Backend.Models;

public class FaceTemplate
{
    [Key]
    public Guid TemplateId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public byte[] EncryptedTemplate { get; set; } = Array.Empty<byte>();

    [MaxLength(256)]
    public string? EncryptionKeyId { get; set; }

    public float QualityScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

