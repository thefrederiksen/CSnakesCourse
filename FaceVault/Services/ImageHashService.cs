using System.Security.Cryptography;

namespace FaceVault.Services;

public class ImageHashService
{
    /// <summary>
    /// Calculates SHA256 hash of a file for duplicate detection
    /// </summary>
    public string CalculateFileHash(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
            
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
            
        Logger.Debug($"Calculating hash for file: {filePath}");
        
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        
        var hashBytes = sha256.ComputeHash(stream);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        
        Logger.Debug($"Hash calculated: {hash} for file: {filePath}");
        return hash;
    }
    
    /// <summary>
    /// Checks if two files have the same hash
    /// </summary>
    public bool AreFilesIdentical(string filePath1, string filePath2)
    {
        var hash1 = CalculateFileHash(filePath1);
        var hash2 = CalculateFileHash(filePath2);
        
        return hash1 == hash2;
    }
}