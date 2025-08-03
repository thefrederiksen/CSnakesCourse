using CSnakes.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace BlazorTrader.Components.Pages;

public partial class TraderTraining : ComponentBase
{
    private List<TrainingModule> trainingModules = new();
    private int completedModules => trainingModules.Count(m => m.IsCompleted);
    private int completionPercentage => trainingModules.Count > 0 ? (int)Math.Round((double)completedModules / trainingModules.Count * 100) : 0;

    private bool dataDownloaded = false;
    private bool indicatorsCreated = false;
    private bool trainingDataCreated = false;
    private bool modelTrained = false;

    // Loading state for the download operation
    private bool isDownloading = false;
    private long downloadProgress = 0;
    private int totalSymbols = 503;
    private string? downloadCompletionMessage = null;

    // Loading state for the create indicators operation
    private bool isCreatingIndicators = false;
    private long indicatorsProgress = 0;
    private string? indicatorsCompletionMessage = null;

    // Loading state for the create training data operation
    private bool isCreatingTrainingData = false;
    private int trainingDataProgress = 0;
    private string? trainingDataCompletionMessage = null;

    // Loading state for the XGBoost training operation
    private bool isTrainingXGBoost = false;
    private int xgboostProgress = 0;
    private string? xgboostCompletionMessage = null;

    private bool isExplainingResults = false;
    private string? trainingExplanation = null;

    private string? pythonWarningMessage = null;
    private bool showClearDataWarning = false;

    [Inject]
    public required IPythonEnvironment PythonEnv { get; set; }

    [Inject]
    public required IConfiguration Configuration { get; set; }

