using System;
using System.Collections.Generic;

namespace FaceVault.Models;

public partial class tbl_app_settings
{
    public int Id { get; set; }

    public int? EnableDuplicateDetection { get; set; }

    public int? EnableAIImageAnalysis { get; set; }

    public string? PhotoDirectory { get; set; }

    public int? AutoScanOnStartup { get; set; }

    public int? ScanSubdirectories { get; set; }

    public int? SupportJpeg { get; set; }

    public int? SupportPng { get; set; }

    public int? SupportHeic { get; set; }

    public int? SupportGif { get; set; }

    public int? SupportBmp { get; set; }

    public int? SupportWebp { get; set; }

    public int? BatchSize { get; set; }

    public int? MaxConcurrentTasks { get; set; }

    public string? AIProvider { get; set; }

    public string? AIApiKey { get; set; }

    public string? AIApiEndpoint { get; set; }

    public string? AIModel { get; set; }

    public double? AITemperature { get; set; }

    public string? ThemeName { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateModified { get; set; }

    public DateTime? LastScanDate { get; set; }
}
