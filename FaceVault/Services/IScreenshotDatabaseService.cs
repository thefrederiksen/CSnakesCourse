namespace FaceVault.Services;

public interface IScreenshotDatabaseService
{
    /// <summary>
    /// Analyze all images in the database for screenshots and update their classification
    /// </summary>
    Task<ScreenshotScanResult> ScanAllImagesAsync(IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyze specific images for screenshots
    /// </summary>
    Task<ScreenshotScanResult> ScanImagesAsync(IEnumerable<int> imageIds, IProgress<ScreenshotScanProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get screenshot statistics from the database
    /// </summary>
    Task<ScreenshotStatistics> GetScreenshotStatisticsAsync();
    
    /// <summary>
    /// Get images filtered by screenshot status
    /// </summary>
    Task<List<Models.Image>> GetImagesByScreenshotStatusAsync(bool isScreenshot, int skip = 0, int take = 50);
    
    /// <summary>
    /// Update screenshot classification for a specific image
    /// </summary>
    Task UpdateImageScreenshotStatusAsync(int imageId, bool isScreenshot, double confidence);
}

public class ScreenshotScanResult
{
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int ScreenshotsFound { get; set; }
    public int PhotosFound { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool WasCancelled { get; set; }
}

public class ScreenshotScanProgress
{
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int ScreenshotsFound { get; set; }
    public int ErrorCount { get; set; }
    public string? CurrentFile { get; set; }
    public double ProgressPercentage => TotalImages > 0 ? (double)ProcessedImages / TotalImages * 100 : 0;
}

public class ScreenshotStatistics
{
    public int TotalImages { get; set; }
    public int ScreenshotCount { get; set; }
    public int PhotoCount { get; set; }
    public int UnprocessedCount { get; set; }
    public double ScreenshotPercentage => TotalImages > 0 ? (double)ScreenshotCount / TotalImages * 100 : 0;
    public DateTime? LastScanDate { get; set; }
}