using Microsoft.EntityFrameworkCore;
using FaceVault.Models;

namespace FaceVault.Data;

public class FaceVaultDbContext : DbContext
{
    public FaceVaultDbContext(DbContextOptions<FaceVaultDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Image> Images { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<Face> Faces { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ImageTag> ImageTags { get; set; }
    public DbSet<AppSettings> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Image entity
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FilePath).IsUnique();
            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.PerceptualHash);
            entity.HasIndex(e => e.DateTaken);
            entity.HasIndex(e => e.DateCreated);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.HasFaces);
            entity.HasIndex(e => e.IsScreenshot);

            // Configure relationships
            entity.HasMany(e => e.Faces)
                  .WithOne(f => f.Image)
                  .HasForeignKey(f => f.ImageId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ImageTags)
                  .WithOne(it => it.Image)
                  .HasForeignKey(it => it.ImageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Person entity
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.DateCreated);
            entity.HasIndex(e => e.IsArchived);
            entity.HasIndex(e => e.IsManuallyCreated);

            // Configure relationships
            entity.HasMany(e => e.Faces)
                  .WithOne(f => f.Person)
                  .HasForeignKey(f => f.PersonId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Self-referencing relationship for profile face
            entity.HasOne(e => e.ProfileFace)
                  .WithMany()
                  .HasForeignKey(e => e.ProfileFaceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Face entity
        modelBuilder.Entity<Face>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ImageId);
            entity.HasIndex(e => e.PersonId);
            entity.HasIndex(e => e.DateDetected);
            entity.HasIndex(e => e.DetectionConfidence);
            entity.HasIndex(e => e.QualityScore);
            entity.HasIndex(e => e.ClusterId);
            entity.HasIndex(e => e.IsManuallyVerified);

            // Configure precision for double values
            entity.Property(e => e.X).HasPrecision(10, 4);
            entity.Property(e => e.Y).HasPrecision(10, 4);
            entity.Property(e => e.Width).HasPrecision(10, 4);
            entity.Property(e => e.Height).HasPrecision(10, 4);
            entity.Property(e => e.DetectionConfidence).HasPrecision(5, 4);
            entity.Property(e => e.QualityScore).HasPrecision(5, 4);
            entity.Property(e => e.Sharpness).HasPrecision(5, 4);
            entity.Property(e => e.Brightness).HasPrecision(5, 4);
            entity.Property(e => e.ClusteringConfidence).HasPrecision(5, 4);
        });

        // Configure Tag entity
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.TagType);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.ParentTagId);
            entity.HasIndex(e => e.IsArchived);
            entity.HasIndex(e => e.UsageCount);

            // Configure self-referencing relationship for hierarchy
            entity.HasOne(e => e.ParentTag)
                  .WithMany(e => e.ChildTags)
                  .HasForeignKey(e => e.ParentTagId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ImageTags)
                  .WithOne(it => it.Tag)
                  .HasForeignKey(it => it.TagId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure precision
            entity.Property(e => e.Confidence).HasPrecision(5, 4);
        });

        // Configure ImageTag junction entity
        modelBuilder.Entity<ImageTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ImageId, e.TagId }).IsUnique();
            entity.HasIndex(e => e.DateTagged);
            entity.HasIndex(e => e.TaggedBy);
            entity.HasIndex(e => e.IsVerified);

            entity.Property(e => e.Confidence).HasPrecision(5, 4);
        });

        // Configure AppSettings entity (singleton)
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Precision for double values
            entity.Property(e => e.FaceDetectionConfidence).HasPrecision(5, 4);
            entity.Property(e => e.FaceRecognitionTolerance).HasPrecision(5, 4);
            entity.Property(e => e.ScreenshotConfidenceThreshold).HasPrecision(5, 4);
            entity.Property(e => e.DuplicateSimilarityThreshold).HasPrecision(5, 4);
            entity.Property(e => e.LLMTemperature).HasPrecision(5, 4);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default tags
        modelBuilder.Entity<Tag>().HasData(
            new Tag
            {
                Id = 1,
                Name = "Family",
                Description = "Family photos and gatherings",
                Color = "#FF6B6B",
                Category = TagCategory.People,
                TagType = TagType.System
            },
            new Tag
            {
                Id = 2,
                Name = "Friends",
                Description = "Photos with friends",
                Color = "#4ECDC4",
                Category = TagCategory.People,
                TagType = TagType.System
            },
            new Tag
            {
                Id = 3,
                Name = "Travel",
                Description = "Travel and vacation photos",
                Color = "#45B7D1",
                Category = TagCategory.Places,
                TagType = TagType.System
            },
            new Tag
            {
                Id = 4,
                Name = "Events",
                Description = "Special events and celebrations",
                Color = "#96CEB4",
                Category = TagCategory.Events,
                TagType = TagType.System
            },
            new Tag
            {
                Id = 5,
                Name = "Screenshots",
                Description = "Screenshots and screen captures",
                Color = "#FFEAA7",
                Category = TagCategory.Technical,
                TagType = TagType.System
            }
        );

        // Seed default application settings
        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings
            {
                Id = 1,
                PhotoDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                EnableOnThisDay = true,
                EnableFaceRecognition = true,
                EnableScreenshotFiltering = true,
                EnableDuplicateDetection = true,
                EnableCollageGeneration = true,
                EnableLLMQuery = true,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            }
        );
    }

    // Database maintenance methods
    public async Task<bool> EnsureDatabaseCreatedAsync()
    {
        try
        {
            return await Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Database creation failed: {ex.Message}");
            return false;
        }
    }

    public Task<int> GetTableCountAsync(string tableName)
    {
        try
        {
            // This is a simplified version - in production you'd want proper SQL query execution
            return tableName switch
            {
                "Images" => Images.CountAsync(),
                "People" => People.CountAsync(), 
                "Faces" => Faces.CountAsync(),
                "Tags" => Tags.CountAsync(),
                "ImageTags" => ImageTags.CountAsync(),
                _ => Task.FromResult(0)
            };
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    public Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var connectionString = Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                var dbPath = connectionString.Split("Data Source=")[1].Split(';')[0];
                if (File.Exists(dbPath))
                {
                    return Task.FromResult(new FileInfo(dbPath).Length);
                }
            }
            return Task.FromResult(0L);
        }
        catch
        {
            return Task.FromResult(0L);
        }
    }

    public async Task<Dictionary<string, int>> GetAllTableCountsAsync()
    {
        var counts = new Dictionary<string, int>();
        
        try
        {
            counts["Images"] = await Images.CountAsync();
            counts["People"] = await People.CountAsync();
            counts["Faces"] = await Faces.CountAsync();
            counts["Tags"] = await Tags.CountAsync();
            counts["ImageTags"] = await ImageTags.CountAsync();
            counts["Settings"] = await AppSettings.CountAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting table counts: {ex.Message}");
        }

        return counts;
    }
}