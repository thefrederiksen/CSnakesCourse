using CSnakes.Runtime;

namespace FaceVault.Services;

public interface IHeicConverterService
{
    Task<byte[]?> ConvertHeicToJpegAsync(string heicPath, int maxSize = 800, int quality = 85);
    Task<byte[]?> GetHeicThumbnailAsync(string heicPath, int maxSize = 250);
    Task<bool> CheckHeicSupportAsync();
}

public class HeicConverterService : IHeicConverterService
{
    private readonly IPythonEnvironment _pythonEnv;
    private readonly ILogger<HeicConverterService> _logger;

    public HeicConverterService(IPythonEnvironment pythonEnv, ILogger<HeicConverterService> logger)
    {
        _pythonEnv = pythonEnv;
        _logger = logger;
    }

    public async Task<byte[]?> ConvertHeicToJpegAsync(string heicPath, int maxSize = 800, int quality = 85)
    {
        try
        {
            _logger.LogDebug("Converting HEIC to JPEG: {FilePath}", heicPath);

            var result = await Task.Run(() =>
            {
                return _pythonEnv.HeicConverter().ConvertHeicToJpeg(heicPath, maxSize, quality);
            });

            if (result is byte[] jpegBytes && jpegBytes.Length > 0)
            {
                _logger.LogInformation("Successfully converted HEIC to JPEG: {FilePath} ({Size} bytes)", 
                    heicPath, jpegBytes.Length);
                return jpegBytes;
            }

            _logger.LogWarning("HEIC conversion returned no data: {FilePath}", heicPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting HEIC file to JPEG: {FilePath}", heicPath);
            return null;
        }
    }

    public async Task<byte[]?> GetHeicThumbnailAsync(string heicPath, int maxSize = 250)
    {
        try
        {
            var result = await Task.Run(() =>
            {
                return _pythonEnv.HeicConverter().GetHeicThumbnail(heicPath, maxSize);
            });

            if (result is byte[] thumbnailBytes && thumbnailBytes.Length > 0)
            {
                return thumbnailBytes;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HEIC thumbnail: {FilePath}", heicPath);
            return null;
        }
    }

    public async Task<bool> CheckHeicSupportAsync()
    {
        try
        {
            var result = await Task.Run(() =>
            {
                return _pythonEnv.HeicConverter().CheckHeicSupport();
            });

            if (result is System.Collections.Generic.IDictionary<string, object> supportInfo)
            {
                var hasSupport = supportInfo.TryGetValue("has_heic_support", out var value) && 
                               value is bool boolValue && boolValue;
                
                _logger.LogInformation("HEIC support check: {HasSupport}", hasSupport);
                return hasSupport;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking HEIC support");
            return false;
        }
    }
}