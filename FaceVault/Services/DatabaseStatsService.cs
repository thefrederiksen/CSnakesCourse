using Microsoft.EntityFrameworkCore;
using FaceVault.Data;

namespace FaceVault.Services;

public interface IDatabaseStatsService
{
    Task<DatabaseStats> GetStatsAsync();
    Task RefreshStatsAsync();
}

public class DatabaseStatsService : IDatabaseStatsService
{
    private readonly FaceVaultDbContext _context;
    private DatabaseStats? _cachedStats;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(5);

    public DatabaseStatsService(FaceVaultDbContext context)
    {
        _context = context;
    }

    public async Task<DatabaseStats> GetStatsAsync()
    {
        // TEMPORARY: Always refresh for debugging - bypass cache
        Logger.Debug("DatabaseStatsService: GetStatsAsync called - bypassing cache for debugging");
        return await RefreshAndGetStatsAsync();
        
        // Use cached stats if still valid
        //if (_cachedStats != null && DateTime.UtcNow - _lastCacheTime < _cacheExpiry)
        //{
        //    return _cachedStats;
        //}

        //return await RefreshAndGetStatsAsync();
    }

    public async Task RefreshStatsAsync()
    {
        await RefreshAndGetStatsAsync();
    }

    private async Task<DatabaseStats> RefreshAndGetStatsAsync()
    {
        try
        {
            Logger.Debug("DatabaseStatsService: Starting RefreshAndGetStatsAsync");
            
            // Clear any tracked entities to ensure fresh data
            _context.ChangeTracker.Clear();
            Logger.Debug("DatabaseStatsService: ChangeTracker cleared");

            var stats = new DatabaseStats();

            // Get counts using direct queries with AsNoTracking for accuracy
            // CRITICAL DEBUGGING: Check which database file this context is using
            var statsDbConnectionString = _context.Database.GetConnectionString();
            Logger.Debug($"DatabaseStatsService: STATS DbContext connection = {statsDbConnectionString}");
            
            Logger.Debug("DatabaseStatsService: Getting TotalImages count...");
            stats.TotalImages = await _context.Images.AsNoTracking().CountAsync();
            Logger.Debug($"DatabaseStatsService: TotalImages = {stats.TotalImages}");
            
            stats.ProcessedImages = await _context.Images.AsNoTracking().CountAsync(i => i.IsProcessed);
            Logger.Debug($"DatabaseStatsService: ProcessedImages = {stats.ProcessedImages}");
            
            stats.ImagesWithFaces = await _context.Images.AsNoTracking().CountAsync(i => i.HasFaces);
            stats.Screenshots = await _context.Images.AsNoTracking().CountAsync(i => i.IsScreenshot);
            stats.TotalPeople = await _context.People.AsNoTracking().CountAsync();
            stats.TotalFaces = await _context.Faces.AsNoTracking().CountAsync();
            stats.TotalTags = await _context.Tags.AsNoTracking().CountAsync();
            stats.TotalImageTags = await _context.ImageTags.AsNoTracking().CountAsync();
            stats.TotalSettings = await _context.AppSettings.AsNoTracking().CountAsync();

            // Calculate additional stats
            stats.UnprocessedImages = stats.TotalImages - stats.ProcessedImages;
            stats.ImagesWithoutFaces = stats.TotalImages - stats.ImagesWithFaces;

            _cachedStats = stats;
            _lastCacheTime = DateTime.UtcNow;

            Logger.Debug($"Database stats refreshed: {stats.TotalImages} images, {stats.TotalPeople} people, {stats.TotalFaces} faces");

            return stats;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting database stats: {ex.Message}");
            Logger.Error($"Exception Type: {ex.GetType().Name}");
            Logger.Error($"Exception Details: {ex}");
            Logger.Error($"Inner Exception: {ex.InnerException?.Message ?? "None"}");
            
            // Log specific database connection errors
            if (ex.Message.Contains("database is locked"))
            {
                Logger.Error("Database is locked while getting stats. Multiple processes may be accessing the database.");
            }
            else if (ex.Message.Contains("no such table"))
            {
                Logger.Error("Database table missing while getting stats. Database may not be properly initialized.");
            }
            else if (ex.Message.Contains("database disk image is malformed"))
            {
                Logger.Error("Database file is corrupted while getting stats.");
            }
            
            // Return empty stats on error
            return new DatabaseStats
            {
                Error = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
    }
}

public class DatabaseStats
{
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int UnprocessedImages { get; set; }
    public int ImagesWithFaces { get; set; }
    public int ImagesWithoutFaces { get; set; }
    public int Screenshots { get; set; }
    public int TotalPeople { get; set; }
    public int TotalFaces { get; set; }
    public int TotalTags { get; set; }
    public int TotalImageTags { get; set; }
    public int TotalSettings { get; set; }
    public string? Error { get; set; }

    public bool HasData => TotalImages > 0 || TotalPeople > 0 || TotalFaces > 0;
    public bool IsHealthy => string.IsNullOrEmpty(Error);
}