using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Models;
using System.Security.Cryptography;
using System.Drawing;
using System.Drawing.Imaging;

namespace FaceVault.Services;

public interface IPhotoScannerService
{
    Task<ScanResult> ScanDirectoryAsync(string directory, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<ScanResult> QuickScanAsync(string directory, CancellationToken cancellationToken = default);
    Task<ImageMetadata> ExtractMetadataAsync(string filePath);
    Task<string> CalculateFileHashAsync(string filePath);
    Task<bool> IsImageFileAsync(string filePath);
    string[] GetSupportedExtensions();
}

public class PhotoScannerService : IPhotoScannerService
{
    private readonly FaceVaultDbContext _context;
    private readonly ISettingsService _settingsService;
    private readonly string[] _supportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif", ".raw", ".cr2", ".nef", ".orf", ".arw"
    };

    public PhotoScannerService(FaceVaultDbContext context, ISettingsService settingsService)
    {
        _context = context;
        _settingsService = settingsService;
    }

    public async Task<ScanResult> ScanDirectoryAsync(string directory, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            DirectoryPath = directory,
            StartTime = DateTime.UtcNow
        };

        try
        {
            if (!Directory.Exists(directory))
            {
                result.Error = $"Directory does not exist: {directory}";
                return result;
            }

            var settings = await _settingsService.GetSettingsAsync();
            var searchOption = settings.ScanSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Get all image files
            var allFiles = new List<string>();
            foreach (var ext in _supportedExtensions)
            {
                var pattern = $"*{ext}";
                var files = Directory.GetFiles(directory, pattern, searchOption);
                allFiles.AddRange(files);
            }

            result.TotalFilesFound = allFiles.Count;
            progress?.Report(new ScanProgress 
            { 
                CurrentFile = "", 
                ProcessedCount = 0, 
                TotalCount = allFiles.Count, 
                Phase = ScanPhase.Discovery,
                Message = $"Found {allFiles.Count} image files"
            });

            if (cancellationToken.IsCancellationRequested)
            {
                result.IsCancelled = true;
                return result;
            }

            // Process files in batches
            var batchSize = await _settingsService.GetBatchSizeAsync();
            var processedCount = 0;

            for (int i = 0; i < allFiles.Count; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.IsCancelled = true;
                    break;
                }

                var batch = allFiles.Skip(i).Take(batchSize);
                
                foreach (var filePath in batch)
                {
                    try
                    {
                        progress?.Report(new ScanProgress 
                        { 
                            CurrentFile = Path.GetFileName(filePath), 
                            ProcessedCount = processedCount, 
                            TotalCount = allFiles.Count,
                            Phase = ScanPhase.Processing,
                            Message = $"Processing {Path.GetFileName(filePath)}"
                        });

                        var existingImage = await _context.Images
                            .FirstOrDefaultAsync(img => img.FilePath == filePath, cancellationToken);

                        if (existingImage != null)
                        {
                            result.SkippedCount++;
                            processedCount++;
                            continue;
                        }

                        var metadata = await ExtractMetadataAsync(filePath);
                        var fileHash = await CalculateFileHashAsync(filePath);

                        var image = new Models.Image
                        {
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath),
                            FileSizeBytes = new FileInfo(filePath).Length,
                            FileHash = fileHash,
                            FileExtension = Path.GetExtension(filePath).ToLowerInvariant(),
                            Width = metadata.Width,
                            Height = metadata.Height,
                            DateTaken = metadata.DateTaken,
                            DateCreated = DateTime.UtcNow,
                            DateModified = File.GetLastWriteTimeUtc(filePath),
                            CameraMake = metadata.CameraMake,
                            CameraModel = metadata.CameraModel,
                            IsProcessed = false,
                            IsDeleted = false
                        };

                        _context.Images.Add(image);
                        result.NewImagesCount++;
                        processedCount++;

                        // Save in batches to avoid memory issues
                        if (result.NewImagesCount % batchSize == 0)
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                            Logger.Info($"Saved batch of {batchSize} images to database");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing {filePath}: {ex.Message}");
                        Logger.Error($"Error processing image {filePath}: {ex.Message}");
                        processedCount++;
                    }
                }

