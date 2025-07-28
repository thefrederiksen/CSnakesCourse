using FaceVault.Services;
using CSnakes.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FaceVault.Tests;

/// <summary>
/// Simplified screenshot detection tests that are more reliable in Visual Studio
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SimpleScreenshotTests
{
    private static ServiceProvider? _serviceProvider;
    private static IScreenshotDetectionService? _screenshotService;
    private static string _screenshotsDirectory = string.Empty;

    [ClassInitialize]
    public static void ClassSetup(TestContext context)
    {
        try
        {
            Console.WriteLine("Setting up screenshot detection test environment...");
            
            // Get the directory where test images are located
            var testProjectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var testImagesDirectory = Path.Combine(testProjectDir!, "Images");
            _screenshotsDirectory = Path.Combine(testImagesDirectory, "screenshots");
            
            Console.WriteLine($"Screenshots directory: {_screenshotsDirectory}");
            
            // Set up Python environment
            var services = new ServiceCollection();
            
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning); // Reduce logging noise
            });

            var testBinDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pythonHome = Path.Join(testBinDir, "Python");
            
            Console.WriteLine($"Python Home: {pythonHome}");

            services
                .WithPython()
                .WithHome(pythonHome)
                .FromRedistributable("3.12");

            services.AddScoped<IScreenshotDetectionService, ScreenshotDetectionService>();

            _serviceProvider = services.BuildServiceProvider();
            _screenshotService = _serviceProvider.GetRequiredService<IScreenshotDetectionService>();
            
            Console.WriteLine("Screenshot detection test environment setup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to setup test environment: {ex.Message}");
            throw;
        }
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    [Timeout(15000)]
    public void TestEnvironment_IsInitialized()
    {
        Assert.IsNotNull(_screenshotService, "Screenshot detection service should be initialized");
        Assert.IsTrue(Directory.Exists(_screenshotsDirectory), $"Screenshots directory should exist: {_screenshotsDirectory}");
        
        var imageFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();
            
        Assert.IsTrue(imageFiles.Length > 0, "Screenshots directory should contain at least one image file");
        Console.WriteLine($"Test environment verified: {imageFiles.Length} image files found");
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task SingleScreenshot_IsDetectedCorrectly()
    {
        Assert.IsNotNull(_screenshotService, "Screenshot service not initialized");
        
        var imageFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();
            
        Assert.IsTrue(imageFiles.Length > 0, "No image files found for testing");
        
        var testFile = imageFiles.First();
        Console.WriteLine($"Testing screenshot detection on: {Path.GetFileName(testFile)}");
        
        try
        {
            var result = await _screenshotService.DetectScreenshotAsync(testFile);
            
            Assert.IsNotNull(result, "Detection result should not be null");
            Assert.IsTrue(result.IsScreenshot, 
                $"File '{Path.GetFileName(testFile)}' should be detected as a screenshot. " +
                $"Confidence: {result.Confidence:F2}, Error: {result.Error ?? "None"}");
            Assert.IsTrue(result.Confidence > 0, "Confidence should be greater than 0");
            
            Console.WriteLine($"✓ Screenshot detected successfully with confidence: {result.Confidence:F2}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Screenshot detection failed: {ex.Message}");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BooleanDetection_WorksCorrectly()
    {
        Assert.IsNotNull(_screenshotService, "Screenshot service not initialized");
        
        var imageFiles = Directory.GetFiles(_screenshotsDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => IsImageFile(file))
            .ToArray();
            
        var testFile = imageFiles.First();
        
        try
        {
            var isScreenshot = await _screenshotService.IsScreenshotAsync(testFile);
            
            Assert.IsTrue(isScreenshot, $"File '{Path.GetFileName(testFile)}' should be detected as a screenshot");
            Console.WriteLine($"✓ Boolean detection successful for: {Path.GetFileName(testFile)}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Boolean screenshot detection failed: {ex.Message}");
        }
    }

    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".webp";
    }
}