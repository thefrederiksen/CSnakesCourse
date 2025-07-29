using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceVault.Models;

public class AppSettings
{
    [Key]
    public int Id { get; set; } = 1; // Always use ID 1 for singleton settings

    // Feature Toggles (from PRD Setup Wizard)
    public bool EnableOnThisDay { get; set; } = true;
    public bool EnableFaceRecognition { get; set; } = true;
    public bool EnableScreenshotFiltering { get; set; } = true;
    public bool EnableDuplicateDetection { get; set; } = true;
    public bool EnableCollageGeneration { get; set; } = true;
    public bool EnableLLMQuery { get; set; } = true;

    // Directory Settings
    [Required]
    [MaxLength(500)]
    public string PhotoDirectory { get; set; } = GetDefaultPicturesDirectory();

    public bool AutoScanOnStartup { get; set; } = false;
    public bool WatchDirectoryForChanges { get; set; } = true;
    public bool ScanSubdirectories { get; set; } = true;

    // File Type Support
    public bool SupportJpeg { get; set; } = true;
    public bool SupportPng { get; set; } = true;
    public bool SupportBmp { get; set; } = true;
    public bool SupportTiff { get; set; } = true;
    public bool SupportWebp { get; set; } = true;
    public bool SupportHeic { get; set; } = true;
    public bool SupportGif { get; set; } = false; // Only static GIFs
    public bool SupportRaw { get; set; } = false; // RAW formats

    // Performance Settings
    public int BatchSize { get; set; } = 100; // Photos per batch
    public int MaxMemoryUsageMB { get; set; } = 2048; // 2GB default
    public int MaxConcurrentTasks { get; set; } = Environment.ProcessorCount;
    public int ThumbnailSize { get; set; } = 256;
    public int PreviewSize { get; set; } = 1024;

    // Face Recognition Settings
    public double FaceDetectionConfidence { get; set; } = 0.6;
    public double FaceRecognitionTolerance { get; set; } = 0.6;
    public bool GroupSimilarFaces { get; set; } = true;
    public int MinFacesPerGroup { get; set; } = 3;
    public bool EnableAgeProgression { get; set; } = true;

    // Screenshot Detection Settings
    public double ScreenshotConfidenceThreshold { get; set; } = 0.8;
    public bool AutoTagScreenshots { get; set; } = true;
    public bool ExcludeScreenshotsFromMemories { get; set; } = true;
    public bool ExcludeScreenshotsFromCollages { get; set; } = true;

    // Duplicate Detection Settings
    public bool EnablePerceptualHashing { get; set; } = true;
    public double DuplicateSimilarityThreshold { get; set; } = 0.95;
    public bool AutoMarkDuplicates { get; set; } = false; // Require manual review
    public bool KeepHighestResolution { get; set; } = true;

