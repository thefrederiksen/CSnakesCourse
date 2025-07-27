using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Models;

namespace FaceVault.Repositories;

public class ImageRepository : Repository<Image>, IImageRepository
{
    public ImageRepository(FaceVaultDbContext context) : base(context)
    {
    }

    public async Task<Image?> GetByFilePathAsync(string filePath)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.FilePath == filePath);
    }

    public async Task<Image?> GetByFileHashAsync(string fileHash)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.FileHash == fileHash);
    }

    public async Task<IEnumerable<Image>> GetByPerceptualHashAsync(string perceptualHash)
    {
        return await _dbSet.Where(i => i.PerceptualHash == perceptualHash).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetDuplicatesByHashAsync(string fileHash)
    {
        return await _dbSet.Where(i => i.FileHash == fileHash).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetSimilarByPerceptualHashAsync(string perceptualHash, double threshold = 0.95)
    {
        // Note: This is a simplified implementation. In practice, you'd want to use
        // a more sophisticated similarity algorithm for perceptual hashes
        return await _dbSet.Where(i => i.PerceptualHash == perceptualHash).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetUnprocessedImagesAsync()
    {
        return await _dbSet.Where(i => !i.IsProcessed && !i.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetProcessedImagesAsync()
    {
        return await _dbSet.Where(i => i.IsProcessed && !i.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesWithFacesAsync()
    {
        return await _dbSet.Where(i => i.HasFaces && !i.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesWithoutFacesAsync()
    {
        return await _dbSet.Where(i => !i.HasFaces && i.IsProcessed && !i.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.Where(i => 
            !i.IsDeleted && 
            ((i.DateTaken.HasValue && i.DateTaken >= startDate && i.DateTaken <= endDate) ||
             (!i.DateTaken.HasValue && i.DateCreated >= startDate && i.DateCreated <= endDate))
        ).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByYearAsync(int year)
    {
        return await _dbSet.Where(i => 
            !i.IsDeleted && 
            ((i.DateTaken.HasValue && i.DateTaken.Value.Year == year) ||
             (!i.DateTaken.HasValue && i.DateCreated.Year == year))
        ).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByMonthAsync(int year, int month)
    {
        return await _dbSet.Where(i => 
            !i.IsDeleted && 
            ((i.DateTaken.HasValue && i.DateTaken.Value.Year == year && i.DateTaken.Value.Month == month) ||
             (!i.DateTaken.HasValue && i.DateCreated.Year == year && i.DateCreated.Month == month))
        ).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesFromSameDatePreviousYearsAsync(DateTime date)
    {
        var dayMonth = new { Day = date.Day, Month = date.Month };
        
        return await _dbSet.Where(i => 
            !i.IsDeleted && 
            ((i.DateTaken.HasValue && 
              i.DateTaken.Value.Day == dayMonth.Day && 
              i.DateTaken.Value.Month == dayMonth.Month && 
              i.DateTaken.Value.Year < date.Year) ||
             (!i.DateTaken.HasValue && 
              i.DateCreated.Day == dayMonth.Day && 
              i.DateCreated.Month == dayMonth.Month && 
              i.DateCreated.Year < date.Year))
        ).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesWithLocationAsync()
    {
        return await _dbSet.Where(i => !i.IsDeleted && i.Latitude.HasValue && i.Longitude.HasValue).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByLocationAsync(double latitude, double longitude, double radiusKm)
    {
        // Simple distance calculation - in production, you'd want a more accurate geo-spatial query
        var latRange = radiusKm / 111.0; // Rough conversion: 1 degree ≈ 111 km
        var lonRange = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180));

        return await _dbSet.Where(i => 
            !i.IsDeleted && 
            i.Latitude.HasValue && i.Longitude.HasValue &&
            Math.Abs(i.Latitude.Value - latitude) <= latRange &&
            Math.Abs(i.Longitude.Value - longitude) <= lonRange
        ).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByCameraAsync(string make, string? model = null)
    {
        var query = _dbSet.Where(i => !i.IsDeleted && i.CameraMake == make);
        
        if (!string.IsNullOrEmpty(model))
        {
            query = query.Where(i => i.CameraModel == model);
        }
        
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<string>> GetUniqueCameraMakesAsync()
    {
        return await _dbSet
            .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.CameraMake))
            .Select(i => i.CameraMake!)
            .Distinct()
            .OrderBy(make => make)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetUniqueCameraModelsAsync(string make)
    {
        return await _dbSet
            .Where(i => !i.IsDeleted && i.CameraMake == make && !string.IsNullOrEmpty(i.CameraModel))
            .Select(i => i.CameraModel!)
            .Distinct()
            .OrderBy(model => model)
            .ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetDeletedImagesAsync()
    {
        return await _dbSet.Where(i => i.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetMissingFilesAsync()
    {
        return await _dbSet.Where(i => !i.IsDeleted && !i.FileExists).ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetScreenshotsAsync()
    {
        return await _dbSet.Where(i => !i.IsDeleted && i.IsScreenshot).ToListAsync();
    }

    public async Task<int> MarkAsDeletedAsync(int imageId)
    {
        var image = await GetByIdAsync(imageId);
        if (image == null) return 0;

        image.IsDeleted = true;
        image.DateDeleted = DateTime.UtcNow;
        
        return await SaveChangesAsync();
    }

    public async Task<int> RestoreDeletedAsync(int imageId)
    {
        var image = await GetByIdAsync(imageId);
        if (image == null) return 0;

        image.IsDeleted = false;
        image.DateDeleted = null;
        
        return await SaveChangesAsync();
    }

    public async Task<long> GetTotalFileSizeAsync()
    {
        return await _dbSet.Where(i => !i.IsDeleted).SumAsync(i => i.FileSizeBytes);
    }

    public async Task<Dictionary<string, int>> GetImageCountsByYearAsync()
    {
        var counts = await _dbSet
            .Where(i => !i.IsDeleted)
            .GroupBy(i => (i.DateTaken ?? i.DateCreated).Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Year.ToString(), x => x.Count);

        return counts;
    }

    public async Task<Dictionary<string, int>> GetImageCountsByCameraAsync()
    {
        var counts = await _dbSet
            .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.CameraMake))
            .GroupBy(i => i.CameraMake!)
            .Select(g => new { Make = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Make, x => x.Count);

        return counts;
    }

    public async Task<Dictionary<string, long>> GetFileSizeStatisticsAsync()
    {
        var stats = new Dictionary<string, long>();
        
        if (await _dbSet.AnyAsync(i => !i.IsDeleted))
        {
            stats["Total"] = await _dbSet.Where(i => !i.IsDeleted).SumAsync(i => i.FileSizeBytes);
            stats["Average"] = (long)await _dbSet.Where(i => !i.IsDeleted).AverageAsync(i => i.FileSizeBytes);
            stats["Minimum"] = await _dbSet.Where(i => !i.IsDeleted).MinAsync(i => i.FileSizeBytes);
            stats["Maximum"] = await _dbSet.Where(i => !i.IsDeleted).MaxAsync(i => i.FileSizeBytes);
        }

        return stats;
    }

    public async Task<IEnumerable<Image>> GetImagesByTagAsync(int tagId)
    {
        return await _dbSet
            .Where(i => !i.IsDeleted && i.ImageTags.Any(it => it.TagId == tagId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByTagsAsync(IEnumerable<int> tagIds, bool requireAll = false)
    {
        var tagIdsList = tagIds.ToList();
        
        if (requireAll)
        {
            return await _dbSet
                .Where(i => !i.IsDeleted && tagIdsList.All(tagId => i.ImageTags.Any(it => it.TagId == tagId)))
                .ToListAsync();
        }
        else
        {
            return await _dbSet
                .Where(i => !i.IsDeleted && i.ImageTags.Any(it => tagIdsList.Contains(it.TagId)))
                .ToListAsync();
        }
    }

    public async Task<IEnumerable<Image>> GetUntaggedImagesAsync()
    {
        return await _dbSet
            .Where(i => !i.IsDeleted && !i.ImageTags.Any())
            .ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByPersonAsync(int personId)
    {
        return await _dbSet
            .Where(i => !i.IsDeleted && i.Faces.Any(f => f.PersonId == personId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByPeopleAsync(IEnumerable<int> personIds, bool requireAll = false)
    {
        var personIdsList = personIds.ToList();
        
        if (requireAll)
        {
            return await _dbSet
                .Where(i => !i.IsDeleted && personIdsList.All(personId => i.Faces.Any(f => f.PersonId == personId)))
                .ToListAsync();
        }
        else
        {
            return await _dbSet
                .Where(i => !i.IsDeleted && i.Faces.Any(f => personIdsList.Contains(f.PersonId ?? 0)))
                .ToListAsync();
        }
    }

    public async Task<IEnumerable<Image>> SearchImagesAsync(string searchTerm)
    {
        return await _dbSet
            .Where(i => !i.IsDeleted && 
                (i.FileName.Contains(searchTerm) ||
                 i.FilePath.Contains(searchTerm) ||
                 (i.LocationName != null && i.LocationName.Contains(searchTerm)) ||
                 (i.CameraMake != null && i.CameraMake.Contains(searchTerm)) ||
                 (i.CameraModel != null && i.CameraModel.Contains(searchTerm))))
            .ToListAsync();
    }

    public async Task<IEnumerable<Image>> GetImagesByFiltersAsync(ImageFilter filter)
    {
        var query = _dbSet.Where(i => !i.IsDeleted);

        if (filter.StartDate.HasValue)
        {
            query = query.Where(i => (i.DateTaken ?? i.DateCreated) >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(i => (i.DateTaken ?? i.DateCreated) <= filter.EndDate.Value);
        }

        if (filter.HasFaces.HasValue)
        {
            query = query.Where(i => i.HasFaces == filter.HasFaces.Value);
        }

        if (filter.IsProcessed.HasValue)
        {
            query = query.Where(i => i.IsProcessed == filter.IsProcessed.Value);
        }

        if (filter.IsScreenshot.HasValue)
        {
            query = query.Where(i => i.IsScreenshot == filter.IsScreenshot.Value);
        }

        if (filter.HasLocation.HasValue)
        {
            if (filter.HasLocation.Value)
            {
                query = query.Where(i => i.Latitude.HasValue && i.Longitude.HasValue);
            }
            else
            {
                query = query.Where(i => !i.Latitude.HasValue || !i.Longitude.HasValue);
            }
        }

        if (!string.IsNullOrEmpty(filter.CameraMake))
        {
            query = query.Where(i => i.CameraMake == filter.CameraMake);
        }

        if (!string.IsNullOrEmpty(filter.CameraModel))
        {
            query = query.Where(i => i.CameraModel == filter.CameraModel);
        }

        if (filter.MinFileSize.HasValue)
        {
            query = query.Where(i => i.FileSizeBytes >= filter.MinFileSize.Value);
        }

        if (filter.MaxFileSize.HasValue)
        {
            query = query.Where(i => i.FileSizeBytes <= filter.MaxFileSize.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(i => 
                i.FileName.Contains(filter.SearchTerm) ||
                i.FilePath.Contains(filter.SearchTerm) ||
                (i.LocationName != null && i.LocationName.Contains(filter.SearchTerm)));
        }

        return await query.ToListAsync();
    }

    public async Task<int> BulkUpdateProcessingStatusAsync(IEnumerable<int> imageIds, bool isProcessed)
    {
        var images = await _dbSet.Where(i => imageIds.Contains(i.Id)).ToListAsync();
        
        foreach (var image in images)
        {
            image.IsProcessed = isProcessed;
            if (isProcessed)
            {
                image.LastProcessed = DateTime.UtcNow;
            }
        }

        return await SaveChangesAsync();
    }

    public async Task<int> BulkUpdateScreenshotFlagAsync(IEnumerable<int> imageIds, bool isScreenshot)
    {
        var images = await _dbSet.Where(i => imageIds.Contains(i.Id)).ToListAsync();
        
        foreach (var image in images)
        {
            image.IsScreenshot = isScreenshot;
        }

        return await SaveChangesAsync();
    }

    public async Task<int> BulkDeleteAsync(IEnumerable<int> imageIds)
    {
        var images = await _dbSet.Where(i => imageIds.Contains(i.Id)).ToListAsync();
        
        foreach (var image in images)
        {
            image.IsDeleted = true;
            image.DateDeleted = DateTime.UtcNow;
        }

        return await SaveChangesAsync();
    }
}