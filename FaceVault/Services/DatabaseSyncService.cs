using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Services;

namespace FaceVault.Services;

public interface IDatabaseSyncService
{
    Task SyncStatsAsync();
    Task<int> GetActualImageCountAsync();
    Task ValidateDataConsistencyAsync();
    Task<string> GetDetailedCountReportAsync();
}

public class DatabaseSyncService : IDatabaseSyncService
{
    private readonly FaceVaultDbContext _context;
    private readonly IDatabaseStatsService _statsService;

    public DatabaseSyncService(FaceVaultDbContext context, IDatabaseStatsService statsService)
    {
        _context = context;
        _statsService = statsService;
    }

    public async Task SyncStatsAsync()
    {
        try
        {
            // Force refresh of stats cache
            await _statsService.RefreshStatsAsync();
            Logger.Info("Database stats synchronized");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error syncing database stats: {ex.Message}");
        }
    }

    public async Task<int> GetActualImageCountAsync()
    {
        try
        {
            // Clear any cached entities and get fresh count
            _context.ChangeTracker.Clear();
            
            // Use a new query with NoTracking for accuracy
            var count = await _context.Images
                .AsNoTracking()
                .CountAsync();
                
            Logger.Debug($"Actual image count from database: {count}");
            return count;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting actual image count: {ex.Message}");
            return 0;
        }
    }

    public async Task ValidateDataConsistencyAsync()
    {
        try
        {
            Logger.Info("Starting data consistency validation...");
            
            // Get counts using different methods to check consistency
            var directCount = await GetActualImageCountAsync();
            var statsCount = (await _statsService.GetStatsAsync()).TotalImages;
            
            if (directCount != statsCount)
            {
                Logger.Warning($"Data inconsistency detected: Direct count={directCount}, Stats count={statsCount}");
                
                // Force stats refresh
                await _statsService.RefreshStatsAsync();
                
                var refreshedStatsCount = (await _statsService.GetStatsAsync()).TotalImages;
                Logger.Info($"After refresh: Direct count={directCount}, Refreshed stats count={refreshedStatsCount}");
            }
            else
            {
                Logger.Info($"Data consistency validated: {directCount} images");
            }
            
            // Check for orphaned records
            await CheckOrphanedRecordsAsync();
            
        }
        catch (Exception ex)
        {
            Logger.Error($"Error validating data consistency: {ex.Message}");
        }
    }

    private async Task CheckOrphanedRecordsAsync()
    {
        try
        {
            // Check for faces without images
            var orphanedFaces = await _context.Faces
                .Where(f => !_context.Images.Any(i => i.Id == f.ImageId))
                .CountAsync();
                
            if (orphanedFaces > 0)
            {
                Logger.Warning($"Found {orphanedFaces} orphaned face records");
            }
            
            // Check for image tags without images
            var orphanedImageTags = await _context.ImageTags
                .Where(it => !_context.Images.Any(i => i.Id == it.ImageId))
                .CountAsync();
                
            if (orphanedImageTags > 0)
            {
                Logger.Warning($"Found {orphanedImageTags} orphaned image tag records");
            }
            
            if (orphanedFaces == 0 && orphanedImageTags == 0)
            {
                Logger.Info("No orphaned records found");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error checking orphaned records: {ex.Message}");
        }
    }

    public async Task<string> GetDetailedCountReportAsync()
    {
        try
        {
            _context.ChangeTracker.Clear();
            
            var directCount = await _context.Images.AsNoTracking().CountAsync();
            var statsCount = (await _statsService.GetStatsAsync()).TotalImages;
            
            // Get count by different methods for comparison
            var allImagesCount = await _context.Images.AsNoTracking().CountAsync();
            var processedCount = await _context.Images.AsNoTracking().CountAsync(i => i.IsProcessed);
            var nonDeletedCount = await _context.Images.AsNoTracking().CountAsync(i => !i.IsDeleted);
            
            var report = $@"
=== Database Count Analysis ===
Direct Count (AsNoTracking): {directCount}
Stats Service Count: {statsCount}
All Images: {allImagesCount}
Processed Images: {processedCount}
Non-Deleted Images: {nonDeletedCount}
=== End Report ===";
            
            Logger.Info(report);
            return report;
        }
        catch (Exception ex)
        {
            var errorReport = $"Error generating count report: {ex.Message}";
            Logger.Error(errorReport);
            return errorReport;
        }
    }
}