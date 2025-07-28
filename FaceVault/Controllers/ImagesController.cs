using Microsoft.AspNetCore.Mvc;
using FaceVault.Services;
using FaceVault.Data;
using Microsoft.EntityFrameworkCore;

namespace FaceVault.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly FaceVaultDbContext _context;
    private readonly IImageService _imageService;
    private readonly IHeicConverterService _heicConverter;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(FaceVaultDbContext context, IImageService imageService, IHeicConverterService heicConverter, ILogger<ImagesController> logger)
    {
        _context = context;
        _imageService = imageService;
        _heicConverter = heicConverter;
        _logger = logger;
    }

    [HttpGet("{id}/view")]
    public async Task<IActionResult> ViewImage(int id)
    {
        try
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(image.FilePath))
            {
                _logger.LogWarning("Image file not found: {FilePath}", image.FilePath);
                return NotFound("Image file not found");
            }

            var extension = Path.GetExtension(image.FilePath).ToLowerInvariant();
            
            // For HEIC/HEIF files, convert to JPEG for browser compatibility
            if (extension == ".heic" || extension == ".heif")
            {
                try
                {
                    // Convert HEIC to JPEG for display
                    var jpegBytes = await _heicConverter.ConvertHeicToJpegAsync(image.FilePath, 1200, 90);
                    if (jpegBytes != null && jpegBytes.Length > 0)
                    {
                        return File(jpegBytes, "image/jpeg");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not convert HEIC file {FilePath} to JPEG", image.FilePath);
                }
                
                // Fallback: return original file and let browser try to handle it
                var fileStream = System.IO.File.OpenRead(image.FilePath);
                return File(fileStream, "image/heic", enableRangeProcessing: true);
            }

            var contentType = GetContentType(image.FilePath);
            var originalFileStream = System.IO.File.OpenRead(image.FilePath);
            
            return File(originalFileStream, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving image {Id}", id);
            return StatusCode(500, "Error serving image");
        }
    }

    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int id, [FromQuery] int maxSize = 250)
    {
        try
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            // Generate thumbnail
            var thumbnailBytes = await _imageService.GetImageThumbnailAsync(image.FilePath, maxSize);
            if (thumbnailBytes == null || thumbnailBytes.Length == 0)
            {
                // Fallback to full image if thumbnail generation fails
                return await ViewImage(id);
            }

            return File(thumbnailBytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for image {Id}", id);
            // Fallback to full image
            return await ViewImage(id);
        }
    }

    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
    }
}