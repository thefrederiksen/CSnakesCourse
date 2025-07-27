using System.Drawing;
using System.Drawing.Imaging;

namespace FaceVault.Services;

public class ImageService : IImageService
{
    private readonly string[] _supportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp"
    };

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
            Logger.Error($"Error reading image {filePath}: {ex.Message}");
            return null;
        }
    }

    public Task<byte[]?> GetThumbnailAsync(string filePath, int maxSize = 300)
    {
        try
        {
            if (!File.Exists(filePath) || !IsValidImagePath(filePath))
                return Task.FromResult<byte[]?>(null);

            // Suppress System.Drawing platform warnings - this is a Windows-focused application
#pragma warning disable CA1416
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
            return Task.FromResult<byte[]?>(stream.ToArray());
#pragma warning restore CA1416
        }
        catch (Exception ex)
        {
            Logger.Error($"Error creating thumbnail for {filePath}: {ex.Message}");
            return Task.FromResult<byte[]?>(null);
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
            Logger.Error($"Error creating data URL for {filePath}: {ex.Message}");
            return string.Empty;
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