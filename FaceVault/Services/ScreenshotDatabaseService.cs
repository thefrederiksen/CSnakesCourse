using FaceVault.Data;
using FaceVault.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FaceVault.Services;

public class ScreenshotDatabaseService : IScreenshotDatabaseService
{
    private readonly FaceVaultDbContext _context;
    private readonly IScreenshotDetectionService _screenshotService;
    private readonly ILogger<ScreenshotDatabaseService> _logger;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public ScreenshotDatabaseService(
        FaceVaultDbContext context,
        IScreenshotDetectionService screenshotService,
        ILogger<ScreenshotDatabaseService> logger)
    {
        _context = context;
        _screenshotService = screenshotService;
        _logger = logger;
    }

    public async Task<ScreenshotScanResult> FullScreenshotScanAsync(IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full screenshot scan - resetting all image statuses to Unknown");
        
        // First, reset all image statuses to Unknown
        var resetCount = await ResetAllScreenshotStatusAsync();
        _logger.LogInformation("Reset {ResetCount} images to Unknown status", resetCount);

        // Then scan all images
        var imageIds = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists)
            .Select(img => img.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Starting full scan of {TotalImages} images", imageIds.Count);
        return await ScanImagesAsync(imageIds, progress, cancellationToken);
    }

    public async Task<ScreenshotScanResult> ScanNewScreenshotsAsync(IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting differential screenshot scan - only Unknown status images");
        
        // Get only images with Unknown status
        var imageIds = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists && img.ScreenshotStatus == ScreenshotStatus.Unknown)
            .Select(img => img.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Starting differential scan of {UnknownImages} unknown images", imageIds.Count);
        return await ScanImagesAsync(imageIds, progress, cancellationToken);
    }

    public async Task<int> ResetAllScreenshotStatusAsync()
    {
        var affectedRows = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE Images SET ScreenshotStatus = {0}, IsScreenshot = {1}, ScreenshotConfidence = {2} WHERE IsDeleted = 0 AND FileExists = 1",
            (int)ScreenshotStatus.Unknown, false, 0.0);
        
