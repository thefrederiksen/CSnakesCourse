using CSnakes.Runtime;

namespace FaceVault.Services;

public class ScreenshotDetectionService : IScreenshotDetectionService
{
    private readonly IPythonEnvironment _pythonEnv;
    private readonly ILogger<ScreenshotDetectionService> _logger;

    public ScreenshotDetectionService(IPythonEnvironment pythonEnv, ILogger<ScreenshotDetectionService> logger)
    {
        _pythonEnv = pythonEnv;
        _logger = logger;
        
        // Check if Python libraries are available
        try
        {
            dynamic libCheck = _pythonEnv.Screenshots().CheckLibraries();
            _logger.LogInformation("Python library check: {Result}", (string)(libCheck?.ToString() ?? "null"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not check Python libraries: {Message}", ex.Message);
        }
    }

    public async Task<ScreenshotDetectionResult> DetectScreenshotAsync(string filePath)
    {
        object? result = null;
        try
        {
            _logger.LogDebug("Analyzing screenshot for: {FilePath}", filePath);

            // Call Python screenshot detection method
            result = await Task.Run(() =>
            {
                return _pythonEnv.Screenshots().DetectScreenshot(filePath);
            });
            
            _logger.LogDebug("Python result type: {Type}, Value: {Value}", result?.GetType()?.Name ?? "null", result?.ToString() ?? "null");

            // Extract results from Python tuple (is_screenshot, confidence, details)
            // CSnakes returns a ValueTuple<bool, double, IReadOnlyDictionary<string, PyObject>>
            if (result is ValueTuple<bool, double, System.Collections.Generic.IReadOnlyDictionary<string, CSnakes.Runtime.Python.PyObject>> resultTuple)
            {
                var isScreenshot = resultTuple.Item1;
                var confidence = resultTuple.Item2;
                var pyDetailsDict = resultTuple.Item3;
                
                // Convert Python dictionary to C# dictionary
                var detailsDict = new Dictionary<string, object>();
                try
                {
                    if (pyDetailsDict != null)
                    {
                        foreach (var kvp in pyDetailsDict)
                        {
                            detailsDict[kvp.Key] = kvp.Value?.ToString() ?? "null";
                        }
                    }
                }
                catch (Exception dictEx)
                {
                    _logger.LogWarning("Could not parse analysis details: {Message}", dictEx.Message);
                    detailsDict["error"] = "Could not parse analysis details";
                }
                
                _logger.LogInformation("Screenshot detection result for {FilePath}: IsScreenshot={IsScreenshot}, Confidence={Confidence}", 
                    filePath, isScreenshot, confidence);
                
                return new ScreenshotDetectionResult
                {
                    IsScreenshot = isScreenshot,
                    Confidence = confidence,
                    FilePath = filePath,
                    Analysis = detailsDict,
                    AnalyzedAt = DateTime.UtcNow
                };
            }
            else
            {
                throw new InvalidOperationException($"Unexpected result type from Python: {result?.GetType()?.Name ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting screenshot for file: {FilePath}", filePath);
            
            // Try to extract useful information from the exception
            var errorMessage = ex.Message;
            var confidence = 0.0;
            var isScreenshot = false;
            
            // If it's a type conversion error, try to parse the result as a fallback
            if (ex.Message.Contains("Unable to cast") || ex.Message.Contains("IndexOutOfRangeException"))
            {
                try
                {
                    // Fallback: try to parse the raw result string
                    var resultString = result?.ToString() ?? "";
                    isScreenshot = resultString.Contains("True");
                    confidence = isScreenshot ? 0.5 : 0.1;
                    errorMessage = $"Fallback parsing used: {ex.Message}";
                }
                catch
                {
                    // Final fallback: filename analysis
                    var filename = Path.GetFileName(filePath).ToLower();
                    isScreenshot = filename.Contains("screenshot") || filename.Contains("capture") || filename.Contains("snip");
                    confidence = isScreenshot ? 0.3 : 0.1;
                    errorMessage = $"Filename-only analysis: {ex.Message}";
                }
            }
            
            return new ScreenshotDetectionResult
            {
                IsScreenshot = isScreenshot,
                Confidence = confidence,
                FilePath = filePath,
                Error = errorMessage,
                Analysis = new Dictionary<string, object>
                {
                    {"error_type", ex.GetType().Name},
                    {"fallback_method", "exception_handling"},
                    {"raw_result", result?.ToString() ?? "null"}
                },
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<bool> IsScreenshotAsync(string filePath)
    {
        var result = await DetectScreenshotAsync(filePath);
        return result.IsScreenshot;
    }

    public async Task<double> GetScreenshotConfidenceAsync(string filePath)
    {
        var result = await DetectScreenshotAsync(filePath);
        return result.Confidence;
    }
}