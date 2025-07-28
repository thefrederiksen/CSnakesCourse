namespace FaceVault.Services;

public interface IImageOrientationService
{
    /// <summary>
    /// Gets the CSS transform needed to correct image orientation based on EXIF data
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>CSS transform string (e.g., "rotate(90deg)") or empty string if no rotation needed</returns>
    Task<string> GetOrientationTransformAsync(string imagePath);

    /// <summary>
    /// Gets the EXIF orientation value from an image
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>EXIF orientation value (1-8) or 1 if no orientation data found</returns>
    Task<int> GetExifOrientationAsync(string imagePath);
}

public class ImageOrientationService : IImageOrientationService
{
    private readonly ILogger<ImageOrientationService> _logger;
    
    public ImageOrientationService(ILogger<ImageOrientationService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetOrientationTransformAsync(string imagePath)
    {
        try
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return string.Empty;

            var orientation = await GetExifOrientationAsync(imagePath);
            
            return orientation switch
            {
                1 => string.Empty, // Normal - no rotation needed
                2 => "scaleX(-1)", // Flip horizontal
                3 => "rotate(180deg)", // Rotate 180 degrees
                4 => "scaleY(-1)", // Flip vertical
                5 => "rotate(90deg) scaleX(-1)", // Rotate 90 degrees and flip horizontal
                6 => "rotate(90deg)", // Rotate 90 degrees clockwise
                7 => "rotate(270deg) scaleX(-1)", // Rotate 270 degrees and flip horizontal
                8 => "rotate(270deg)", // Rotate 270 degrees clockwise (90 degrees counter-clockwise)
                _ => string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting orientation transform for image: {ImagePath}", imagePath);
            return string.Empty;
        }
    }

    public async Task<int> GetExifOrientationAsync(string imagePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                    return 1;

                // Skip non-image files
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                if (!IsImageFile(extension))
                    return 1;

                // System.Drawing.Image is only supported on Windows 6.1 and later
                if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                {
                    _logger.LogDebug("EXIF orientation reading is only supported on Windows 6.1 and later");
                    return 1;
                }

                using var image = System.Drawing.Image.FromFile(imagePath);
                
                // PropertyTagOrientation = 0x0112
                const int orientationPropertyId = 0x0112;
                
                if (image.PropertyIdList.Contains(orientationPropertyId))
                {
                    var orientationProperty = image.GetPropertyItem(orientationPropertyId);
                    if (orientationProperty?.Value != null && orientationProperty.Value.Length > 0)
                    {
                        return orientationProperty.Value[0];
                    }
                }
                
                return 1; // Default orientation
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read EXIF orientation from image: {ImagePath}", imagePath);
                return 1; // Default orientation if we can't read EXIF data
            }
        });
    }

    private static bool IsImageFile(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp" };
        return imageExtensions.Contains(extension);
    }
}