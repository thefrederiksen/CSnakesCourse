using FaceVault.Models;

namespace FaceVault.Models;

public class LibraryReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public OverallStatistics Overall { get; set; } = new();
    public List<YearBreakdown> ImagesByYear { get; set; } = new();
    public List<MonthBreakdown> CurrentYearByMonth { get; set; } = new();
    public DuplicateReportData Duplicates { get; set; } = new();
    public ScreenshotReportData Screenshots { get; set; } = new();
    public List<FileFormatStat> FileFormats { get; set; } = new();
    public StorageStatistics Storage { get; set; } = new();
    public List<Image> LargestFiles { get; set; } = new();
    public RecentActivity Recent { get; set; } = new();
}

public class OverallStatistics
{
    public int TotalImages { get; set; }
    public int TotalPhotos { get; set; }
    public int TotalScreenshots { get; set; }
    public int TotalDuplicates { get; set; }
    public int UniqueImages { get; set; }
    public int ImagesWithHashes { get; set; }
    public int HeicImages { get; set; }
    public int RegularImages { get; set; }
    public DateTime? OldestImage { get; set; }
    public DateTime? NewestImage { get; set; }
    public TimeSpan LibrarySpan => NewestImage.HasValue && OldestImage.HasValue ? 
        NewestImage.Value - OldestImage.Value : TimeSpan.Zero;
}

public class YearBreakdown
{
    public int Year { get; set; }
    public int Count { get; set; }
    public int Photos { get; set; }
    public int Screenshots { get; set; }
    public long TotalBytes { get; set; }
    public string TotalSizeFormatted => FormatBytes(TotalBytes);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class MonthBreakdown
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Photos { get; set; }
    public int Screenshots { get; set; }
}

public class DuplicateReportData
{
    public int TotalGroups { get; set; }
    public int TotalDuplicates { get; set; }
    public long WastedBytes { get; set; }
    public string WastedSizeFormatted => FormatBytes(WastedBytes);
    public List<DuplicateGroup> SampleGroups { get; set; } = new();
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",  
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class ScreenshotReportData
{
    public int TotalScreenshots { get; set; }
    public double PercentageOfLibrary { get; set; }
    public long TotalBytes { get; set; }
    public string TotalSizeFormatted => FormatBytes(TotalBytes);
    public List<Image> SampleScreenshots { get; set; } = new();
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB", 
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class FileFormatStat
{
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public long TotalBytes { get; set; }
    public string TotalSizeFormatted => FormatBytes(TotalBytes);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB", 
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class StorageStatistics
{
    public long TotalBytes { get; set; }
    public long PhotoBytes { get; set; }
    public long ScreenshotBytes { get; set; }
    public long DuplicateBytes { get; set; }
    public long HeicBytes { get; set; }
    public double AverageFileSizeMB { get; set; }
    
    public string TotalSizeFormatted => FormatBytes(TotalBytes);
    public string PhotoSizeFormatted => FormatBytes(PhotoBytes);
    public string ScreenshotSizeFormatted => FormatBytes(ScreenshotBytes);
    public string HeicSizeFormatted => FormatBytes(HeicBytes);
    public string DuplicateSizeFormatted => FormatBytes(DuplicateBytes);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class RecentActivity
{
    public int ImagesLast7Days { get; set; }
    public int ImagesLast30Days { get; set; }
    public int ImagesLast90Days { get; set; }
    public DateTime? LastScanDate { get; set; }
    public List<Image> RecentImages { get; set; } = new();
}