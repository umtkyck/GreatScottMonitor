using System.IO;
using MedSecureVision.Client.Constants;

namespace MedSecureVision.Client.Services;

/// <summary>
/// Service for managing enrollment-related file paths.
/// Centralizes all path logic to ensure consistency across the application.
/// </summary>
public interface IEnrollmentPathService
{
    /// <summary>
    /// Gets the base directory for MedSecure Vision data.
    /// </summary>
    string BaseDirectory { get; }

    /// <summary>
    /// Gets the path to the primary enrollment file.
    /// </summary>
    string PrimaryEnrollmentPath { get; }

    /// <summary>
    /// Gets the directory for storing individual face data.
    /// </summary>
    string FacesDirectory { get; }

    /// <summary>
    /// Checks if any enrollment exists.
    /// </summary>
    bool HasEnrollment();

    /// <summary>
    /// Gets the path for a specific face enrollment file.
    /// </summary>
    /// <param name="faceId">The unique face identifier.</param>
    /// <returns>Full path to the face enrollment file.</returns>
    string GetFacePath(string faceId);

    /// <summary>
    /// Ensures all required directories exist.
    /// </summary>
    void EnsureDirectoriesExist();

    /// <summary>
    /// Deletes the primary enrollment.
    /// </summary>
    /// <returns>True if deletion was successful.</returns>
    bool DeletePrimaryEnrollment();

    /// <summary>
    /// Deletes a specific face enrollment.
    /// </summary>
    /// <param name="faceId">The face identifier to delete.</param>
    /// <returns>True if deletion was successful.</returns>
    bool DeleteFace(string faceId);
}

/// <summary>
/// Implementation of enrollment path management.
/// </summary>
public class EnrollmentPathService : IEnrollmentPathService
{
    /// <inheritdoc />
    public string BaseDirectory { get; }

    /// <inheritdoc />
    public string PrimaryEnrollmentPath { get; }

    /// <inheritdoc />
    public string FacesDirectory { get; }

    /// <summary>
    /// Creates a new EnrollmentPathService with paths based on LocalApplicationData.
    /// </summary>
    public EnrollmentPathService()
    {
        BaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppDataFolderName);

        PrimaryEnrollmentPath = Path.Combine(BaseDirectory, AppConstants.EnrollmentFileName);
        FacesDirectory = Path.Combine(BaseDirectory, AppConstants.FacesDirectoryName);
    }

    /// <inheritdoc />
    public bool HasEnrollment()
    {
        return File.Exists(PrimaryEnrollmentPath);
    }

    /// <inheritdoc />
    public string GetFacePath(string faceId)
    {
        if (string.IsNullOrWhiteSpace(faceId))
        {
            throw new ArgumentException("Face ID cannot be null or empty.", nameof(faceId));
        }

        // Sanitize faceId to prevent path traversal
        var sanitizedId = Path.GetFileName(faceId);
        return Path.Combine(FacesDirectory, $"{sanitizedId}.dat");
    }

    /// <inheritdoc />
    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(FacesDirectory);
    }

    /// <inheritdoc />
    public bool DeletePrimaryEnrollment()
    {
        try
        {
            if (File.Exists(PrimaryEnrollmentPath))
            {
                File.Delete(PrimaryEnrollmentPath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool DeleteFace(string faceId)
    {
        try
        {
            if (faceId == "primary")
            {
                return DeletePrimaryEnrollment();
            }

            var facePath = GetFacePath(faceId);
            if (File.Exists(facePath))
            {
                File.Delete(facePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}

