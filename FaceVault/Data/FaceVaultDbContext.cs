using System;
using System.Collections.Generic;
using FaceVault.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceVault.Data;

public partial class FaceVaultDbContext : DbContext
{
    public FaceVaultDbContext()
    {
    }

    public FaceVaultDbContext(DbContextOptions<FaceVaultDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<tbl_app_settings> tbl_app_settings { get; set; }

    public virtual DbSet<tbl_image_analysis> tbl_image_analysis { get; set; }

    public virtual DbSet<tbl_image_metadata> tbl_image_metadata { get; set; }

    public virtual DbSet<tbl_images> tbl_images { get; set; }

    public virtual DbSet<tbl_version> tbl_version { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Development database connection - not sensitive since it's a local dev database
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=Database\\dev_facevault.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<tbl_app_settings>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DateCreated).HasColumnType("DATETIME");
            entity.Property(e => e.DateModified).HasColumnType("DATETIME");
            entity.Property(e => e.LastScanDate).HasColumnType("DATETIME");
        });

        modelBuilder.Entity<tbl_image_analysis>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.HasIndex(e => e.ImageCategory, "IX_tbl_image_analysis_ImageCategory");

            entity.Property(e => e.ImageId).ValueGeneratedNever();
            entity.Property(e => e.AIAnalyzedAt).HasColumnType("DATETIME");

            entity.HasOne(d => d.Image).WithOne(p => p.tbl_image_analysis).HasForeignKey<tbl_image_analysis>(d => d.ImageId);
        });

        modelBuilder.Entity<tbl_image_metadata>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.Property(e => e.ImageId).ValueGeneratedNever();
            entity.Property(e => e.DateTaken).HasColumnType("DATETIME");

            entity.HasOne(d => d.Image).WithOne(p => p.tbl_image_metadata).HasForeignKey<tbl_image_metadata>(d => d.ImageId);
        });

        modelBuilder.Entity<tbl_images>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.HasIndex(e => e.DateCreated, "IX_tbl_images_DateCreated");

            entity.HasIndex(e => e.FileHash, "IX_tbl_images_FileHash");

            entity.HasIndex(e => e.IsDeleted, "IX_tbl_images_IsDeleted");

            entity.HasIndex(e => e.RelativePath, "IX_tbl_images_RelativePath").IsUnique();

            entity.Property(e => e.DateCreated).HasColumnType("DATETIME");
            entity.Property(e => e.DateDeleted).HasColumnType("DATETIME");
            entity.Property(e => e.DateModified).HasColumnType("DATETIME");
            entity.Property(e => e.FileExists).HasDefaultValue(1);
        });

        modelBuilder.Entity<tbl_version>(entity =>
        {
            entity.HasNoKey();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
