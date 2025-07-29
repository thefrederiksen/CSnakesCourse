using FaceVault.Data;
using FaceVault.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FaceVault.Services;

public interface IDuplicateDetectionService
{
    /// <summary>
    /// Scan all images in database and calculate hashes for those missing them
    /// </summary>
    Task<DuplicateScanResult> ScanForDuplicatesAsync(
        IProgress<DuplicateScanProgress>? progress = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get statistics about duplicate detection
    /// </summary>
    Task<DuplicateStatistics> GetDuplicateStatisticsAsync();
    
    /// <summary>
    /// Get groups of duplicate images
    /// </summary>
    Task<List<DuplicateGroup>> GetDuplicateGroupsAsync(int skip = 0, int take = 50);
    
    /// <summary>
    /// Update hash for a specific image
    /// </summary>
    Task UpdateImageHashAsync(int imageId, string hash);
    
    /// <summary>
    /// Check if an image needs hash calculation
    /// </summary>
    Task<bool> NeedsHashCalculationAsync(int imageId);
}


public class DuplicateDetectionService : IDuplicateDetectionService, IDisposable
{
    private readonly FaceVaultDbContext _context;
    private readonly IHashCalculationService _hashService;
    private readonly ILogger<DuplicateDetectionService> _logger;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public DuplicateDetectionService(
        FaceVaultDbContext context,
        IHashCalculationService hashService,
        ILogger<DuplicateDetectionService> logger)
    {
        _context = context;
        _hashService = hashService;
        _logger = logger;
    }

    public async Task<DuplicateScanResult> ScanForDuplicatesAsync(
        IProgress<DuplicateScanProgress>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        await _processingLock.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new DuplicateScanResult();
            
            // Get all images that need hash calculation
            var imagesNeedingHashes = await _context.Images
                .Where(img => !img.IsDeleted && img.FileExists && 
                             (string.IsNullOrEmpty(img.FileHash) || img.FileHash == string.Empty))
                .ToListAsync(cancellationToken);

            result.TotalImages = imagesNeedingHashes.Count;
            
            var scanProgress = new DuplicateScanProgress
            {
                TotalImages = result.TotalImages
            };

            // Calculate total bytes
            foreach (var image in imagesNeedingHashes)
            {
                scanProgress.TotalBytes += image.FileSizeBytes;
            }

            _logger.LogInformation("Starting duplicate scan for {ImageCount} images without hashes", result.TotalImages);

            var lastProgressUpdate = DateTime.UtcNow;
            const int progressUpdateIntervalMs = 200; // Update UI every 200ms

            // Process images in batches to avoid memory issues
            const int batchSize = 25;
            var batches = imagesNeedingHashes.Chunk(batchSize);

            foreach (var batch in batches)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    break;
                }

                // Calculate hashes for this batch
                var filePaths = batch.Where(img => File.Exists(img.FilePath))
                                   .Select(img => img.FilePath)
                                   .ToList();

                if (filePaths.Any())
                {
                    try
                    {
                        var hashProgress = new Progress<HashCalculationProgress>(hashProg =>
                        {
                            scanProgress.CurrentFile = hashProg.CurrentFile;
                        });

                        var hashes = await _hashService.CalculateFileHashesAsync(
                            filePaths, hashProgress, cancellationToken);

                        // Update database with calculated hashes
                        foreach (var image in batch)
                        {
                            if (hashes.TryGetValue(image.FilePath, out var hash))
                            {
                                image.FileHash = hash;
                                result.HashesCalculated++;
                                scanProgress.ProcessedBytes += image.FileSizeBytes;
                            }
                            else if (File.Exists(image.FilePath))
                            {
                                result.Errors.Add($"Failed to calculate hash for {image.FileName}");
                                result.ErrorCount++;
                            }

                            result.ProcessedImages++;
                            scanProgress.ProcessedImages = result.ProcessedImages;
                            scanProgress.CurrentFile = image.FilePath;

                            // Throttle progress updates
                            var now = DateTime.UtcNow;
                            if (progress != null && (now - lastProgressUpdate).TotalMilliseconds >= progressUpdateIntervalMs)
                            {
                                progress.Report(scanProgress);
                                lastProgressUpdate = now;
                                await Task.Yield();
                            }
                        }

                        // Save batch to database
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing batch for duplicate detection");
                        result.Errors.Add($"Batch processing error: {ex.Message}");
                        result.ErrorCount++;
                    }
                }
            }

