using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceVault.Models;

public class Image
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string FileExtension { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? PerceptualHash { get; set; }

    public long FileSizeBytes { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public DateTime? DateTaken { get; set; }

    // EXIF Data
    [MaxLength(100)]
    public string? CameraMake { get; set; }
    
    [MaxLength(100)]
    public string? CameraModel { get; set; }
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    [MaxLength(200)]
    public string? LocationName { get; set; }

    public double? FNumber { get; set; }
    public double? ExposureTime { get; set; }
    public int? ISO { get; set; }
    public double? FocalLength { get; set; }

    // Processing Status
    public bool IsProcessed { get; set; } = false;
    public bool HasFaces { get; set; } = false;
    public DateTime? LastProcessed { get; set; }

    // File Status
    public bool IsDeleted { get; set; } = false;
    public bool FileExists { get; set; } = true;
    public DateTime? DateDeleted { get; set; }

    // Thumbnail
    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    // Classification
    public ScreenshotStatus ScreenshotStatus { get; set; } = ScreenshotStatus.Unknown;
    public bool IsScreenshot { get; set; } = false; // Keep for backward compatibility
    public double ScreenshotConfidence { get; set; } = 0.0;

    // JSON storage for additional metadata
    [Column(TypeName = "TEXT")]
    public string? ExifJson { get; set; }

    [Column(TypeName = "TEXT")]
    public string? ProcessingNotes { get; set; }

    // Navigation Properties
    public virtual ICollection<Face> Faces { get; set; } = new List<Face>();
    public virtual ICollection<ImageTag> ImageTags { get; set; } = new List<ImageTag>();

    // Computed Properties
    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(FileName) ? FileName : Path.GetFileName(FilePath);

    [NotMapped]
    public double AspectRatio => Height > 0 ? (double)Width / Height : 1.0;

    [NotMapped]
    public string SizeFormatted => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F1} GB"
    };

    [NotMapped]
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
}