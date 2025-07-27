using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaceVault.Models;

public class Person
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateModified { get; set; }

    // Person Management
    public bool IsManuallyCreated { get; set; } = false;
    public bool IsConfirmed { get; set; } = false;
    public double GroupingConfidence { get; set; } = 0.0;

    // Profile Information
    public int? ProfileFaceId { get; set; }
    
    [MaxLength(500)]
    public string? ProfileImagePath { get; set; }

    // Statistics (computed periodically)
    public int FaceCount { get; set; } = 0;
    public int ImageCount { get; set; } = 0;
    public DateTime? FirstAppearance { get; set; }
    public DateTime? LastAppearance { get; set; }

    // Colors and Display
    [MaxLength(7)]
    public string? DisplayColor { get; set; }

    public bool IsArchived { get; set; } = false;
    public DateTime? DateArchived { get; set; }

    // Additional metadata
    [Column(TypeName = "TEXT")]
    public string? MetadataJson { get; set; }

    // Navigation Properties
    public virtual ICollection<Face> Faces { get; set; } = new List<Face>();

    // Foreign Key for Profile Face
    [ForeignKey(nameof(ProfileFaceId))]
    public virtual Face? ProfileFace { get; set; }

    // Computed Properties
    [NotMapped]
    public bool IsUnknown => Name.StartsWith("Unknown Person", StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Person #{Id}" : Name;

    [NotMapped]
    public TimeSpan? ActivePeriod => FirstAppearance.HasValue && LastAppearance.HasValue 
        ? LastAppearance.Value - FirstAppearance.Value 
        : null;

    [NotMapped]
    public double PhotosPerDay => ActivePeriod?.TotalDays > 0 ? ImageCount / ActivePeriod.Value.TotalDays : 0;

    // Helper Methods
    public void UpdateStatistics()
    {
        FaceCount = Faces?.Count ?? 0;
        ImageCount = Faces?.Select(f => f.ImageId).Distinct().Count() ?? 0;
        FirstAppearance = Faces?.Min(f => f.Image?.DateTaken ?? f.Image?.DateCreated ?? DateTime.MinValue);
        LastAppearance = Faces?.Max(f => f.Image?.DateTaken ?? f.Image?.DateCreated ?? DateTime.MinValue);
        DateModified = DateTime.UtcNow;
    }

    public void SetAsUnknown(int sequenceNumber)
    {
        Name = $"Unknown Person {sequenceNumber:D3}";
        IsManuallyCreated = false;
        IsConfirmed = false;
    }

    public void AssignName(string personName, bool isManual = true)
    {
        Name = personName;
        IsManuallyCreated = isManual;
        IsConfirmed = isManual;
        DateModified = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        DateArchived = DateTime.UtcNow;
        DateModified = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsArchived = false;
        DateArchived = null;
        DateModified = DateTime.UtcNow;
    }
}