                // Memory management - force garbage collection after each batch
                if (i % (batchSize * 5) == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Save any remaining changes
            if (result.NewImagesCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            result.EndTime = DateTime.UtcNow;
            result.IsSuccess = !result.IsCancelled && result.ErrorCount == 0;

            // Update settings with scan completion
            var updatedSettings = await _settingsService.GetSettingsAsync();
            updatedSettings.MarkScanCompleted();
            await _settingsService.SaveSettingsAsync(updatedSettings);

            progress?.Report(new ScanProgress 
            { 
                CurrentFile = "", 
                ProcessedCount = processedCount, 
                TotalCount = allFiles.Count,
                Phase = ScanPhase.Complete,
                Message = $"Scan complete: {result.NewImagesCount} new images, {result.SkippedCount} skipped, {result.ErrorCount} errors"
            });

            Logger.Info($"Photo scan completed: {result.NewImagesCount} new images added, {result.SkippedCount} skipped, {result.ErrorCount} errors");

        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            Logger.Error($"Photo scan failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ScanResult> QuickScanAsync(string directory, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            DirectoryPath = directory,
            StartTime = DateTime.UtcNow
        };

        try
        {
            if (!Directory.Exists(directory))
            {
                result.Error = $"Directory does not exist: {directory}";
                return result;
            }

            var settings = await _settingsService.GetSettingsAsync();
            var searchOption = settings.ScanSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Quick count of image files
            var totalFiles = 0;
            foreach (var ext in _supportedExtensions)
            {
                var pattern = $"*{ext}";
                var files = Directory.GetFiles(directory, pattern, searchOption);
                totalFiles += files.Length;
            }

            result.TotalFilesFound = totalFiles;
            result.EndTime = DateTime.UtcNow;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    public Task<ImageMetadata> ExtractMetadataAsync(string filePath)
    {
        var metadata = new ImageMetadata();

        try
        {
            // Suppress System.Drawing platform warnings - this is a Windows-focused application
#pragma warning disable CA1416
            using var image = System.Drawing.Image.FromFile(filePath);
            metadata.Width = image.Width;
            metadata.Height = image.Height;

            // Extract EXIF data
            foreach (var prop in image.PropertyItems)
            {
                switch (prop.Id)
                {
                    case 0x010F: // Camera make
                        if (prop.Value != null)
                            metadata.CameraMake = System.Text.Encoding.ASCII.GetString(prop.Value).TrimEnd('\0');
                        break;
                    case 0x0110: // Camera model
                        if (prop.Value != null)
                            metadata.CameraModel = System.Text.Encoding.ASCII.GetString(prop.Value).TrimEnd('\0');
                        break;
                    case 0x0132: // DateTime
                        if (prop.Value != null)
                        {
                            var dateStr = System.Text.Encoding.ASCII.GetString(prop.Value).TrimEnd('\0');
                            if (DateTime.TryParseExact(dateStr, "yyyy:MM:dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var dateTime))
                            {
                                metadata.DateTaken = dateTime;
                            }
                        }
                        break;
                    case 0x829A: // Exposure time
                        if (prop.Value != null && prop.Value.Length >= 8)
                        {
                            var numerator = BitConverter.ToUInt32(prop.Value, 0);
                            var denominator = BitConverter.ToUInt32(prop.Value, 4);
                            if (denominator > 0)
                            {
                                metadata.ExposureTime = $"{numerator}/{denominator}";
                            }
                        }
                        break;
                    case 0x829D: // F-Number
                        if (prop.Value != null && prop.Value.Length >= 8)
                        {
                            var numerator = BitConverter.ToUInt32(prop.Value, 0);
                            var denominator = BitConverter.ToUInt32(prop.Value, 4);
                            if (denominator > 0)
                            {
                                metadata.FNumber = $"f/{(double)numerator / denominator:F1}";
                            }
                        }
                        break;
                    case 0x8827: // ISO
                        if (prop.Value != null && prop.Value.Length >= 2)
                        {
                            metadata.ISO = BitConverter.ToUInt16(prop.Value, 0);
                        }
                        break;
                }
            }
#pragma warning restore CA1416
        }
        catch (Exception ex)
        {
            Logger.Warning($"Could not extract metadata from {filePath}: {ex.Message}");
            // Return basic metadata even if EXIF extraction fails
            try
            {
#pragma warning disable CA1416
                using var image = System.Drawing.Image.FromFile(filePath);
                metadata.Width = image.Width;
                metadata.Height = image.Height;
#pragma warning restore CA1416
            }
            catch
            {
                // If we can't even get basic dimensions, set defaults
                metadata.Width = 0;
                metadata.Height = 0;
            }
        }

        // Fallback to file dates if no EXIF date
        if (!metadata.DateTaken.HasValue)
        {
            var fileInfo = new FileInfo(filePath);
            metadata.DateTaken = fileInfo.CreationTimeUtc < fileInfo.LastWriteTimeUtc 
                ? fileInfo.CreationTimeUtc 
                : fileInfo.LastWriteTimeUtc;
        }

        return Task.FromResult(metadata);
    }

    public async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
        return Convert.ToHexString(hashBytes);
    }

    public Task<bool> IsImageFileAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var isSupported = _supportedExtensions.Contains(extension);

        if (!isSupported)
            return Task.FromResult(false);

        try
        {
            // Verify it's actually an image by trying to open it
#pragma warning disable CA1416
            using var image = System.Drawing.Image.FromFile(filePath);
#pragma warning restore CA1416
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string[] GetSupportedExtensions()
    {
        return _supportedExtensions;
    }
}

// Support classes
public class ScanResult
{
    public string DirectoryPath { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalFilesFound { get; set; }
    public int NewImagesCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess { get; set; }
    public bool IsCancelled { get; set; }
    public string? Error { get; set; }
    
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    public double FilesPerSecond => Duration.TotalSeconds > 0 ? TotalFilesFound / Duration.TotalSeconds : 0;
}

public class ScanProgress
{
    public string CurrentFile { get; set; } = "";
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public ScanPhase Phase { get; set; }
    public string Message { get; set; } = "";
    public double PercentComplete => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
}

public enum ScanPhase
{
    Discovery,
    Processing,
    Complete,
    Error
}

public class ImageMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime? DateTaken { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? ExposureTime { get; set; }
    public string? FNumber { get; set; }
    public int? ISO { get; set; }
}