        _logger.LogInformation("Reset {AffectedRows} images to Unknown screenshot status", affectedRows);
        return affectedRows;
    }

    public async Task<int> GetUnprocessedImageCountAsync()
    {
        return await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists && img.ScreenshotStatus == ScreenshotStatus.Unknown)
            .CountAsync();
    }

    public async Task<ScreenshotScanResult> ScanAllImagesAsync(IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        // Legacy method - scan all images without resetting
        var imageIds = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists)
            .Select(img => img.Id)
            .ToListAsync(cancellationToken);

        return await ScanImagesAsync(imageIds, progress, cancellationToken);
    }

    public async Task<ScreenshotScanResult> ScanImagesAsync(IEnumerable<int> imageIds, IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await _processingLock.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var imageIdList = imageIds.ToList();
            var result = new ScreenshotScanResult
            {
                TotalImages = imageIdList.Count
            };

            var scanProgress = new ScreenshotScanProgress
            {
                TotalImages = imageIdList.Count
            };

            if (imageIdList.Count == 0)
            {
                _logger.LogInformation("No images to process for screenshot detection");
                progress?.Report(scanProgress);
                return result;
            }

            _logger.LogInformation("Processing {TotalImages} images for screenshot detection", imageIdList.Count);

            const int batchSize = 20;
            var maxConcurrency = Environment.ProcessorCount;
            const int progressUpdateIntervalMs = 250;

            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var lastProgressUpdate = DateTime.UtcNow;

            // Process images in batches
            for (int i = 0; i < imageIdList.Count; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    break;
                }

                var batchIds = imageIdList.Skip(i).Take(batchSize).ToList();
                var batchImages = await _context.Images
                    .Where(img => batchIds.Contains(img.Id))
                    .ToListAsync(cancellationToken);

                var batchTasks = batchImages.Select(async image =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await ProcessSingleImageAsync(image, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);

                // Update database with batch results
                foreach (var (image, detectionResult, error) in batchResults)
                {
                    if (error != null)
                    {
                        result.Errors.Add($"{image.FileName}: {error}");
                        result.ErrorCount++;
                    }
                    else if (detectionResult != null)
                    {
                        // Check if the analysis was reliable
                        bool hasAnalysisError = false;
                        if (detectionResult.Analysis != null && detectionResult.Analysis.ContainsKey("error"))
                        {
                            var errorMsg = detectionResult.Analysis["error"]?.ToString() ?? "";
                            if (errorMsg.Contains("Image analysis libraries not available") || 
                                errorMsg.Contains("file not found"))
                            {
                                hasAnalysisError = true;
                                _logger.LogWarning("Unreliable screenshot detection for {FilePath}: {Error}", 
                                    image.FilePath, errorMsg);
                            }
                        }
                        
                        // Only update if we have a reliable result
                        if (!hasAnalysisError && string.IsNullOrEmpty(detectionResult.Error))
                        {
                            // Update both new and legacy fields
                            var newStatus = detectionResult.IsScreenshot ? ScreenshotStatus.IsScreenshot : ScreenshotStatus.NotScreenshot;
                            image.ScreenshotStatus = newStatus;
                            image.IsScreenshot = detectionResult.IsScreenshot; // Keep for backward compatibility
                            image.ScreenshotConfidence = detectionResult.Confidence;
                            
                            if (detectionResult.IsScreenshot)
                                result.ScreenshotsFound++;
                            else
                                result.PhotosFound++;
                        }
                        else
                        {
                            // Keep as Unknown if analysis was unreliable
                            image.ScreenshotStatus = ScreenshotStatus.Unknown;
                            image.IsScreenshot = false;
                            image.ScreenshotConfidence = 0.0;
                            result.ErrorCount++;
                            result.Errors.Add($"{image.FileName}: Unreliable analysis - missing image libraries");
                        }
                    }

                    result.ProcessedImages++;
                    scanProgress.ProcessedImages = result.ProcessedImages;
                    scanProgress.ScreenshotsFound = result.ScreenshotsFound;
                    scanProgress.ErrorCount = result.ErrorCount;
                    scanProgress.CurrentFile = image.FileName;

                    // Throttle progress updates to prevent UI freezing
                    var now = DateTime.UtcNow;
                    if (progress != null && (now - lastProgressUpdate).TotalMilliseconds >= progressUpdateIntervalMs)
                    {
                        progress.Report(scanProgress);
                        lastProgressUpdate = now;
                        
                        // Allow UI to update
                        await Task.Yield();
                    }
                }

                // Save changes for this batch
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving batch changes to database");
                    result.ErrorCount += batchImages.Count;
                    result.Errors.Add($"Database save error: {ex.Message}");
                }
            }

            // Final progress update
            progress?.Report(scanProgress);

            result.Duration = stopwatch.Elapsed;
            
            _logger.LogInformation(
                "Screenshot scan completed: {ProcessedImages}/{TotalImages} processed, {ScreenshotsFound} screenshots, {PhotosFound} photos, {ErrorCount} errors in {Duration:mm\\:ss}",
                result.ProcessedImages, result.TotalImages, result.ScreenshotsFound, result.PhotosFound, result.ErrorCount, result.Duration);

            return result;
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task<(Models.Image image, ScreenshotDetectionResult? result, string? error)> ProcessSingleImageAsync(
        Models.Image image, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(image.FilePath))
            {
                return (image, null, "File not found");
            }

            var result = await _screenshotService.DetectScreenshotAsync(image.FilePath);
            return (image, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing image {FilePath}", image.FilePath);
            return (image, null, ex.Message);
        }
    }

    public async Task<ScreenshotStatistics> GetScreenshotStatisticsAsync()
    {
        var stats = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists)
            .GroupBy(img => img.ScreenshotStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalImages = stats.Sum(s => s.Count);
        var screenshotCount = stats.FirstOrDefault(s => s.Status == ScreenshotStatus.IsScreenshot)?.Count ?? 0;
        var photoCount = stats.FirstOrDefault(s => s.Status == ScreenshotStatus.NotScreenshot)?.Count ?? 0;
        var unknownCount = stats.FirstOrDefault(s => s.Status == ScreenshotStatus.Unknown)?.Count ?? 0;

        return new ScreenshotStatistics
        {
            TotalImages = totalImages,
            ScreenshotCount = screenshotCount,
            PhotoCount = photoCount,
            UnknownCount = unknownCount
        };
    }

    public async Task<List<Models.Image>> GetImagesByScreenshotStatusAsync(ScreenshotStatus status, int skip = 0, int take = 50)
    {
        return await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists && img.ScreenshotStatus == status)
            .OrderBy(img => img.DateCreated)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task UpdateImageScreenshotStatusAsync(int imageId, ScreenshotStatus status, double confidence)
    {
        var image = await _context.Images.FindAsync(imageId);
        if (image != null)
        {
            image.ScreenshotStatus = status;
            image.IsScreenshot = status == ScreenshotStatus.IsScreenshot; // Keep legacy field in sync
            image.ScreenshotConfidence = confidence;
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated image {ImageId} screenshot status to {Status}", imageId, status);
        }
    }

    public void Dispose()
    {
        _processingLock?.Dispose();
    }
}