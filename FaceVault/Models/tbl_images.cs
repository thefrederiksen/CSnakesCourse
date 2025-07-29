using System;
using System.Collections.Generic;

namespace FaceVault.Models;

public partial class tbl_images
{
    public int ImageId { get; set; }

    public string RelativePath { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string? FileExtension { get; set; }

    public string FileHash { get; set; } = null!;

    public int FileSizeBytes { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public int IsDeleted { get; set; }

    public int FileExists { get; set; }

    public DateTime? DateDeleted { get; set; }

    public virtual tbl_image_analysis? tbl_image_analysis { get; set; }

    public virtual tbl_image_metadata? tbl_image_metadata { get; set; }
}
