using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceVault.Models;

public class Face
{
    [Key]
    public int Id { get; set; }

    // Foreign Keys
    [Required]
    public int ImageId { get; set; }
    
    public int? PersonId { get; set; }

    // Face Detection Data
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    // Face Quality Metrics
    public double DetectionConfidence { get; set; }
    public double QualityScore { get; set; } = 0.0;
    public double Sharpness { get; set; } = 0.0;
    public double Brightness { get; set; } = 0.0;
    
    // Face Pose and Attributes
    public double? FaceAngle { get; set; }
    public double? HeadPose { get; set; }
    public bool IsBlurry { get; set; } = false;
    public bool IsPartiallyOccluded { get; set; } = false;

    // Face Recognition Data
    [Column(TypeName = "BLOB")]
    public byte[]? FaceEncoding { get; set; }

    [MaxLength(50)]
    public string? EncodingVersion { get; set; }

    // Processing Information
    public DateTime DateDetected { get; set; } = DateTime.UtcNow;
    public DateTime? DateRecognized { get; set; }
    
    [MaxLength(100)]
    public string? DetectionModel { get; set; }

    [MaxLength(100)]
    public string? RecognitionModel { get; set; }

    // Manual Verification
    public bool IsManuallyVerified { get; set; } = false;
    public bool IsCorrectPerson { get; set; } = true;
    public DateTime? DateVerified { get; set; }

    [MaxLength(200)]
    public string? VerificationNotes { get; set; }

    // Face Thumbnail
    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    // Clustering Information
    public int? ClusterId { get; set; }
    public double ClusteringConfidence { get; set; } = 0.0;

    // Additional Metadata
    [Column(TypeName = "TEXT")]
    public string? MetadataJson { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(ImageId))]
    public virtual Image Image { get; set; } = null!;

    [ForeignKey(nameof(PersonId))]
    public virtual Person? Person { get; set; }

    // Computed Properties
    [NotMapped]
    public double CenterX => X + (Width / 2);

    [NotMapped]
    public double CenterY => Y + (Height / 2);

    [NotMapped]
    public double Area => Width * Height;

    [NotMapped]
    public double AspectRatio => Height > 0 ? Width / Height : 1.0;

    [NotMapped]
    public bool IsSquarish => Math.Abs(AspectRatio - 1.0) < 0.2;

    [NotMapped]
    public bool IsHighQuality => QualityScore > 0.7 && !IsBlurry && DetectionConfidence > 0.8;

    [NotMapped]
    public bool HasEncoding => FaceEncoding != null && FaceEncoding.Length > 0;

    [NotMapped]
    public bool IsAssigned => PersonId.HasValue;

    [NotMapped]
    public string QualityDescription => QualityScore switch
    {
        >= 0.8 => "Excellent",
        >= 0.6 => "Good", 
        >= 0.4 => "Fair",
        >= 0.2 => "Poor",
        _ => "Very Poor"
    };

    // Helper Methods
    public void SetBoundingBox(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void AssignToPerson(int personId, double confidence = 1.0)
    {
        PersonId = personId;
        ClusteringConfidence = confidence;
        DateRecognized = DateTime.UtcNow;
    }

    public void UpdateQualityMetrics(double quality, double sharpness, double brightness)
    {
        QualityScore = Math.Max(0, Math.Min(1, quality));
        Sharpness = Math.Max(0, Math.Min(1, sharpness));
        Brightness = Math.Max(0, Math.Min(1, brightness));
        
        // Update blur detection based on sharpness
        IsBlurry = sharpness < 0.3;
    }

    public void VerifyPerson(bool isCorrect, string? notes = null)
    {
        IsManuallyVerified = true;
        IsCorrectPerson = isCorrect;
        DateVerified = DateTime.UtcNow;
        VerificationNotes = notes;
    }

    public Rectangle GetBoundingRectangle()
    {
        return new Rectangle
        {
            X = (int)Math.Round(X),
            Y = (int)Math.Round(Y),
            Width = (int)Math.Round(Width),
            Height = (int)Math.Round(Height)
        };
    }
}

// Helper struct for bounding rectangles
public struct Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public int Right => X + Width;
    public int Bottom => Y + Height;
    public int Area => Width * Height;
}