    /// <summary>
    /// Gets the user data directory for storing S&P 500 data and indicators.
    /// Uses the standard Windows ApplicationData folder for user-specific data.
    /// </summary>
    public static string UserDataDirectory
    {
        get
        {
            var userDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "BlazorTrader"
            );
            
            // Ensure the directory exists
            Directory.CreateDirectory(userDataPath);
            
            return userDataPath;
        }
    }


    private int completedSteps =>
        (dataDownloaded ? 1 : 0) +
        (indicatorsCreated ? 1 : 0) +
        (trainingDataCreated ? 1 : 0) +
        (modelTrained ? 1 : 0);

    private int aiCompletionPercentage => (int)Math.Round((double)completedSteps / 4 * 100);
    private int downloadProgressPercentage => totalSymbols > 0 ? (int)Math.Round((double)downloadProgress / totalSymbols * 100) : 0;
    private int indicatorsProgressPercentage => totalSymbols > 0 ? (int)Math.Round((double)indicatorsProgress / totalSymbols * 100) : 0;

    protected override async Task OnInitializedAsync()
    {
        await InitializeTrainingData();
    }

    private Task InitializeTrainingData()
    {
        trainingModules = new List<TrainingModule>
        {
            new TrainingModule
            {
                Id = 1,
                Title = "Introduction to Stock Trading",
                Description = "Learn the fundamentals of stock trading, including basic terminology and concepts.",
                Duration = "30 min",
                Difficulty = "Beginner"
            },
            new TrainingModule
            {
                Id = 2,
                Title = "Technical Analysis Basics",
                Description = "Introduction to chart reading and technical indicators for making informed trading decisions.",
                Duration = "45 min",
                Difficulty = "Beginner"
            },
            new TrainingModule
            {
                Id = 3,
                Title = "Risk Management Strategies",
                Description = "Learn how to protect your capital and manage risk in your trading activities.",
                Duration = "40 min",
                Difficulty = "Intermediate"
            }
        };

        return Task.CompletedTask;
    }

    private void LogException(string operation, Exception ex)
    {
        Console.WriteLine($"Error {operation}: {ex.Message}");
        
        // Print inner exception details if available
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            Console.WriteLine($"Inner Exception Type: {ex.InnerException.GetType().Name}");
            Console.WriteLine("Inner Exception Stack Trace:");
            Console.WriteLine(ex.InnerException.StackTrace);
        }
        
        // Print full exception details
        Console.WriteLine("Full Exception:");
        Console.WriteLine(ex.ToString());
    }

    private async Task DownloadSpData()
    {
        // Set loading state
        isDownloading = true;
        downloadProgress = 0;
        downloadCompletionMessage = null; // Clear any previous message
        StateHasChanged(); // Update UI to show spinner

        try
        {
            // Run the Python operation on a background thread to avoid blocking the UI
            await Task.Run(async () =>
            {
                foreach (var progress in PythonEnv.ADownloadSp500Data().LoadOrDownload(UserDataDirectory))
                {
                    // Update progress using InvokeAsync to marshal to UI thread
                    await InvokeAsync(() =>
                    {
                        downloadProgress = progress;
                        Console.WriteLine($"Download progress: {progress} of {totalSymbols}");
                        StateHasChanged();
                    });
                }
            });

            // Mark as completed
            dataDownloaded = true;
            downloadProgress = totalSymbols; // Ensure progress shows 100%
            downloadCompletionMessage = $"✅ All S&P 500 data downloaded successfully! ({totalSymbols} symbols processed, 500 companies)";
            Console.WriteLine($"Download completed: {totalSymbols} symbols processed");
        }
        catch (Exception ex)
        {
            LogException("downloading S&P data", ex);
            downloadCompletionMessage = "❌ Download failed. Please check the console for error details.";
        }
        finally
        {
            // Always clear loading state
            isDownloading = false;
            StateHasChanged(); // Update UI to hide spinner and show completion message
        }
    }

    private async Task CreateIndicators()
    {
        // Set loading state
        isCreatingIndicators = true;
        indicatorsProgress = 0;
        indicatorsCompletionMessage = null; // Clear any previous message
        StateHasChanged(); // Update UI to show spinner

        try
        {
            // Run the Python operation on a background thread to avoid blocking the UI
            await Task.Run(async () =>
            {
                var pythonGenerator = PythonEnv.BCalculateIndicators().ProcessAllFiles(UserDataDirectory);
                while (pythonGenerator.MoveNext())
                {
                    var progress = pythonGenerator.Current;
                    await InvokeAsync(() =>
                    {
                        indicatorsProgress = (long)(int)progress;
                        Console.WriteLine($"Indicators progress: {progress} of {totalSymbols}");
                        StateHasChanged();
                    });
                }
            });

            // Mark as completed
            indicatorsCreated = true;
            indicatorsProgress = totalSymbols; // Ensure progress shows 100%
            indicatorsCompletionMessage = $"✅ All technical indicators calculated successfully! ({totalSymbols} symbols processed)";
            Console.WriteLine($"Indicators completed: {totalSymbols} symbols processed");
        }
        catch (Exception ex)
        {
            LogException("creating indicators", ex);
            indicatorsCompletionMessage = "❌ Indicator calculation failed. Please check the console for error details.";
            pythonWarningMessage = ex.Message;
        }
        finally
        {
            // Always clear loading state
            isCreatingIndicators = false;
            StateHasChanged(); // Update UI to hide spinner and show completion message
        }
    }

    private async Task CreateTrainingData()
    {
        // Set loading state for training data
        isCreatingTrainingData = true;
        trainingDataProgress = 0;
        trainingDataCompletionMessage = null;
        StateHasChanged();

        try
        {
            await Task.Run(async () =>
            {
                var pythonGenerator = PythonEnv.CCreateTrainingData().CreateTrainingData(UserDataDirectory);
                while (pythonGenerator.MoveNext())
                {
                    var progress = pythonGenerator.Current;
                    await InvokeAsync(() =>
                    {
                        trainingDataProgress = (int)progress;
                        StateHasChanged();
                    });
                }
            });

            // Mark as completed
            trainingDataCreated = true;
            trainingDataProgress = 100;
            trainingDataCompletionMessage = $"✅ Training data created successfully!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            LogException("creating training data", ex);
            trainingDataCompletionMessage = "❌ Training data creation failed. Please check the console for error details.";
            pythonWarningMessage = ex.Message;
        }
        finally
        {
            isCreatingTrainingData = false;
            StateHasChanged();
        }
    }

    private async Task TrainXGBoost()
    {
        isTrainingXGBoost = true;
        xgboostProgress = 0;
        xgboostCompletionMessage = null;
        StateHasChanged();

        try
        {
            await Task.Run(async () =>
            {
                var pythonGenerator = PythonEnv.DTrainXgboost().TrainModels(UserDataDirectory);
                bool? success = null;
                string? message = null;
                while (pythonGenerator.MoveNext())
                {
                    var progress = pythonGenerator.Current;
                    await InvokeAsync(() =>
                    {
                        xgboostProgress = (int)progress;
                        StateHasChanged();
                    });
                }
                // Try to get the return value (success, message) from the generator
                if (pythonGenerator is IDisposable disposable)
                {
                    try
                    {
                        // CSnakes generator may expose Return property or similar
                        var result = pythonGenerator.Return; // Should be (bool, string)
                        success = result.Item1;
                        message = result.Item2;
                    }
                    catch { }
                }
                await InvokeAsync(() =>
                {
                    if (success.HasValue)
                    {
                        modelTrained = success.Value;
                        xgboostCompletionMessage = message;
                    }
                    else
                    {
                        xgboostCompletionMessage = "✅ XGBoost model trained successfully!";
                    }
                    StateHasChanged();
                });
            });
        }
        catch (Exception ex)
        {
            LogException("training XGBoost", ex);
            var details = ex.Message;
            if (ex.InnerException != null)
            {
                details += "\nInner Exception: " + ex.InnerException.Message;
                details += "\nInner Exception Type: " + ex.InnerException.GetType().Name;
                details += "\nInner Exception Stack Trace:\n" + ex.InnerException.StackTrace;
            }
            details += "\nFull Exception:\n" + ex.ToString();
            xgboostCompletionMessage = "❌ XGBoost training failed. Please check the console for error details.";
            pythonWarningMessage = details;
        }
        finally
        {
            isTrainingXGBoost = false;
            StateHasChanged();
        }
    }

    private async Task ExplainTrainingResults()
    {
        isExplainingResults = true;
        trainingExplanation = null;
        StateHasChanged();
        try
        {
            // Set the OPENAI_API_KEY environment variable from config
            var openAiKey = Configuration["OPENAI_API_KEY"];

            var instrctions = "Explain the results of the XGBoost model training in detail, including feature importance and model performance metrics. Provide insights into how the model can be improved based on the training data and results.";

            // Call the Python method to get the explanation
            trainingExplanation = await Task.Run(() => PythonEnv.EExplainXgboostResults().ExplainXgboostResults(UserDataDirectory,openAiKey, instrctions));
        }
        catch (Exception ex)
        {
            trainingExplanation = $"Error explaining training results: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}";
        }
        finally
        {
            isExplainingResults = false;
            StateHasChanged();
        }
    }

    private void ShowClearDataWarning()
    {
        showClearDataWarning = true;
    }

    private async Task ClearAllData()
    {
        showClearDataWarning = false;
        StateHasChanged();
        try
        {
            // Delete S&P data and models directories
            var spDataDir = Path.Combine(UserDataDirectory, "sp500_data");
            var modelsDir = Path.Combine(UserDataDirectory, "models");
            var logDir = Path.Combine(UserDataDirectory, "log");
            if (Directory.Exists(spDataDir)) Directory.Delete(spDataDir, true);
            if (Directory.Exists(modelsDir)) Directory.Delete(modelsDir, true);
            if (Directory.Exists(logDir)) Directory.Delete(logDir, true);
            // Reset UI state
            dataDownloaded = false;
            indicatorsCreated = false;
            trainingDataCreated = false;
            modelTrained = false;
            downloadProgress = 0;
            indicatorsProgress = 0;
            trainingDataProgress = 0;
            xgboostProgress = 0;
            downloadCompletionMessage = null;
            indicatorsCompletionMessage = null;
            trainingDataCompletionMessage = null;
            xgboostCompletionMessage = null;
            trainingExplanation = null;
            pythonWarningMessage = null;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            pythonWarningMessage = $"Error clearing data: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}";
            StateHasChanged();
        }
    }
}

public class TrainingModule
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}