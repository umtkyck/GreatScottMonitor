using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace CXA.Client.Helpers;

/// <summary>
/// Helper class for image conversion operations.
/// Provides thread-safe, reusable image conversion methods used throughout the application.
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Convert OpenCV Mat to WPF BitmapSource.
    /// Creates a frozen (immutable) BitmapSource that can be used on any thread.
    /// </summary>
    /// <param name="mat">The OpenCV Mat to convert.</param>
    /// <param name="logger">Optional logger for error reporting.</param>
    /// <returns>A frozen BitmapSource, or null if conversion fails.</returns>
    /// <remarks>
    /// This method handles different color channel configurations:
    /// - Single channel (grayscale) is converted to BGR.
    /// - 4 channel (BGRA) is converted to BGR.
    /// - 3 channel (BGR) is used as-is.
    /// 
    /// The returned BitmapSource is frozen for thread safety.
    /// </remarks>
    public static BitmapSource? MatToBitmapSource(Mat? mat, ILogger? logger = null)
    {
        if (mat == null || mat.Empty())
        {
            return null;
        }

        try
        {
            // Convert to BGR if necessary
            using var displayMat = EnsureBgrFormat(mat);
            
            int width = displayMat.Width;
            int height = displayMat.Height;
            
            // Calculate stride with 4-byte alignment for WPF
            int stride = (width * 3 + 3) & ~3;

            byte[] pixels = new byte[height * stride];
            
            // Copy pixel data row by row
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(displayMat.Ptr(y), pixels, y * stride, width * 3);
            }

            var bitmapSource = BitmapSource.Create(
                width, height,
                96, 96, // Standard DPI
                PixelFormats.Bgr24,
                null,
                pixels,
                stride);

            // Freeze for thread safety
            bitmapSource.Freeze();

            return bitmapSource;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error converting Mat to BitmapSource");
            return null;
        }
    }

    /// <summary>
    /// Ensures the Mat is in BGR format for WPF display.
    /// </summary>
    /// <param name="mat">Source Mat in any format.</param>
    /// <returns>A new Mat in BGR format, or a clone if already BGR.</returns>
    private static Mat EnsureBgrFormat(Mat mat)
    {
        return mat.Channels() switch
        {
            1 => mat.CvtColor(ColorConversionCodes.GRAY2BGR),
            4 => mat.CvtColor(ColorConversionCodes.BGRA2BGR),
            _ => mat.Clone() // Already BGR or unknown - clone to ensure ownership
        };
    }

    /// <summary>
    /// Flip a frame horizontally for mirror effect (selfie camera).
    /// </summary>
    /// <param name="frame">The frame to flip.</param>
    /// <returns>The flipped frame (modified in place).</returns>
    public static Mat FlipHorizontal(Mat frame)
    {
        if (frame == null || frame.Empty())
        {
            return frame!;
        }

        Cv2.Flip(frame, frame, FlipMode.Y);
        return frame;
    }

    /// <summary>
    /// Convert BitmapSource to JPEG Base64 string for IPC transmission.
    /// </summary>
    /// <param name="frame">The BitmapSource to convert.</param>
    /// <param name="quality">JPEG quality level (0-100).</param>
    /// <returns>Base64 encoded JPEG string.</returns>
    public static string BitmapSourceToBase64(BitmapSource frame, int quality = 80)
    {
        if (frame == null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(frame));
        
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
}

