using FaceVault.Data;
using FaceVault.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaceVault.Services;

public interface IDuplicateCleanupService
{
    /// <summary>
    /// Delete all duplicates except the oldest copy in each group
    /// </summary>
    Task<DuplicateCleanupResult> DeleteAllDuplicatesAsync(IProgress<DuplicateCleanupProgress>? progress = null);
    
    /// <summary>
    /// Delete duplicates in a specific group
    /// </summary>
    Task<DuplicateCleanupResult> DeleteDuplicatesInGroupAsync(string hash);
    
    /// <summary>
    /// Move file to recycle bin (Windows) or trash (Mac/Linux)
    /// </summary>
    Task<bool> MoveToRecycleBinAsync(string filePath);
}


public class DuplicateCleanupService : IDuplicateCleanupService
{
    private readonly FaceVaultDbContext _context;
    private readonly IDuplicateDetectionService _duplicateService;
    private readonly ILogger<DuplicateCleanupService> _logger;

    public DuplicateCleanupService(
        FaceVaultDbContext context,
        IDuplicateDetectionService duplicateService,
        ILogger<DuplicateCleanupService> logger)
    {
        _context = context;
        _duplicateService = duplicateService;
        _logger = logger;
    }

    public async Task<DuplicateCleanupResult> DeleteAllDuplicatesAsync(IProgress<DuplicateCleanupProgress>? progress = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new DuplicateCleanupResult();
        
        try
        {
            // Get all duplicate groups
            var duplicateGroups = await _duplicateService.GetDuplicateGroupsAsync(0, 1000);
            
            var cleanupProgress = new DuplicateCleanupProgress
            {
                TotalGroups = duplicateGroups.Count
            };
            
            _logger.LogInformation("Starting cleanup of {GroupCount} duplicate groups", duplicateGroups.Count);

            foreach (var group in duplicateGroups)
            {
                try
                {
                    // Keep the oldest image (by DateCreated)
                    var keepImage = group.Images.OrderBy(i => i.DateCreated).First();
                    var deleteImages = group.Images.Where(i => i.Id != keepImage.Id).ToList();

                    foreach (var deleteImage in deleteImages)
                    {
                        cleanupProgress.CurrentFile = deleteImage.FileName;
                        progress?.Report(cleanupProgress);

                        var deleted = await DeleteImageFileAndRecord(deleteImage);
                        if (deleted)
                        {
                            result.FilesDeleted++;
                            result.BytesFreed += deleteImage.FileSizeBytes;
                            cleanupProgress.FilesDeleted = result.FilesDeleted;
                            cleanupProgress.BytesFreed = result.BytesFreed;
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"Failed to delete {deleteImage.FileName}");
                        }
                    }

                    result.GroupsProcessed++;
                    cleanupProgress.ProcessedGroups = result.GroupsProcessed;
                    progress?.Report(cleanupProgress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing duplicate group {Hash}", group.Hash);
                    result.ErrorCount++;
                    result.Errors.Add($"Error processing group: {ex.Message}");
                }
            }

            // Save all database changes at once
            await _context.SaveChangesAsync();
            
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Duplicate cleanup completed: {GroupsProcessed} groups, {FilesDeleted} files deleted, {BytesFreed} bytes freed, {ErrorCount} errors in {Duration}",
                result.GroupsProcessed, result.FilesDeleted, result.BytesFreed, result.ErrorCount, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during duplicate cleanup");
            result.ErrorCount++;
            result.Errors.Add($"Cleanup failed: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<DuplicateCleanupResult> DeleteDuplicatesInGroupAsync(string hash)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new DuplicateCleanupResult();

        try
        {
            // Get images in this group
            var groupImages = await _context.Images
                .Where(img => img.FileHash == hash && !img.IsDeleted && img.FileExists)
                .OrderBy(img => img.DateCreated)
                .ToListAsync();

            if (groupImages.Count <= 1)
            {
                _logger.LogWarning("Group {Hash} has {Count} images - no duplicates to delete", hash, groupImages.Count);
                return result;
            }

            // Keep the first (oldest) image
            var keepImage = groupImages.First();
            var deleteImages = groupImages.Skip(1).ToList();

            _logger.LogInformation("Deleting {DeleteCount} duplicates in group {Hash}, keeping {KeepFile}", 
                deleteImages.Count, hash, keepImage.FileName);

            foreach (var deleteImage in deleteImages)
            {
                var deleted = await DeleteImageFileAndRecord(deleteImage);
                if (deleted)
                {
                    result.FilesDeleted++;
                    result.BytesFreed += deleteImage.FileSizeBytes;
                }
                else
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Failed to delete {deleteImage.FileName}");
                }
            }

            result.GroupsProcessed = 1;

            // Save database changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting duplicates in group {Hash}", hash);
            result.ErrorCount++;
            result.Errors.Add($"Failed to delete group: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<bool> DeleteImageFileAndRecord(Image image)
    {
        try
        {
            // Try to delete the physical file
            if (File.Exists(image.FilePath))
            {
                var deleted = await MoveToRecycleBinAsync(image.FilePath);
                if (!deleted)
                {
                    // Fallback to regular delete if recycle bin fails
                    File.Delete(image.FilePath);
                }
            }

            // Update database record
            image.IsDeleted = true;
            image.FileExists = false;
            image.DateDeleted = DateTime.UtcNow;

            _logger.LogDebug("Deleted duplicate file: {FilePath}", image.FilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image file {FilePath}", image.FilePath);
            return false;
        }
    }

    public async Task<bool> MoveToRecycleBinAsync(string filePath)
    {
        try
        {
            // For now, use regular File.Delete
            // In production, you'd want to use platform-specific recycle bin APIs
            await Task.Run(() => File.Delete(filePath));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving file to recycle bin: {FilePath}", filePath);
            return false;
        }
    }
}