    // Privacy Settings
    public bool EncryptFaceEncodings { get; set; } = true;
    public bool AllowTelemetry { get; set; } = false;
    public bool CreateBackups { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 30;

    // LLM Settings
    [MaxLength(100)]
    public string? LLMProvider { get; set; } = "OpenAI"; // OpenAI, Claude, Local, etc.
    
    [MaxLength(500)]
    public string? LLMApiKey { get; set; }
    
    [MaxLength(200)]
    public string? LLMApiEndpoint { get; set; }
    
    [MaxLength(100)]
    public string? LLMModel { get; set; } = "gpt-4o-mini";

    public bool EnableVectorSearch { get; set; } = true;
    public int LLMMaxTokens { get; set; } = 1000;
    public double LLMTemperature { get; set; } = 0.7;

    // UI/UX Settings
    public bool ShowThumbnailsInGrid { get; set; } = true;
    public int ThumbnailGridColumns { get; set; } = 5;
    public bool ShowFileInfo { get; set; } = true;
    public bool ShowFaceCount { get; set; } = true;
    public bool EnableKeyboardShortcuts { get; set; } = true;

    [MaxLength(50)]
    public string ThemeName { get; set; } = "Light";
    
    [MaxLength(50)]
    public string Language { get; set; } = "en-US";

    // Memory Features
    public bool ShowMemoriesOnStartup { get; set; } = true;
    public int MemoryLookbackYears { get; set; } = 10;
    public bool EnableMemoryNotifications { get; set; } = true;
    public TimeSpan MemoryNotificationTime { get; set; } = new(9, 0, 0); // 9 AM

    // Collage Settings
    public int DefaultCollageWidth { get; set; } = 1920;
    public int DefaultCollageHeight { get; set; } = 1080;
    public bool DefaultGrayscaleMode { get; set; } = false;
    public int CollagePadding { get; set; } = 10;
    public int MaxCollageImages { get; set; } = 50;

    // Maintenance Settings
    public bool AutoCleanupThumbnails { get; set; } = true;
    public int CleanupIntervalDays { get; set; } = 7;
    public bool AutoUpdateStatistics { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;

    // Timestamps
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
    public DateTime? LastScanDate { get; set; }

    // Computed Properties
    [NotMapped]
    public string[] SupportedExtensions
    {
        get
        {
            var extensions = new List<string>();
            if (SupportJpeg) extensions.AddRange([".jpg", ".jpeg"]);
            if (SupportPng) extensions.Add(".png");
            if (SupportBmp) extensions.Add(".bmp");
            if (SupportTiff) extensions.AddRange([".tiff", ".tif"]);
            if (SupportWebp) extensions.Add(".webp");
            if (SupportHeic) extensions.AddRange([".heic", ".heif"]);
            if (SupportGif) extensions.Add(".gif");
            if (SupportRaw) extensions.AddRange([".cr2", ".nef", ".arw", ".dng"]);
            return extensions.ToArray();
        }
    }

    [NotMapped]
    public bool HasLLMConfiguration => !string.IsNullOrEmpty(LLMApiKey) && !string.IsNullOrEmpty(LLMProvider);

    [NotMapped]
    public bool IsFirstRun => LastScanDate == null;

    [NotMapped]
    public long MaxMemoryUsageBytes => MaxMemoryUsageMB * 1024L * 1024L;

    // Helper Methods
    public static string GetDefaultPicturesDirectory()
    {
        try
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }
        catch
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures");
        }
    }

    public void MarkAsModified()
    {
        DateModified = DateTime.UtcNow;
    }

    public void MarkScanCompleted()
    {
        LastScanDate = DateTime.UtcNow;
        MarkAsModified();
    }

    public bool IsFeatureEnabled(string featureName)
    {
        return featureName.ToLowerInvariant() switch
        {
            "face" or "facerecognition" => EnableFaceRecognition,
            "onthisday" or "memories" => EnableOnThisDay,
            "screenshots" or "screenshotfiltering" => EnableScreenshotFiltering,
            "duplicates" or "duplicatedetection" => EnableDuplicateDetection,
            "collage" or "collagegeneration" => EnableCollageGeneration,
            "llm" or "llmquery" => EnableLLMQuery && HasLLMConfiguration,
            _ => false
        };
    }

    public void EnableAllFeatures()
    {
        EnableOnThisDay = true;
        EnableFaceRecognition = true;
        EnableScreenshotFiltering = true;
        EnableDuplicateDetection = true;
        EnableCollageGeneration = true;
        EnableLLMQuery = true;
        MarkAsModified();
    }

    public void DisableAllFeatures()
    {
        EnableOnThisDay = false;
        EnableFaceRecognition = false;
        EnableScreenshotFiltering = false;
        EnableDuplicateDetection = false;
        EnableCollageGeneration = false;
        EnableLLMQuery = false;
        MarkAsModified();
    }

    public void ResetToDefaults()
    {
        EnableAllFeatures();
        PhotoDirectory = GetDefaultPicturesDirectory();
        AutoScanOnStartup = false;
        FaceDetectionConfidence = 0.6;
        FaceRecognitionTolerance = 0.6;
        BatchSize = 100;
        MaxMemoryUsageMB = 2048;
        ThemeName = "Light";
        MarkAsModified();
    }

    public Dictionary<string, object> GetFeatureStatus()
    {
        return new Dictionary<string, object>
        {
            ["Face Recognition"] = EnableFaceRecognition,
            ["On This Day"] = EnableOnThisDay,
            ["Screenshot Filtering"] = EnableScreenshotFiltering,
            ["Duplicate Detection"] = EnableDuplicateDetection,
            ["Collage Generation"] = EnableCollageGeneration,
            ["LLM Query"] = EnableLLMQuery && HasLLMConfiguration,
            ["Directory Watching"] = WatchDirectoryForChanges,
            ["Auto Scan"] = AutoScanOnStartup
        };
    }
}