using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Models;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace FaceVault.Services;

public interface IFastPhotoScannerService
{
    Task<ScanResult> ScanDirectoryAsync(string directory, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<QuickScanResult> QuickScanAsync(string directory, CancellationToken cancellationToken = default);
    string[] GetSupportedExtensions();
}

public class FastPhotoScannerService : IFastPhotoScannerService
{
    private readonly FaceVaultDbContext _context;
    private readonly ISettingsService _settingsService;
    private readonly string[] _supportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif"
    };

    public FastPhotoScannerService(FaceVaultDbContext context, ISettingsService settingsService)
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

            // Get existing files from database to avoid duplicates
            progress?.Report(new ScanProgress 
            { 
                CurrentFile = "", 
                ProcessedCount = 0, 
                TotalCount = allFiles.Count, 
                Phase = ScanPhase.Processing,
                Message = "Checking for existing files in database..."
            });

            var existingPaths = await _context.Images
                .Where(img => allFiles.Contains(img.FilePath))
                .Select(img => img.FilePath)
                .ToHashSetAsync(cancellationToken);

            var newFiles = allFiles.Where(f => !existingPaths.Contains(f)).ToList();
            result.SkippedCount = allFiles.Count - newFiles.Count;

            if (newFiles.Count == 0)
            {
                result.EndTime = DateTime.UtcNow;
                result.IsSuccess = true;
                progress?.Report(new ScanProgress 
                { 
                    CurrentFile = "", 
                    ProcessedCount = allFiles.Count, 
                    TotalCount = allFiles.Count,
                    Phase = ScanPhase.Complete,
                    Message = "All files already in database"
                });
                return result;
            }

            // Process files in parallel batches
            var batchSize = await _settingsService.GetBatchSizeAsync();
            var maxConcurrency = Math.Min(Environment.ProcessorCount, 4); // Limit to 4 threads
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var processedCount = 0;
            var imagesToAdd = new ConcurrentBag<Models.Image>();

            // Process in chunks for database saves
            for (int i = 0; i < newFiles.Count; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.IsCancelled = true;
                    break;
                }

