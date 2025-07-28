using System.IO;

namespace FaceVault.Models;

public class DuplicateScanProgress
{
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int ImagesWithHashes { get; set; }
    public int DuplicatesFound { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }
    public double ProgressPercentage => TotalImages > 0 ? (double)ProcessedImages / TotalImages * 100 : 0;
    public string CurrentFileDisplay => Path.GetFileName(CurrentFile);
}

public class DuplicateScanResult
{
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int HashesCalculated { get; set; }
    public int DuplicatesFound { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool WasCancelled { get; set; }
}

public class DuplicateStatistics
{
    public int TotalImages { get; set; }
    public int ImagesWithHashes { get; set; }
    public int ImagesWithoutHashes { get; set; }
    public int UniqueImages { get; set; }
    public int DuplicateImages { get; set; }
    public int DuplicateGroups { get; set; }
    public long TotalDuplicateBytes { get; set; }
    public string TotalDuplicateSizeFormatted => FormatBytes(TotalDuplicateBytes);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class DuplicateGroup
{
    public string Hash { get; set; } = string.Empty;
    public List<Image> Images { get; set; } = new();
    public int Count => Images.Count;
    public long TotalSize => Images.Sum(i => i.FileSizeBytes);
    public long WastedSpace => Images.Skip(1).Sum(i => i.FileSizeBytes); // All but the first are "wasted"
    public string TotalSizeFormatted => FormatBytes(TotalSize);
    public string WastedSpaceFormatted => FormatBytes(WastedSpace);
    
    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}

public class DuplicateCleanupProgress
{
    public int TotalGroups { get; set; }
    public int ProcessedGroups { get; set; }
    public int FilesDeleted { get; set; }
    public long BytesFreed { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public double ProgressPercentage => TotalGroups > 0 ? (double)ProcessedGroups / TotalGroups * 100 : 0;
}

public class DuplicateCleanupResult
{
    public int GroupsProcessed { get; set; }
    public int FilesDeleted { get; set; }
    public long BytesFreed { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool WasSuccessful => ErrorCount == 0;
}

public class HashCalculationProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }
    public double ProgressPercentage => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
    public string CurrentFileDisplay => Path.GetFileName(CurrentFile);
}