using System.Diagnostics;

namespace FaceVault.Services;

public interface IFileOpenService
{
    void OpenInDefaultViewer(string filePath);
    bool OpenInExplorer(string filePath);
}

public class FileOpenService : IFileOpenService
{
    private readonly ILogger<FileOpenService> _logger;

    public FileOpenService(ILogger<FileOpenService> logger)
    {
        _logger = logger;
    }

    public void OpenInDefaultViewer(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Cannot open file - file not found: {FilePath}", filePath);
                return;
            }

            // Use Process.Start to open the file with the default associated program
            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true // This is important for opening with default program
            };

            Process.Start(startInfo);
            _logger.LogInformation("Opened file in default viewer: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening file: {FilePath}", filePath);
        }
    }

    public bool OpenInExplorer(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Cannot open in explorer - file not found: {FilePath}", filePath);
                return false;
            }

            // Open Windows Explorer and select the file
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            _logger.LogInformation("Opened file location in Explorer: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening file in Explorer: {FilePath}", filePath);
            return false;
        }
    }
}