            // Now find duplicates
            if (!result.WasCancelled)
            {
                var duplicateGroups = await _context.Images
                    .Where(img => !img.IsDeleted && img.FileExists && !string.IsNullOrEmpty(img.FileHash))
                    .GroupBy(img => img.FileHash)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { Hash = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                result.DuplicatesFound = duplicateGroups.Sum(g => g.Count - 1); // All but one in each group
                scanProgress.DuplicatesFound = result.DuplicatesFound;
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Duplicate scan completed: {ProcessedImages}/{TotalImages} processed, {HashesCalculated} hashes calculated, {DuplicatesFound} duplicates found in {Duration}",
                result.ProcessedImages, result.TotalImages, result.HashesCalculated, result.DuplicatesFound, result.Duration);

            return result;
        }
        finally
        {
            _processingLock.Release();
        }
    }

    public async Task<DuplicateStatistics> GetDuplicateStatisticsAsync()
    {
        var stats = new DuplicateStatistics();

        // Get basic counts
        var allImages = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists)
            .Select(img => new { img.FileHash, img.FileSizeBytes })
            .ToListAsync();

        stats.TotalImages = allImages.Count;
        stats.ImagesWithHashes = allImages.Count(img => !string.IsNullOrEmpty(img.FileHash));
        stats.ImagesWithoutHashes = stats.TotalImages - stats.ImagesWithHashes;

        // Find duplicates
        var hashGroups = allImages
            .Where(img => !string.IsNullOrEmpty(img.FileHash))
            .GroupBy(img => img.FileHash)
            .ToList();

        stats.UniqueImages = hashGroups.Count(g => g.Count() == 1);
        stats.DuplicateGroups = hashGroups.Count(g => g.Count() > 1);
        stats.DuplicateImages = hashGroups.Where(g => g.Count() > 1).Sum(g => g.Count() - 1);

        // Calculate wasted space (all duplicates except one per group)
        stats.TotalDuplicateBytes = hashGroups
            .Where(g => g.Count() > 1)
            .Sum(g => g.Skip(1).Sum(img => img.FileSizeBytes));

        return stats;
    }

    public async Task<List<DuplicateGroup>> GetDuplicateGroupsAsync(int skip = 0, int take = 50)
    {
        var duplicateHashes = await _context.Images
            .Where(img => !img.IsDeleted && img.FileExists && !string.IsNullOrEmpty(img.FileHash))
            .GroupBy(img => img.FileHash)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(hash => hash)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var duplicateGroups = new List<DuplicateGroup>();

        foreach (var hash in duplicateHashes)
        {
            var images = await _context.Images
                .Where(img => img.FileHash == hash && !img.IsDeleted && img.FileExists)
                .OrderBy(img => img.DateCreated)
                .ToListAsync();

            duplicateGroups.Add(new DuplicateGroup
            {
                Hash = hash,
                Images = images
            });
        }

        return duplicateGroups;
    }

    public async Task UpdateImageHashAsync(int imageId, string hash)
    {
        var image = await _context.Images.FindAsync(imageId);
        if (image != null)
        {
            image.FileHash = hash;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> NeedsHashCalculationAsync(int imageId)
    {
        var image = await _context.Images
            .Where(img => img.Id == imageId)
            .Select(img => new { img.FileHash, img.IsDeleted, img.FileExists })
            .FirstOrDefaultAsync();

        return image != null && !image.IsDeleted && image.FileExists && 
               (string.IsNullOrEmpty(image.FileHash) || image.FileHash == string.Empty);
    }

    public void Dispose()
    {
        _processingLock?.Dispose();
    }
}