using FaceVault.Models;

namespace FaceVault.Repositories;

public interface IImageRepository : IRepository<Image>
{
    // File-based queries
    Task<Image?> GetByFilePathAsync(string filePath);
    Task<Image?> GetByFileHashAsync(string fileHash);
    Task<IEnumerable<Image>> GetByPerceptualHashAsync(string perceptualHash);
    Task<IEnumerable<Image>> GetDuplicatesByHashAsync(string fileHash);
    Task<IEnumerable<Image>> GetSimilarByPerceptualHashAsync(string perceptualHash, double threshold = 0.95);

    // Processing status queries
    Task<IEnumerable<Image>> GetUnprocessedImagesAsync();
    Task<IEnumerable<Image>> GetProcessedImagesAsync();
    Task<IEnumerable<Image>> GetImagesWithFacesAsync();
    Task<IEnumerable<Image>> GetImagesWithoutFacesAsync();

    // Date-based queries
    Task<IEnumerable<Image>> GetImagesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Image>> GetImagesByYearAsync(int year);
    Task<IEnumerable<Image>> GetImagesByMonthAsync(int year, int month);
    Task<IEnumerable<Image>> GetImagesFromSameDatePreviousYearsAsync(DateTime date);

    // Location-based queries
    Task<IEnumerable<Image>> GetImagesWithLocationAsync();
    Task<IEnumerable<Image>> GetImagesByLocationAsync(double latitude, double longitude, double radiusKm);

    // Camera/EXIF queries
    Task<IEnumerable<Image>> GetImagesByCameraAsync(string make, string? model = null);
    Task<IEnumerable<string>> GetUniqueCameraMakesAsync();
    Task<IEnumerable<string>> GetUniqueCameraModelsAsync(string make);

    // File management
    Task<IEnumerable<Image>> GetDeletedImagesAsync();
    Task<IEnumerable<Image>> GetMissingFilesAsync();
    Task<IEnumerable<Image>> GetScreenshotsAsync();
    Task<int> MarkAsDeletedAsync(int imageId);
    Task<int> RestoreDeletedAsync(int imageId);

    // Statistics
    Task<long> GetTotalFileSizeAsync();
    Task<Dictionary<string, int>> GetImageCountsByYearAsync();
    Task<Dictionary<string, int>> GetImageCountsByCameraAsync();
    Task<Dictionary<string, long>> GetFileSizeStatisticsAsync();

    // Tagging support
    Task<IEnumerable<Image>> GetImagesByTagAsync(int tagId);
    Task<IEnumerable<Image>> GetImagesByTagsAsync(IEnumerable<int> tagIds, bool requireAll = false);
    Task<IEnumerable<Image>> GetUntaggedImagesAsync();

    // Person/Face support
    Task<IEnumerable<Image>> GetImagesByPersonAsync(int personId);
    Task<IEnumerable<Image>> GetImagesByPeopleAsync(IEnumerable<int> personIds, bool requireAll = false);

    // Search and filtering
    Task<IEnumerable<Image>> SearchImagesAsync(string searchTerm);
    Task<IEnumerable<Image>> GetImagesByFiltersAsync(ImageFilter filter);

    // Bulk operations
    Task<int> BulkUpdateProcessingStatusAsync(IEnumerable<int> imageIds, bool isProcessed);
    Task<int> BulkUpdateScreenshotFlagAsync(IEnumerable<int> imageIds, bool isScreenshot);
    Task<int> BulkDeleteAsync(IEnumerable<int> imageIds);
}

// Filter class for complex image queries
public class ImageFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? HasFaces { get; set; }
    public bool? IsProcessed { get; set; }
    public bool? IsScreenshot { get; set; }
    public bool? HasLocation { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public IEnumerable<int>? TagIds { get; set; }
    public IEnumerable<int>? PersonIds { get; set; }
    public long? MinFileSize { get; set; }
    public long? MaxFileSize { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
    public string? SearchTerm { get; set; }
}