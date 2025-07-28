using System.Security.Cryptography;
using FaceVault.Models;
using Microsoft.Extensions.Logging;

namespace FaceVault.Services;

public interface IHashCalculationService
{
    /// <summary>
    /// Calculate SHA256 hash for a file
    /// </summary>
    Task<string> CalculateFileHashAsync(string filePath);
    
    /// <summary>
    /// Calculate hash for multiple files with progress tracking
    /// </summary>
    Task<Dictionary<string, string>> CalculateFileHashesAsync(
        IEnumerable<string> filePaths, 
        IProgress<HashCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate if a file's current hash matches the stored hash
    /// </summary>
    Task<bool> ValidateFileHashAsync(string filePath, string expectedHash);
}


public class HashCalculationService : IHashCalculationService
{
    private readonly ILogger<HashCalculationService> _logger;
    private const int BufferSize = 8192; // 8KB buffer for efficient file reading

    public HashCalculationService(ILogger<HashCalculationService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CalculateFileHashAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(filePath);
            
            var hashBytes = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating hash for file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> CalculateFileHashesAsync(
        IEnumerable<string> filePaths, 
        IProgress<HashCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var filePathList = filePaths.ToList();
        var results = new Dictionary<string, string>();
        
        var progressInfo = new HashCalculationProgress
        {
            TotalFiles = filePathList.Count
        };

        // Calculate total bytes for more accurate progress
        foreach (var filePath in filePathList)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    progressInfo.TotalBytes += fileInfo.Length;
                }
                catch
                {
                    // Ignore errors getting file size
                }
            }
        }

        _logger.LogInformation("Starting hash calculation for {FileCount} files ({TotalMB:F1} MB)", 
            filePathList.Count, progressInfo.TotalBytes / 1024.0 / 1024.0);

        foreach (var filePath in filePathList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            progressInfo.CurrentFile = filePath;
            progress?.Report(progressInfo);

            try
            {
                var hash = await CalculateFileHashAsync(filePath);
                results[filePath] = hash;
                
                // Update processed bytes
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    progressInfo.ProcessedBytes += fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate hash for file: {FilePath}", filePath);
                // Continue processing other files
            }

            progressInfo.ProcessedFiles++;
        }

        _logger.LogInformation("Hash calculation completed: {ProcessedCount}/{TotalCount} files processed", 
            progressInfo.ProcessedFiles, progressInfo.TotalFiles);

        return results;
    }

    public async Task<bool> ValidateFileHashAsync(string filePath, string expectedHash)
    {
        try
        {
            var currentHash = await CalculateFileHashAsync(filePath);
            return string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating hash for file: {FilePath}", filePath);
            return false;
        }
    }
}