namespace FaceVault.Services;

public interface IScreenshotDetectionService
{
    Task<ScreenshotDetectionResult> DetectScreenshotAsync(string filePath);
    Task<bool> IsScreenshotAsync(string filePath);
    Task<double> GetScreenshotConfidenceAsync(string filePath);
}

public class ScreenshotDetectionResult
{
    public bool IsScreenshot { get; set; }
    public double Confidence { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public Dictionary<string, object> Analysis { get; set; } = new();
    public string? Error { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class ScreenshotAnalysisDetails
{
    public FilenameAnalysis Filename { get; set; } = new();
    public ExifAnalysis Exif { get; set; } = new();
    public DimensionAnalysis Dimensions { get; set; } = new();
    public ContentAnalysis Content { get; set; } = new();
    public double TotalScore { get; set; }
    public double MaxScore { get; set; }
    public double Confidence { get; set; }
}

public class FilenameAnalysis
{
    public string Filename { get; set; } = string.Empty;
    public List<string> Matches { get; set; } = new();
    public double Score { get; set; }
}

public class ExifAnalysis
{
    public bool HasExif { get; set; }
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? Software { get; set; }
    public double Score { get; set; }
}

public class DimensionAnalysis
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double AspectRatio { get; set; }
    public bool MatchesScreenResolution { get; set; }
    public double? MatchesCommonRatio { get; set; }
    public double Score { get; set; }
}

public class ContentAnalysis
{
    public double ColorAnalysis { get; set; }
    public double EdgeAnalysis { get; set; }
    public double UniformityAnalysis { get; set; }
    public double Score { get; set; }
    public string? Error { get; set; }
}