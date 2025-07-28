using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Models;

namespace FaceVault.Services;

public class MemoryService : IMemoryService
{
    private readonly FaceVaultDbContext _context;

    public MemoryService(FaceVaultDbContext context)
    {
        _context = context;
    }

    public async Task<MemoryCollection> GetTodaysMemoriesAsync(DateTime date, bool excludeScreenshots = false)
    {
        var yearGroups = await GetPhotosByDateAsync(date, excludeScreenshots);
        var totalPhotos = yearGroups.Sum(g => g.PhotoCount);

        return new MemoryCollection
        {
            Date = date,
            YearGroups = yearGroups,
            TotalPhotos = totalPhotos
        };
    }

    public async Task<List<YearGroup>> GetPhotosByDateAsync(DateTime date, bool excludeScreenshots = false)
    {
        try
        {
            // Get photos from this date across all years
            var query = _context.Images
                .AsNoTracking()
                .Where(img => img.DateTaken.HasValue && 
                             img.DateTaken.Value.Month == date.Month && 
                             img.DateTaken.Value.Day == date.Day);
            
            // Add screenshot filter if requested
            if (excludeScreenshots)
            {
                // Filter by both the legacy IsScreenshot field AND the new ScreenshotStatus enum
                // Also exclude Unknown status images since we don't know if they're screenshots yet
                query = query.Where(img => !img.IsScreenshot && 
                                         img.ScreenshotStatus != ScreenshotStatus.IsScreenshot &&
                                         img.ScreenshotStatus != ScreenshotStatus.Unknown);
            }
            
            var photos = await query
                .OrderByDescending(img => img.DateTaken)
                .Take(200) // Limit to prevent performance issues
                .ToListAsync();

            // Group by year and create year groups
            var yearGroups = photos
                .GroupBy(img => img.DateTaken!.Value.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new YearGroup
                {
                    Year = g.Key,
                    Photos = g.OrderByDescending(img => img.DateTaken).ToList()
                })
                .ToList();

            Logger.Info($"Found {photos.Count} photos for {date:MMMM d} across {yearGroups.Count} years");
            return yearGroups;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching photos for date {date:MMMM d}: {ex.Message}");
            return new List<YearGroup>();
        }
    }

    public async Task<int> GetTotalPhotosForDateAsync(DateTime date)
    {
        try
        {
            return await _context.Images
                .AsNoTracking()
                .CountAsync(img => img.DateTaken.HasValue && 
                           img.DateTaken.Value.Month == date.Month && 
                           img.DateTaken.Value.Day == date.Day);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error counting photos for date {date:MMMM d}: {ex.Message}");
            return 0;
        }
    }
}