                var batch = newFiles.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(async filePath =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var image = await ProcessImageFastAsync(filePath, cancellationToken);
                        if (image != null)
                        {
                            imagesToAdd.Add(image);
                        }
                        
                        Interlocked.Increment(ref processedCount);
                        
                        progress?.Report(new ScanProgress 
                        { 
                            CurrentFile = Path.GetFileName(filePath), 
                            ProcessedCount = processedCount, 
                            TotalCount = allFiles.Count,
                            Phase = ScanPhase.Processing,
                            Message = $"Processing {Path.GetFileName(filePath)}"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Error processing {filePath}: {ex.Message}");
                        Interlocked.Increment(ref processedCount);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                // Save batch to database
                if (imagesToAdd.Count > 0)
                {
                    var imagesToSave = imagesToAdd.ToArray();
                    imagesToAdd.Clear();
                    
                    try
                    {
                        // Use a transaction for batch insert
                        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                        
                        await _context.Images.AddRangeAsync(imagesToSave, cancellationToken);
                        var savedCount = await _context.SaveChangesAsync(cancellationToken);
                        
                        await transaction.CommitAsync(cancellationToken);
                        
                        result.NewImagesCount += savedCount;
                        Logger.Info($"Successfully committed batch of {savedCount} images to database");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error saving batch to database: {ex.Message}");
                        Logger.Error($"Exception Type: {ex.GetType().Name}");
                        Logger.Error($"Exception Details: {ex}");
                        Logger.Error($"Inner Exception: {ex.InnerException?.Message ?? "None"}");
                        
                        result.ErrorCount += imagesToSave.Length;
                        
                        // Clear the context to prevent issues, but only after transaction failure
                        _context.ChangeTracker.Clear();
                        
                        // Log specific SQLite errors
                        if (ex.Message.Contains("database is locked"))
                        {
                            Logger.Error("Database is locked during batch save. This may indicate concurrent access issues.");
                        }
                        else if (ex.Message.Contains("constraint"))
                        {
                            Logger.Error("Database constraint violation during batch save. Check for duplicate keys or foreign key issues.");
                        }
                        else if (ex.Message.Contains("disk I/O error"))
                        {
                            Logger.Error("Disk I/O error during batch save. Check disk space and permissions.");
                        }
                        
                        Logger.Error($"Transaction failed for batch of {imagesToSave.Length} images");
                    }
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.IsSuccess = !result.IsCancelled && result.ErrorCount < (result.TotalFilesFound * 0.1); // Allow 10% error rate

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

            Logger.Info($"Fast photo scan completed: {result.NewImagesCount} new images added, {result.SkippedCount} skipped, {result.ErrorCount} errors");

        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            Logger.Error($"Fast photo scan failed: {ex.Message}");
        }

        return result;
    }

    private async Task<Models.Image?> ProcessImageFastAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            // Get file info in one call
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return null;

            // Fast hash calculation - just first 64KB + file size
            var quickHash = await CalculateQuickHashAsync(filePath, cancellationToken);
            
            // Basic metadata without loading full image
            var basicMetadata = GetBasicMetadata(fileInfo);

            var image = new Models.Image
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSizeBytes = fileInfo.Length,
                FileHash = quickHash,
                FileExtension = fileInfo.Extension.ToLowerInvariant(),
                Width = basicMetadata.Width,
                Height = basicMetadata.Height,
                DateTaken = basicMetadata.DateTaken ?? fileInfo.CreationTimeUtc,
                DateCreated = DateTime.UtcNow,
                DateModified = fileInfo.LastWriteTimeUtc,
                CameraMake = basicMetadata.CameraMake,
                CameraModel = basicMetadata.CameraModel,
                IsProcessed = false,
                IsDeleted = false
            };

            return image;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Fast processing failed for {filePath}: {ex.Message}");
            return null;
        }
    }

    private async Task<string> CalculateQuickHashAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan);
            
            // Read first 64KB + file size for quick hash
            var buffer = new byte[65536]; // 64KB
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            
            // Include file size in hash for uniqueness
            var sizeBytes = BitConverter.GetBytes(stream.Length);
            var combinedData = new byte[bytesRead + sizeBytes.Length];
            Array.Copy(buffer, 0, combinedData, 0, bytesRead);
            Array.Copy(sizeBytes, 0, combinedData, bytesRead, sizeBytes.Length);
            
            var hashBytes = sha256.ComputeHash(combinedData);
            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            // Fallback to simple hash
            return $"QUICK_{Path.GetFileName(filePath)}_{new FileInfo(filePath).Length}_{DateTime.UtcNow.Ticks}";
        }
    }

    private BasicImageMetadata GetBasicMetadata(FileInfo fileInfo)
    {
        var metadata = new BasicImageMetadata
        {
            DateTaken = fileInfo.CreationTimeUtc < fileInfo.LastWriteTimeUtc 
                ? fileInfo.CreationTimeUtc 
                : fileInfo.LastWriteTimeUtc
        };

        // Try to get basic dimensions without loading full image
        try
        {
            // For common formats, we can read dimensions from file headers
            var extension = fileInfo.Extension.ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    metadata = GetJpegDimensions(fileInfo.FullName) ?? metadata;
                    break;
                case ".png":
                    metadata = GetPngDimensions(fileInfo.FullName) ?? metadata;
                    break;
                default:
                    // For other formats, set reasonable defaults
                    metadata.Width = 0;
                    metadata.Height = 0;
                    break;
            }
        }
        catch
        {
            // If header reading fails, use defaults
            metadata.Width = 0;
            metadata.Height = 0;
        }

        return metadata;
    }

    private BasicImageMetadata? GetJpegDimensions(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // Read JPEG header
            if (reader.ReadUInt16() != 0xD8FF) // JPEG SOI marker
                return null;

            while (stream.Position < stream.Length)
            {
                var marker = reader.ReadUInt16();
                if ((marker & 0xFF00) != 0xFF00)
                    break;

                var segmentLength = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());

                // SOF0 marker (Start of Frame)
                if (marker == 0xC0FF)
                {
                    reader.ReadByte(); // precision
                    var height = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
                    var width = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
                    
                    return new BasicImageMetadata { Width = width, Height = height };
                }

                stream.Seek(segmentLength - 2, SeekOrigin.Current);
            }
        }
        catch
        {
            // Fall back to defaults on any error
        }

        return null;
    }

    private BasicImageMetadata? GetPngDimensions(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // Read PNG header
            var signature = reader.ReadBytes(8);
            if (!IsPngSignature(signature))
                return null;

            // Read IHDR chunk
            var chunkLength = ReadBigEndianUInt32(reader);
            var chunkType = reader.ReadBytes(4);
            
            if (chunkType[0] == 'I' && chunkType[1] == 'H' && chunkType[2] == 'D' && chunkType[3] == 'R')
            {
                var width = (int)ReadBigEndianUInt32(reader);
                var height = (int)ReadBigEndianUInt32(reader);
                
                return new BasicImageMetadata { Width = width, Height = height };
            }
        }
        catch
        {
            // Fall back to defaults on any error
        }

        return null;
    }

    private bool IsPngSignature(byte[] signature)
    {
        return signature.Length == 8 &&
               signature[0] == 0x89 && signature[1] == 0x50 && signature[2] == 0x4E && signature[3] == 0x47 &&
               signature[4] == 0x0D && signature[5] == 0x0A && signature[6] == 0x1A && signature[7] == 0x0A;
    }

    private uint ReadBigEndianUInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    public async Task<QuickScanResult> QuickScanAsync(string directory, CancellationToken cancellationToken = default)
    {
        var result = new QuickScanResult
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
            await Task.Run(() =>
            {
                foreach (var ext in _supportedExtensions)
                {
                    var pattern = $"*{ext}";
                    var files = Directory.GetFiles(directory, pattern, searchOption);
                    totalFiles += files.Length;
                }
            }, cancellationToken);

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

    public string[] GetSupportedExtensions()
    {
        return _supportedExtensions;
    }
}

public class BasicImageMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime? DateTaken { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
}

public class QuickScanResult
{
    public string DirectoryPath { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalFilesFound { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
}