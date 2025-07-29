namespace FaceVault.Services;

public interface IImageService
{
    Task<byte[]?> GetImageBytesAsync(string filePath);
    Task<byte[]?> GetThumbnailAsync(string filePath, int maxSize = 300);
    Task<byte[]?> GetImageThumbnailAsync(string filePath, int maxSize = 300);
    Task<string> GetImageMimeTypeAsync(string filePath);
    bool IsValidImagePath(string filePath);
    Task<string> GetImageDataUrlAsync(string filePath, int maxSize = 300);
}