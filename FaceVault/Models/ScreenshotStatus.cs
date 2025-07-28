namespace FaceVault.Models;

public enum ScreenshotStatus
{
    Unknown = 0,        // Not yet processed for screenshot detection
    NotScreenshot = 1,  // Processed and determined not to be a screenshot
    IsScreenshot = 2    // Processed and determined to be a screenshot
}