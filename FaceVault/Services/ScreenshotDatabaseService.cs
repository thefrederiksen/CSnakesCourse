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

    public async Task<ScreenshotScanResult> ScanAllImagesAsync(IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        // Get all image IDs to avoid loading full entities into memory
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
            
            var lastProgressUpdate = DateTime.UtcNow;
            const int progressUpdateIntervalMs = 100; // Update UI at most every 100ms

            _logger.LogInformation("Starting screenshot detection scan for {ImageCount} images", imageIdList.Count);

            // Process images in batches to avoid memory issues
            const int batchSize = 50;
            var batches = imageIdList.Chunk(batchSize);

            foreach (var batch in batches)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    break;
                }

                // Load batch of images
                var images = await _context.Images
                    .Where(img => batch.Contains(img.Id))
                    .ToListAsync(cancellationToken);

                // Process each image in the batch
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
                var batchTasks = images.Select(async image =>
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
                        image.IsScreenshot = detectionResult.IsScreenshot;
                        image.ScreenshotConfidence = detectionResult.Confidence;
                        
                        if (detectionResult.IsScreenshot)
                            result.ScreenshotsFound++;
                        else
                            result.PhotosFound++;
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
                    _logger.LogError(ex, "Error saving screenshot detection results for batch");
                    result.Errors.Add($"Database save error: {ex.Message}");
                    result.ErrorCount++;
                }
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Screenshot detection scan completed: {ProcessedImages}/{TotalImages} processed, {ScreenshotsFound} screenshots, {PhotosFound} photos, {ErrorCount} errors in {Duration}",
                result.ProcessedImages, result.TotalImages, result.ScreenshotsFound, result.PhotosFound, result.ErrorCount, result.Duration);

            return result;
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task<(Image image, ScreenshotDetectionResult? result, string? error)> ProcessSingleImageAsync(Image image, CancellationToken cancellationToken)
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
            _logger.LogWarning(ex, "Error processing image {FileName} for screenshot detection", image.FileName);
            return (image, null, ex.Message);
        }
    }

    public async Task<ScreenshotStatistics> GetScreenshotStatisticsAsync()
    {
        var stats = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists)
            .GroupBy(img => 1)
            .Select(g => new ScreenshotStatistics
            {
                TotalImages = g.Count(),
                ScreenshotCount = g.Count(img => img.IsScreenshot),
                PhotoCount = g.Count(img => !img.IsScreenshot),
                UnprocessedCount = g.Count(img => img.ScreenshotConfidence == 0.0)
            })
            .FirstOrDefaultAsync();

        return stats ?? new ScreenshotStatistics();
    }

    public async Task<List<Image>> GetImagesByScreenshotStatusAsync(bool isScreenshot, int skip = 0, int take = 50)
    {
        return await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists && img.IsScreenshot == isScreenshot)
            .OrderByDescending(img => img.DateTaken ?? img.DateCreated)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task UpdateImageScreenshotStatusAsync(int imageId, bool isScreenshot, double confidence)
    {
        var image = await _context.Images.FindAsync(imageId);
        if (image != null)
        {
            image.IsScreenshot = isScreenshot;
            image.ScreenshotConfidence = confidence;
            await _context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _processingLock?.Dispose();
    }
}