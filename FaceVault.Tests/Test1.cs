using FaceVault.Services;

namespace FaceVault.Tests;

[TestClass]
public sealed class ImageHashServiceTests
{
    private string _testFilesDirectory = string.Empty;
    private string _testFile1 = string.Empty;
    private string _testFile2 = string.Empty;
    private string _identicalFile = string.Empty;
    
    [TestInitialize]
    public void Setup()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        _testFilesDirectory = Path.Combine(Path.GetTempPath(), $"FaceVaultTests_{uniqueId}");
        Directory.CreateDirectory(_testFilesDirectory);
        
        // Create test files with unique names
        _testFile1 = Path.Combine(_testFilesDirectory, $"test1_{uniqueId}.txt");
        _testFile2 = Path.Combine(_testFilesDirectory, $"test2_{uniqueId}.txt");
        _identicalFile = Path.Combine(_testFilesDirectory, $"identical_{uniqueId}.txt");
        
        File.WriteAllText(_testFile1, "This is test content for file 1");
        File.WriteAllText(_testFile2, "This is different content for file 2");
        File.WriteAllText(_identicalFile, "This is test content for file 1");
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testFilesDirectory))
        {
            Directory.Delete(_testFilesDirectory, true);
        }
    }
    
    [TestMethod]
    public void CalculateFileHash_ValidFile_ReturnsHash()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act
        var hash = service.CalculateFileHash(_testFile1);
        
        // Assert
        Assert.IsNotNull(hash);
        Assert.AreEqual(64, hash.Length); // SHA256 produces 64 character hex string
        Assert.IsTrue(hash.All(c => "0123456789abcdef".Contains(c))); // Valid hex
    }
    
    [TestMethod]
    public void CalculateFileHash_SameFile_ReturnsSameHash()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act
        var hash1 = service.CalculateFileHash(_testFile1);
        var hash2 = service.CalculateFileHash(_testFile1);
        
        // Assert
        Assert.AreEqual(hash1, hash2);
    }
    
    [TestMethod]
    public void CalculateFileHash_DifferentFiles_ReturnsDifferentHashes()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act
        var hash1 = service.CalculateFileHash(_testFile1);
        var hash2 = service.CalculateFileHash(_testFile2);
        
        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }
    
    [TestMethod]
    public void CalculateFileHash_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => service.CalculateFileHash(null!));
    }
    
    [TestMethod]
    public void CalculateFileHash_EmptyPath_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => service.CalculateFileHash(""));
    }
    
    [TestMethod]
    public void CalculateFileHash_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var service = new ImageHashService();
        var nonexistentFile = Path.Combine(_testFilesDirectory, "nonexistent.txt");
        
        // Act & Assert
        Assert.ThrowsException<FileNotFoundException>(() => service.CalculateFileHash(nonexistentFile));
    }
    
    [TestMethod]
    public void AreFilesIdentical_IdenticalFiles_ReturnsTrue()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act
        var result = service.AreFilesIdentical(_testFile1, _identicalFile);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void AreFilesIdentical_DifferentFiles_ReturnsFalse()
    {
        // Arrange
        var service = new ImageHashService();
        
        // Act
        var result = service.AreFilesIdentical(_testFile1, _testFile2);
        
        // Assert
        Assert.IsFalse(result);
    }
}
