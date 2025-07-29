using System.Drawing;
using System.Drawing.Imaging;

namespace FaceVault.Services;

public class ImageService : IImageService
{
    private readonly ILogger<ImageService> _logger;
    private readonly IHeicConverterService _heicConverter;
    private readonly string[] _supportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif"
    };

    public ImageService(ILogger<ImageService> logger, IHeicConverterService heicConverter)
    {
        _logger = logger;
        _heicConverter = heicConverter;
    }

    public async Task<byte[]?> GetImageBytesAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath) || !IsValidImagePath(filePath))
                return null;

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading image {filePath}: {ex.Message}");
            return null;
        }
    }

    public async Task<byte[]?> GetThumbnailAsync(string filePath, int maxSize = 300)
    {
        try
        {
            if (!File.Exists(filePath) || !IsValidImagePath(filePath))
                return null;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // HEIC/HEIF files need special handling as System.Drawing can't process them
            if (extension == ".heic" || extension == ".heif")
            {
                _logger.LogDebug($"Converting HEIC/HEIF file to JPEG thumbnail: {filePath}");
                return await ConvertHeicThumbnailAsync(filePath, maxSize);
            }

            // Suppress System.Drawing platform warnings - this is a Windows-focused application
#pragma warning disable CA1416
            return await Task.Run(() =>
            {
                using var originalImage = Image.FromFile(filePath);
                
                // Calculate thumbnail dimensions maintaining aspect ratio
                var (width, height) = CalculateThumbnailSize(originalImage.Width, originalImage.Height, maxSize);
                
                using var thumbnail = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(thumbnail);
                
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                
                graphics.DrawImage(originalImage, 0, 0, width, height);
                
                using var stream = new MemoryStream();
                thumbnail.Save(stream, ImageFormat.Jpeg);
                return stream.ToArray();
            });
#pragma warning restore CA1416
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating thumbnail for {filePath}: {ex.Message}");
            return null;
        }
    }

    public Task<string> GetImageMimeTypeAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".webp" => "image/webp",
            _ => "image/jpeg" // fallback
        };
        return Task.FromResult(mimeType);
    }

    public bool IsValidImagePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return _supportedExtensions.Contains(extension);
    }

    public async Task<string> GetImageDataUrlAsync(string filePath, int maxSize = 300)
    {
        try
        {
            var imageBytes = await GetThumbnailAsync(filePath, maxSize);
            if (imageBytes == null)
                return string.Empty;

            var mimeType = await GetImageMimeTypeAsync(filePath);
            var base64 = Convert.ToBase64String(imageBytes);
            return $"data:{mimeType};base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating data URL for {filePath}: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<byte[]?> GetImageThumbnailAsync(string filePath, int maxSize = 300)
    {
        // This is an alias for GetThumbnailAsync
        return await GetThumbnailAsync(filePath, maxSize);
    }

    private async Task<byte[]?> ConvertHeicThumbnailAsync(string filePath, int maxSize)
    {
        try
        {
            var thumbnailBytes = await _heicConverter.GetHeicThumbnailAsync(filePath, maxSize);
            if (thumbnailBytes != null && thumbnailBytes.Length > 0)
            {
                _logger.LogDebug($"Successfully converted HEIC thumbnail: {filePath} ({thumbnailBytes.Length} bytes)");
                return thumbnailBytes;
            }

            _logger.LogWarning($"HEIC thumbnail conversion failed for: {filePath}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error converting HEIC thumbnail: {filePath}");
            return null;
        }
    }

    private static (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight, int maxSize)
    {
        if (originalWidth <= maxSize && originalHeight <= maxSize)
            return (originalWidth, originalHeight);

        var ratio = Math.Min((double)maxSize / originalWidth, (double)maxSize / originalHeight);
        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }
}