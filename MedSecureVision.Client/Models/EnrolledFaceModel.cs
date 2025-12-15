using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace MedSecureVision.Client.Models;

/// <summary>
/// Represents the enrollment status of a face.
/// </summary>
public enum FaceStatus
{
    /// <summary>
    /// Face enrollment is active and valid.
    /// </summary>
    Active,

    /// <summary>
    /// Face enrollment has expired and needs re-enrollment.
    /// </summary>
    Expired,

    /// <summary>
    /// Face enrollment is pending verification.
    /// </summary>
    Pending
}

/// <summary>
/// Model representing an enrolled face in the system.
/// Used for display in the dashboard and face management UI.
/// </summary>
public class EnrolledFaceModel
{
    #region Private Constants

    // Status colors (GitHub Dark theme)
    private static readonly Color ActiveColor = Color.FromRgb(0x23, 0x86, 0x36);
    private static readonly Color ExpiredColor = Color.FromRgb(0xB0, 0x40, 0x38);
    private static readonly Color PendingColor = Color.FromRgb(0x9E, 0x6A, 0x03);
    private static readonly Color UnknownColor = Color.FromRgb(0x48, 0x4F, 0x58);

    #endregion

    #region Properties

    /// <summary>
    /// Unique identifier for the enrolled face.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the enrolled person.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Initials derived from the name for avatar display.
    /// </summary>
    public string Initials { get; set; } = string.Empty;

    /// <summary>
    /// Role or designation of the enrolled person.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Formatted enrollment date string.
    /// </summary>
    public string EnrolledDate { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the enrollment.
    /// </summary>
    public FaceStatus Status { get; set; } = FaceStatus.Active;

    /// <summary>
    /// Quality score of the face enrollment (0.0 - 1.0).
    /// </summary>
    public float Quality { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the human-readable status text.
    /// </summary>
    public string StatusText => Status switch
    {
        FaceStatus.Active => "Active",
        FaceStatus.Expired => "Expired",
        FaceStatus.Pending => "Pending",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the status indicator color.
    /// </summary>
    public SolidColorBrush StatusColor => Status switch
    {
        FaceStatus.Active => new SolidColorBrush(ActiveColor),
        FaceStatus.Expired => new SolidColorBrush(ExpiredColor),
        FaceStatus.Pending => new SolidColorBrush(PendingColor),
        _ => new SolidColorBrush(UnknownColor)
    };

    /// <summary>
    /// Gets the quality as a percentage string.
    /// </summary>
    public string QualityPercent => $"{Quality * 100:F0}%";

    #endregion

    #region Static Helper Methods

    /// <summary>
    /// Extracts initials from a full name.
    /// </summary>
    /// <param name="name">The full name to extract initials from.</param>
    /// <returns>Two-character initials string.</returns>
    public static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "??";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
        }
        
        if (parts.Length == 1 && parts[0].Length >= 2)
        {
            return parts[0][..2].ToUpperInvariant();
        }

        return "??";
    }

    /// <summary>
    /// Creates an EnrolledFaceModel from enrollment file data.
    /// </summary>
    /// <param name="id">The enrollment ID.</param>
    /// <param name="enrollmentData">Raw enrollment data content.</param>
    /// <returns>A populated EnrolledFaceModel.</returns>
    public static EnrolledFaceModel FromEnrollmentData(string id, string enrollmentData)
    {
        var enrolledAt = DateTime.Now;

        foreach (var line in enrollmentData.Split('\n'))
        {
            if (line.StartsWith("EnrolledAt=", StringComparison.OrdinalIgnoreCase))
            {
                var dateStr = line["EnrolledAt=".Length..].Trim();
                if (DateTime.TryParse(dateStr, out var date))
                {
                    enrolledAt = date;
                }
            }
        }

        var userName = Environment.UserName;
        
        return new EnrolledFaceModel
        {
            Id = id,
            Name = userName,
            Initials = GetInitials(userName),
            Role = id == "primary" ? "Primary User" : "Additional User",
            EnrolledDate = enrolledAt.ToString("MMM dd, yyyy"),
            Status = FaceStatus.Active,
            Quality = 0.95f
        };
    }

    #endregion
}

