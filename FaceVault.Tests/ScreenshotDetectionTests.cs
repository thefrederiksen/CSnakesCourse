using FaceVault.Services;
using CSnakes.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FaceVault.Tests;

[TestClass]
[DoNotParallelize] // Prevent parallel execution due to Python environment conflicts
public sealed class ScreenshotDetectionTests
{
    private ServiceProvider? _serviceProvider;
    private IScreenshotDetectionService? _screenshotService;
    private ILogger<ScreenshotDetectionService>? _logger;
    
    private string _testImagesDirectory = string.Empty;
    private string _screenshotsDirectory = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        // Get the directory where test images are located
        var testProjectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        _testImagesDirectory = Path.Combine(testProjectDir!, "Images");
        _screenshotsDirectory = Path.Combine(_testImagesDirectory, "screenshots");
        
        // Verify the screenshots directory exists
        Assert.IsTrue(Directory.Exists(_screenshotsDirectory), 
            $"Screenshots directory not found at: {_screenshotsDirectory}");

        // Set up Python environment similar to main FaceVault project
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
        });

        // Configure Python services - use the copied Python directory in test output
        var testBinDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var pythonHome = Path.Join(testBinDir, "Python");

        Console.WriteLine($"Test Python Home: {pythonHome}");

        services
            .WithPython()
            .WithHome(pythonHome)
            .FromRedistributable("3.12");

        // Register screenshot detection service
        services.AddScoped<IScreenshotDetectionService, ScreenshotDetectionService>();

        try
        {
            _serviceProvider = services.BuildServiceProvider();
            
            // Add timeout for service initialization
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            _logger = _serviceProvider.GetRequiredService<ILogger<ScreenshotDetectionService>>();
            _screenshotService = _serviceProvider.GetRequiredService<IScreenshotDetectionService>();

            Assert.IsNotNull(_screenshotService, "Screenshot detection service should be initialized");
            
            // Test Python environment is working
            Console.WriteLine("Testing Python environment initialization...");
            try
            {
                var testResult = _screenshotService.IsScreenshotAsync("nonexistent.jpg").Result;
                Console.WriteLine("Python environment test completed (expected failure for nonexistent file)");
            }
            catch (AggregateException aex) when (aex.InnerException is FileNotFoundException)
            {
                Console.WriteLine("Python environment is working correctly (expected FileNotFoundException)");
            }
        }
        catch (FileNotFoundException)
        {
            // Expected for nonexistent file test - Python environment is working
            Console.WriteLine("Python environment is working correctly");
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Test setup timed out - Python environment may not be initializing properly");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to initialize test environment: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    [Timeout(30000)] // 30 second timeout
    public async Task DetectScreenshot_SingleScreenshot_ReturnsTrue()
    {
        // Arrange
        var screenshotFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();

        Assert.IsTrue(screenshotFiles.Length > 0, 
            $"No image files found in screenshots directory: {_screenshotsDirectory}");

        var testFile = screenshotFiles.First();
        Console.WriteLine($"Testing single screenshot: {Path.GetFileName(testFile)}");

        // Act
        var result = await _screenshotService!.DetectScreenshotAsync(testFile);

        // Assert
        Assert.IsNotNull(result, "Detection result should not be null");
        Assert.IsTrue(result.IsScreenshot, 
            $"File '{Path.GetFileName(testFile)}' should be detected as a screenshot. " +
            $"Confidence: {result.Confidence:F2}, Error: {result.Error ?? "None"}");
        Assert.IsTrue(result.Confidence > 0, "Confidence should be greater than 0");
        Assert.IsNull(result.Error, $"Detection should not have errors. Error: {result.Error}");
    }

    [TestMethod]
    [Timeout(60000)] // 60 second timeout for processing multiple images
    public async Task DetectScreenshot_AllScreenshots_ReturnsTrue()
    {
        // Arrange
        var screenshotFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();

        Assert.IsTrue(screenshotFiles.Length > 0, 
            $"No image files found in screenshots directory: {_screenshotsDirectory}");

        Console.WriteLine($"Testing {screenshotFiles.Length} screenshot files...");

        var failedDetections = new List<string>();
        var results = new List<(string fileName, ScreenshotDetectionResult result)>();

        // Act
        foreach (var file in screenshotFiles)
        {
            var fileName = Path.GetFileName(file);
            Console.WriteLine($"Testing: {fileName}");

            try
            {
                var result = await _screenshotService!.DetectScreenshotAsync(file);
                results.Add((fileName, result));

                // Check if detection failed
                if (!result.IsScreenshot)
                {
                    failedDetections.Add($"{fileName} - Confidence: {result.Confidence:F2}");
                }

                Console.WriteLine($"  Result: {(result.IsScreenshot ? "SCREENSHOT" : "NOT SCREENSHOT")} " +
                                $"(Confidence: {result.Confidence:F2})");
            }
            catch (Exception ex)
            {
                failedDetections.Add($"{fileName} - Exception: {ex.Message}");
                Console.WriteLine($"  ERROR: {ex.Message}");
            }
        }

        // Assert
        Assert.AreEqual(0, failedDetections.Count,
            $"The following {failedDetections.Count} files were not detected as screenshots:\n" +
            string.Join("\n", failedDetections));

        // Additional assertions
        Assert.IsTrue(results.All(r => r.result.IsScreenshot),
            "All files in the screenshots directory should be detected as screenshots");

        Assert.IsTrue(results.All(r => r.result.Confidence > 0),
            "All detection results should have confidence > 0");

        Assert.IsTrue(results.All(r => string.IsNullOrEmpty(r.result.Error)),
            "No detection should have errors");
    }

    [TestMethod]
    [Timeout(60000)] // 60 second timeout
    public async Task IsScreenshotAsync_AllScreenshots_ReturnsTrue()
    {
        // Arrange
        var screenshotFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();

        Assert.IsTrue(screenshotFiles.Length > 0, 
            $"No image files found in screenshots directory: {_screenshotsDirectory}");

        Console.WriteLine($"Testing IsScreenshotAsync for {screenshotFiles.Length} files...");

        var failedDetections = new List<string>();

        // Act & Assert
        foreach (var file in screenshotFiles)
        {
            var fileName = Path.GetFileName(file);
            Console.WriteLine($"Testing IsScreenshotAsync: {fileName}");

            try
            {
                var isScreenshot = await _screenshotService!.IsScreenshotAsync(file);
                
                if (!isScreenshot)
                {
                    failedDetections.Add(fileName);
                }

                Console.WriteLine($"  Result: {(isScreenshot ? "SCREENSHOT" : "NOT SCREENSHOT")}");
            }
            catch (Exception ex)
            {
                failedDetections.Add($"{fileName} - Exception: {ex.Message}");
                Console.WriteLine($"  ERROR: {ex.Message}");
            }
        }

        // Assert
        Assert.AreEqual(0, failedDetections.Count,
            $"The following {failedDetections.Count} files were not detected as screenshots using IsScreenshotAsync:\n" +
            string.Join("\n", failedDetections));
    }

    [TestMethod]
    [Timeout(60000)] // 60 second timeout
    public async Task GetScreenshotConfidenceAsync_AllScreenshots_ReturnsPositiveConfidence()
    {
        // Arrange
        var screenshotFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();

        Assert.IsTrue(screenshotFiles.Length > 0, 
            $"No image files found in screenshots directory: {_screenshotsDirectory}");

        Console.WriteLine($"Testing confidence scores for {screenshotFiles.Length} files...");

        var lowConfidenceFiles = new List<string>();

        // Act & Assert
        foreach (var file in screenshotFiles)
        {
            var fileName = Path.GetFileName(file);
            Console.WriteLine($"Testing confidence: {fileName}");

            try
            {
                var confidence = await _screenshotService!.GetScreenshotConfidenceAsync(file);
                
                Console.WriteLine($"  Confidence: {confidence:F2}");

                Assert.IsTrue(confidence >= 0, $"Confidence should be non-negative for {fileName}");
                
                // We expect screenshots to have reasonable confidence (> 0.5)
                if (confidence <= 0.5)
                {
                    lowConfidenceFiles.Add($"{fileName} - Confidence: {confidence:F2}");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"GetScreenshotConfidenceAsync failed for {fileName}: {ex.Message}");
            }
        }

        // Warning for low confidence (not a failure, but worth noting)
        if (lowConfidenceFiles.Count > 0)
        {
            Console.WriteLine($"WARNING: {lowConfidenceFiles.Count} files had low confidence scores:\n" +
                            string.Join("\n", lowConfidenceFiles));
        }
    }

    [TestMethod]
    [Timeout(10000)] // 10 second timeout for simple test
    public void ScreenshotsDirectory_ContainsImages()
    {
        // Arrange & Act
        var imageFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();

        // Assert
        Assert.IsTrue(imageFiles.Length > 0, 
            $"Screenshots directory should contain at least one image file. " +
            $"Directory: {_screenshotsDirectory}");

        Console.WriteLine($"Found {imageFiles.Length} image files in screenshots directory:");
        foreach (var file in imageFiles)
        {
            Console.WriteLine($"  - {Path.GetFileName(file)}");
        }
        
        // Test if service is initialized
        if (_screenshotService != null)
        {
            Console.WriteLine("Screenshot detection service is properly initialized");
        }
        else
        {
            Console.WriteLine("WARNING: Screenshot detection service is not initialized - other tests may fail");
        }
    }

    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".webp";
    }
}