using System;
using System.Collections.Generic;

namespace FaceVault.Models;

public partial class tbl_image_metadata
{
    public int ImageId { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public DateTime? DateTaken { get; set; }

    public virtual tbl_images Image { get; set; } = null!;
}
