using FaceVault.Data;
using FaceVault.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FaceVault.Services;

public interface ILibraryReportService
{
    Task<LibraryReport> GenerateReportAsync();
}

public class LibraryReportService : ILibraryReportService
{
    private readonly FaceVaultDbContext _context;
    private readonly IDuplicateDetectionService _duplicateService;
    private readonly ILogger<LibraryReportService> _logger;

    public LibraryReportService(
        FaceVaultDbContext context,
        IDuplicateDetectionService duplicateService,
        ILogger<LibraryReportService> logger)
    {
        _context = context;
        _duplicateService = duplicateService;
        _logger = logger;
    }

    public async Task<LibraryReport> GenerateReportAsync()
    {
        _logger.LogInformation("Generating library report...");

        var report = new LibraryReport
        {
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            // Get all images for analysis
            var allImages = await _context.Images
                .Where(img => !img.IsDeleted && img.FileExists)
                .ToListAsync();

            // Overall statistics
            report.Overall = await GenerateOverallStatistics(allImages);

            // Images by year
            report.ImagesByYear = await GenerateYearBreakdown(allImages);

            // Current year by month
            report.CurrentYearByMonth = await GenerateCurrentYearMonthBreakdown(allImages);

            // Duplicate statistics
            report.Duplicates = await GenerateDuplicateStatistics();

            // Screenshot statistics
            report.Screenshots = await GenerateScreenshotStatistics(allImages);

            // File format statistics
            report.FileFormats = GenerateFileFormatStatistics(allImages);

            // Storage statistics
            report.Storage = GenerateStorageStatistics(allImages);

            // Largest files
            report.LargestFiles = allImages
                .OrderByDescending(img => img.FileSizeBytes)
                .Take(10)
                .ToList();

            // Recent activity
            report.Recent = GenerateRecentActivity(allImages);

            _logger.LogInformation("Library report generated successfully with {TotalImages} images", report.Overall.TotalImages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating library report");
            throw;
        }

        return report;
    }

    private async Task<OverallStatistics> GenerateOverallStatistics(List<Image> allImages)
    {
        var stats = new OverallStatistics
        {
            TotalImages = allImages.Count,
            TotalPhotos = allImages.Count(img => !img.IsScreenshot),
            TotalScreenshots = allImages.Count(img => img.IsScreenshot),
            HeicImages = allImages.Count(img => img.FileExtension.ToLower() is ".heic" or ".heif"),
            ImagesWithHashes = allImages.Count(img => !string.IsNullOrEmpty(img.FileHash))
        };

        stats.RegularImages = stats.TotalImages - stats.HeicImages;

        // Get date range
        if (allImages.Any())
        {
            stats.OldestImage = allImages
                .Where(img => img.DateTaken.HasValue)
                .MinBy(img => img.DateTaken)?.DateTaken ?? 
                allImages.MinBy(img => img.DateCreated)?.DateCreated;

            stats.NewestImage = allImages
                .Where(img => img.DateTaken.HasValue)
                .MaxBy(img => img.DateTaken)?.DateTaken ?? 
                allImages.MaxBy(img => img.DateCreated)?.DateCreated;
        }

        // Get duplicate count
        var duplicateStats = await _duplicateService.GetDuplicateStatisticsAsync();
        stats.TotalDuplicates = duplicateStats.DuplicateImages;
        stats.UniqueImages = duplicateStats.UniqueImages;

        return stats;
    }

    private Task<List<YearBreakdown>> GenerateYearBreakdown(List<Image> allImages)
    {
        var yearStats = allImages
            .GroupBy(img => (img.DateTaken ?? img.DateCreated).Year)
            .Select(g => new YearBreakdown
            {
                Year = g.Key,
                Count = g.Count(),
                Photos = g.Count(img => !img.IsScreenshot),
                Screenshots = g.Count(img => img.IsScreenshot),
                TotalBytes = g.Sum(img => img.FileSizeBytes)
            })
            .OrderByDescending(y => y.Year)
            .ToList();

        return Task.FromResult(yearStats);
    }

    private Task<List<MonthBreakdown>> GenerateCurrentYearMonthBreakdown(List<Image> allImages)
    {
        var currentYear = DateTime.Now.Year;
        var monthStats = allImages
            .Where(img => (img.DateTaken ?? img.DateCreated).Year == currentYear)
            .GroupBy(img => (img.DateTaken ?? img.DateCreated).Month)
            .Select(g => new MonthBreakdown
            {
                Month = g.Key,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                Count = g.Count(),
                Photos = g.Count(img => !img.IsScreenshot),
                Screenshots = g.Count(img => img.IsScreenshot)
            })
            .OrderBy(m => m.Month)
            .ToList();

        return Task.FromResult(monthStats);
    }

    private async Task<DuplicateReportData> GenerateDuplicateStatistics()
    {
        var duplicateStats = await _duplicateService.GetDuplicateStatisticsAsync();
        var sampleGroups = await _duplicateService.GetDuplicateGroupsAsync(0, 3);

        return new DuplicateReportData
        {
            TotalGroups = duplicateStats.DuplicateGroups,
            TotalDuplicates = duplicateStats.DuplicateImages,
            WastedBytes = duplicateStats.TotalDuplicateBytes,
            SampleGroups = sampleGroups
        };
    }

    private Task<ScreenshotReportData> GenerateScreenshotStatistics(List<Image> allImages)
    {
        var screenshots = allImages.Where(img => img.IsScreenshot).ToList();
        var sampleScreenshots = screenshots.Take(3).ToList();

        var data = new ScreenshotReportData
        {
            TotalScreenshots = screenshots.Count,
            PercentageOfLibrary = allImages.Count > 0 ? (double)screenshots.Count / allImages.Count * 100 : 0,
            TotalBytes = screenshots.Sum(img => img.FileSizeBytes),
            SampleScreenshots = sampleScreenshots
        };

        return Task.FromResult(data);
    }

    private List<FileFormatStat> GenerateFileFormatStatistics(List<Image> allImages)
    {
        var totalImages = allImages.Count;
        
        return allImages
            .GroupBy(img => img.FileExtension.ToLower())
            .Select(g => new FileFormatStat
            {
                Extension = g.Key,
                Count = g.Count(),
                Percentage = totalImages > 0 ? (double)g.Count() / totalImages * 100 : 0,
                TotalBytes = g.Sum(img => img.FileSizeBytes)
            })
            .OrderByDescending(f => f.Count)
            .ToList();
    }

    private StorageStatistics GenerateStorageStatistics(List<Image> allImages)
    {
        var photos = allImages.Where(img => !img.IsScreenshot).ToList();
        var screenshots = allImages.Where(img => img.IsScreenshot).ToList();
        var heicImages = allImages.Where(img => img.FileExtension.ToLower() is ".heic" or ".heif").ToList();

        return new StorageStatistics
        {
            TotalBytes = allImages.Sum(img => img.FileSizeBytes),
            PhotoBytes = photos.Sum(img => img.FileSizeBytes),
            ScreenshotBytes = screenshots.Sum(img => img.FileSizeBytes),
            HeicBytes = heicImages.Sum(img => img.FileSizeBytes),
            AverageFileSizeMB = allImages.Count > 0 ? 
                allImages.Average(img => img.FileSizeBytes) / (1024.0 * 1024.0) : 0
        };
    }

    private RecentActivity GenerateRecentActivity(List<Image> allImages)
    {
        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);
        var last90Days = now.AddDays(-90);

        return new RecentActivity
        {
            ImagesLast7Days = allImages.Count(img => img.DateCreated >= last7Days),
            ImagesLast30Days = allImages.Count(img => img.DateCreated >= last30Days),
            ImagesLast90Days = allImages.Count(img => img.DateCreated >= last90Days),
            RecentImages = allImages
                .OrderByDescending(img => img.DateCreated)
                .Take(5)
                .ToList()
        };
    }
}