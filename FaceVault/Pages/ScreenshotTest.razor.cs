using FaceVault.Services;
using Microsoft.AspNetCore.Components;

namespace FaceVault.Pages;

public partial class ScreenshotTest : ComponentBase
{
    private List<TestResult> testResults = new();
    private bool isRunning = false;
    private string errorMessage = "";

    private async Task TestScreenshotDetection()
    {
        await RunTest("Screenshot Detection Test", async () =>
        {
            var results = new List<string>();
            var success = true;

            try
            {
                results.Add("Testing comprehensive screenshot detection...");
                
                // Test cases with expected results
                var testCases = new[]
                {
                    new { filename = "screenshot_2024.png", expectedScreenshot = true, description = "Screenshot filename" },
                    new { filename = "Screen Shot 2024-01-01.jpg", expectedScreenshot = true, description = "macOS screenshot" },
                    new { filename = "capture.png", expectedScreenshot = true, description = "Capture filename" },
                    new { filename = "snip_tool.png", expectedScreenshot = true, description = "Snipping tool" },
                    new { filename = "vacation_photo.jpg", expectedScreenshot = false, description = "Regular photo" },
                    new { filename = "IMG_1234.jpg", expectedScreenshot = false, description = "Camera photo" },
                    new { filename = "family_portrait.png", expectedScreenshot = false, description = "Portrait photo" }
                };
                
                int correctPredictions = 0;
                
                foreach (var testCase in testCases)
                {
                    var result = await ScreenshotService.DetectScreenshotAsync(testCase.filename);
                    var isCorrect = result.IsScreenshot == testCase.expectedScreenshot;
                    if (isCorrect) correctPredictions++;
                    
                    var status = isCorrect ? "✓" : "✗";
                    results.Add($"{status} {testCase.description}: {result.IsScreenshot} (conf: {result.Confidence:F2}) - Expected: {testCase.expectedScreenshot}");
                    
                    // Show detailed analysis if available
                    if (result.Analysis.ContainsKey("filename"))
                    {
                        var filenameAnalysis = result.Analysis["filename"];
                        results.Add($"  → Filename analysis: {filenameAnalysis}");
                    }
                    
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        results.Add($"  → Error: {result.Error}");
                    }
                }
                
                var accuracy = (double)correctPredictions / testCases.Length * 100;
                results.Add($"");
                results.Add($"Accuracy: {correctPredictions}/{testCases.Length} ({accuracy:F1}%)");
                
                // Test with real image files if they exist
                await TestRealImageFiles(results);
                
                success = accuracy >= 70; // Require at least 70% accuracy
                
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    results.Add($"Inner: {ex.InnerException.Message}");
                }
                success = false;
            }

            return new TestResult
            {
                Success = success,
                Results = results,
                TestName = "Screenshot Detection Test"
            };
        });
    }
    
    private async Task TestRealImageFiles(List<string> results)
    {
        results.Add("");
        results.Add("Testing with real image files:");
        
        // Check for test images
        var testImagePaths = new[]
        {
            Path.Combine("Python", "test", "images", "screenshot.jpg"),
            Path.Combine("Python", "test", "images", "photo.jpg")
        };
        
        foreach (var imagePath in testImagePaths)
        {
            if (File.Exists(imagePath))
            {
                try
                {
                    var result = await ScreenshotService.DetectScreenshotAsync(imagePath);
                    results.Add($"Real file {Path.GetFileName(imagePath)}: {result.IsScreenshot} (conf: {result.Confidence:F2})");
                    
                    // Show detailed analysis
                    if (result.Analysis.ContainsKey("total_score"))
                    {
                        results.Add($"  → Total score: {result.Analysis["total_score"]} / {result.Analysis.GetValueOrDefault("max_score", "N/A")}");
                    }
                    
                    if (result.Analysis.ContainsKey("dimensions"))
                    {
                        var dimensions = result.Analysis["dimensions"];
                        results.Add($"  → Dimensions: {dimensions}");
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"Error analyzing {imagePath}: {ex.Message}");
                }
            }
            else
            {
                results.Add($"Test image not found: {imagePath}");
            }
        }
    }

    private async Task TestBasicPython()
    {
        await RunTest("Basic Python Test", async () =>
        {
            var results = new List<string>();
            var success = true;

            try
            {
                results.Add("Testing basic Python integration...");
                
                // Test a simple call that doesn't require files
                var testResult = await ScreenshotService.DetectScreenshotAsync("test_file.png");
                results.Add($"✓ Python call successful");
                results.Add($"Result type: {testResult.GetType().Name}");
                results.Add($"IsScreenshot: {testResult.IsScreenshot}");
                results.Add($"Confidence: {testResult.Confidence}");
                results.Add($"Error: {testResult.Error ?? "None"}");
                
            }
            catch (Exception ex)
            {
                results.Add($"❌ Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    results.Add($"Inner: {ex.InnerException.Message}");
                }
                success = false;
            }

            return new TestResult
            {
                Success = success,
                Results = results,
                TestName = "Basic Python Test"
            };
        });
    }

    private async Task RunTest(string testName, Func<Task<TestResult>> testFunc)
    {
        if (isRunning) return;

        isRunning = true;
        errorMessage = "";
        StateHasChanged();

        try
        {
            var result = await testFunc();
            testResults.Insert(0, result); // Add to beginning for newest first
        }
        catch (Exception ex)
        {
            errorMessage = $"Test execution failed: {ex.Message}\n{ex.StackTrace}";
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private void ClearResults()
    {
        testResults.Clear();
        errorMessage = "";
        StateHasChanged();
    }

    private string GetResultLineClass(string line)
    {
        if (line.Contains("ERROR") || line.Contains("❌"))
            return "text-danger";
        if (line.Contains("WARNING") || line.Contains("⚠"))
            return "text-warning";
        if (line.Contains("✓"))
            return "text-success";
        return "";
    }
}

public class TestResult
{
    public bool Success { get; set; }
    public List<string> Results { get; set; } = new();
    public string TestName { get; set; } = "";
}