using BlazorTrader.Models;
using CSnakes.Runtime;
using CSnakes.Runtime.Python;
using Microsoft.AspNetCore.Components;
using System.Text.Json;


namespace BlazorTrader.Components.Pages;

public record PredictionRecord
{
    public string Symbol { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string Prediction { get; init; } = string.Empty;
    public double? LatestPrice { get; set; }
}

public class TradingPlanData
{
    public string Instructions { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public partial class SundayTrades : ComponentBase, IDisposable
{
    private int totalTrades = 0;
    private double successRate;
    private bool showAddTradeForm = false;
    private string newTradeSymbol = "";
    private int newTradeQuantity = 0;
    private double newTradePrice = 0;
    private List<Trade> trades = new();
    
    // Symbol dropdown search functionality
    private bool showSymbolDropdown = false;
    private List<string> filteredSymbols = new();
    private Timer? hideDropdownTimer;
    
    private string? userMessage;
    private bool isRunningPredictions = false;
    private int predictionsProgress = 0;
    private string? predictionsCompletionMessage = null;
    private List<PredictionRecord>? _allPredictions = null;
    List<PredictionRecord> _mustBuyList = new();
    private string _tradingPlanInstructions = string.Empty;

    private const string TradesFilePath = "wwwroot/data/sundaytrades.json";

    [Inject]
    public required IPythonEnvironment PythonEnv { get; set; }

    [Inject]
    public required IConfiguration Configuration { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadTradeDataAsync();
        FilterSymbols(); // Initialize filtered symbols
        await LoadSavedPredictionsAsync();
        await LoadTradingPlanInstructionsAsync();
        await LoadLatestPricesForTradesAsync();
    }

    private async Task LoadTradeDataAsync()
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(TradesFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if the file exists
            if (File.Exists(TradesFilePath))
            {
                // Read the JSON file
                var json = await File.ReadAllTextAsync(TradesFilePath);
                if (!string.IsNullOrEmpty(json))
                {
                    // Deserialize the JSON to a list of trades
                    trades = JsonSerializer.Deserialize<List<Trade>>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Trade>();
                }
            }

            // Calculate statistics
            await UpdateStatisticsAsync();
        }
        catch (Exception)
        {
            // Handle any errors (could add logging here)
            trades = new List<Trade>();
            totalTrades = 0;
            successRate = 0;
        }
    }

    private void AddTrade()
    {
        showAddTradeForm = true;
        newTradeSymbol = "";
        FilterSymbols();
    }

    private async Task SaveTradeAsync()
    {
        // Validate the trade information
        if (!string.IsNullOrEmpty(newTradeSymbol) && newTradeQuantity > 0 && newTradePrice > 0)
        {
            // Create a new trade
            var trade = new Trade
            {
                Symbol = newTradeSymbol.ToUpper(), // Ensure symbol is uppercase
                Quantity = newTradeQuantity,
                Price = newTradePrice,
                CreatedDate = DateTime.Now,
                IsSundayTrade = true,
                IsBuy = true // Default to buy trade
            };

            // Add to the list
            trades.Add(trade);
            
            // Save to file
            await SaveTradesToFileAsync();
            
            // Update statistics
            await UpdateStatisticsAsync();

            // Reset form
            newTradeSymbol = "";
            newTradeQuantity = 0;
            newTradePrice = 0;
            showAddTradeForm = false;
            showSymbolDropdown = false;
        }
    }

    private async Task RemoveTradeAsync(Guid tradeId)
    {
        // Find and remove the trade
        var tradeToRemove = trades.FirstOrDefault(t => t.Id == tradeId);
        if (tradeToRemove != null)
        {
            trades.Remove(tradeToRemove);
            
            // Save to file
            await SaveTradesToFileAsync();
            
            // Update statistics
            await UpdateStatisticsAsync();
            
            // Refresh the UI
            StateHasChanged();
        }
    }

    private Task UpdateStatisticsAsync()
    {
        totalTrades = trades.Count;
        if (totalTrades > 0)
        {
            var successfulTrades = trades.Count(t => t.TotalValue > 0);
            successRate = Math.Round((double)successfulTrades / totalTrades * 100, 1);
        }
        else
        {
            successRate = 0;
        }
        
        return Task.CompletedTask;
    }

    private async Task SaveTradesToFileAsync()
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(TradesFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize trades to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, // Makes the JSON more readable
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(trades, jsonOptions);
            
            // Write to file
            await File.WriteAllTextAsync(TradesFilePath, json);
        }
        catch (Exception)
        {
            // Handle any errors (could add logging here)
        }
    }

    private void CancelAddTrade()
    {
        showAddTradeForm = false;
        newTradeSymbol = "";
        newTradeQuantity = 0;
        newTradePrice = 0;
        showSymbolDropdown = false;
    }

    private async Task RefreshDataAsync()
    {
        await LoadTradeDataAsync();
        StateHasChanged();
    }

    private async Task LoadSavedPredictionsAsync()
    {
        try
        {
            var predictionsFilePath = Path.Combine(TraderTraining.UserDataDirectory, "logs", "predictions.json");
            if (File.Exists(predictionsFilePath))
            {
                var json = await File.ReadAllTextAsync(predictionsFilePath);
                if (!string.IsNullOrEmpty(json))
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    _allPredictions = JsonSerializer.Deserialize<List<PredictionRecord>>(json, jsonOptions) ?? new List<PredictionRecord>();

                    // Create mustBuyList from loaded predictions
                    CreateMustBuyList();

                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            // Handle any errors silently - predictions are optional
            Console.WriteLine($"Error loading saved predictions: {ex.Message}");
        }
    }

    private void CreateMustBuyList()
    {
        if ( _allPredictions == null)
        {
            _mustBuyList = new List<PredictionRecord>();
            return;
        }

        _mustBuyList = _allPredictions
            .Where(pr => pr.Prediction == "Must Buy")
            .ToList();


        // We now need to fill in latest prices for the must buy list
        foreach (PredictionRecord record in _mustBuyList)
        {
            // Get the latest price for the symbol
            var latestPriceReturn = PythonEnv.GLlmKnowledge().LookupLatestPrice(TraderTraining.UserDataDirectory, record.Symbol);
            string iso = latestPriceReturn.Item1.GetAttr("isoformat").Call().As<string>();
            DateTime latestPriceDate = DateTime.Parse(iso);

            record.LatestPrice = latestPriceReturn.Item2;
        }
    }

    private async Task LoadLatestPricesForTradesAsync()
    {
        try
        {
            foreach (var trade in trades)
            {
                if (string.IsNullOrEmpty(trade.Symbol))
                    continue;
                    
                try
                {
                    // Call Python function to get latest price
                    (PyObject, double) result = PythonEnv.GLlmKnowledge().LookupLatestPrice(TraderTraining.UserDataDirectory, trade.Symbol);
                    // result is a tuple of (date, price)
                    var price = result.Item2; // Get the price from the tuple
                    trade.LatestPrice = price;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting latest price for {trade.Symbol}: {ex.Message}");
                    trade.LatestPrice = null;
                }
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading latest prices: {ex.Message}");
        }
    }

    private async Task LoadTradingPlanInstructionsAsync()
    {
        try
        {
            var tradingPlanFilePath = Path.Combine(TraderTraining.UserDataDirectory, "logs", "tradingplan.json");
            if (File.Exists(tradingPlanFilePath))
            {
                var json = await File.ReadAllTextAsync(tradingPlanFilePath);
                if (!string.IsNullOrEmpty(json))
                {
                    var tradingPlanData = JsonSerializer.Deserialize<TradingPlanData>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    _tradingPlanInstructions = tradingPlanData?.Instructions ?? string.Empty;
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading trading plan instructions: {ex.Message}");
        }
    }

    private async Task SaveTradingPlanInstructionsAsync()
    {
        try
        {
            var logDir = Path.Combine(TraderTraining.UserDataDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            var tradingPlanFilePath = Path.Combine(logDir, "tradingplan.json");
            var tradingPlanData = new TradingPlanData
            {
                Instructions = _tradingPlanInstructions,
                LastUpdated = DateTime.Now
            };
            
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(tradingPlanData, jsonOptions);
            await File.WriteAllTextAsync(tradingPlanFilePath, json);
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving trading plan instructions: {ex.Message}");
        }
    }

    private void OnInstructionsChanged()
    {
        StateHasChanged();
    }

    private string GatherKnowledge()
    {
        var knowledge = new System.Text.StringBuilder();
        
        // Header
        knowledge.AppendLine("=== TRADING KNOWLEDGE BASE ===");
        knowledge.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        knowledge.AppendLine();
        
        // Existing Trades Section
        knowledge.AppendLine("## EXISTING TRADES");
        knowledge.AppendLine($"Total trades: {trades.Count}");
        if (trades.Any())
        {
            knowledge.AppendLine("Trade History:");
            foreach (var trade in trades.OrderByDescending(t => t.CreatedDate))
            {
                knowledge.AppendLine($"- {trade.Symbol}: {trade.Quantity} shares @ ${trade.Price:F2} = ${trade.TotalValue:F2} ({trade.CreatedDate:MM/dd/yyyy HH:mm})");
            }
        }
        else
        {
            knowledge.AppendLine("No existing trades found.");
        }
        knowledge.AppendLine();
        
        // Must Buy Predictions Section
        knowledge.AppendLine("## MUST BUY PREDICTIONS");
        if (_mustBuyList != null && _mustBuyList.Any())
        {
            knowledge.AppendLine($"Total Must Buy signals: {_mustBuyList.Count}");
            knowledge.AppendLine("Current Must Buy recommendations:");
            foreach (var prediction in _mustBuyList.OrderBy(p => p.Symbol))
            {
                var priceInfo = prediction.LatestPrice.HasValue 
                    ? $"Current Price: ${prediction.LatestPrice.Value:F2}" 
                    : "Price: N/A";
                knowledge.AppendLine($"- {prediction.Symbol}: {prediction.Prediction} | {priceInfo} | Date: {prediction.Date:MM/dd/yyyy}");
            }
        }
        else
        {
            knowledge.AppendLine("No Must Buy predictions available.");
        }
        knowledge.AppendLine();
        
        // Trading Plan Instructions Section
        knowledge.AppendLine("## TRADING PLAN INSTRUCTIONS");
        if (!string.IsNullOrWhiteSpace(_tradingPlanInstructions))
        {
            knowledge.AppendLine("Current trading plan:");
            knowledge.AppendLine(_tradingPlanInstructions);
        }
        else
        {
            knowledge.AppendLine("No trading plan instructions available.");
        }
        knowledge.AppendLine();
        
        // Summary Section
        knowledge.AppendLine("## SUMMARY");
        knowledge.AppendLine($"- Total existing trades: {trades.Count}");
        knowledge.AppendLine($"- Total Must Buy signals: {_mustBuyList?.Count ?? 0}");
        knowledge.AppendLine($"- Has trading plan: {!string.IsNullOrWhiteSpace(_tradingPlanInstructions)}");
        knowledge.AppendLine();
        knowledge.AppendLine("=== END KNOWLEDGE BASE ===");
        
        return knowledge.ToString();
    }

    private string TradingPlanInstructions
    {
        get => _tradingPlanInstructions;
        set
        {
            _tradingPlanInstructions = value;
            OnInstructionsChanged();
        }
    }

    private async Task RunPredictionsAsync()
    {
        isRunningPredictions = true;
        predictionsProgress = 0;
        predictionsCompletionMessage = null;
        _allPredictions = null;
        StateHasChanged();

        // Check for required model file before running predictions
        var modelsDir = Path.Combine(TraderTraining.UserDataDirectory, "models");
        var modelPath = Path.Combine(modelsDir, "stage1_model.joblib");
        if (!System.IO.File.Exists(modelPath))
        {
            predictionsCompletionMessage = "❌ Required model file not found. Please train the model first.";
            isRunningPredictions = false;
            StateHasChanged();
            return;
        }

        try
        {
            await Task.Run(async () =>
            {
                var pythonGenerator = PythonEnv.DTrainXgboost().PredictLatestForAllSymbols(TraderTraining.UserDataDirectory);
                IReadOnlyList<IReadOnlyDictionary<string, PyObject>>? result = null;
                while (pythonGenerator.MoveNext())
                {
                    var progress = pythonGenerator.Current;
                    await InvokeAsync(() =>
                    {
                        predictionsProgress = (int)progress;
                        StateHasChanged();
                    });
                }
                // Try to get the return value (list of dicts) from the generator
                string? message = null;
                if (pythonGenerator is IDisposable disposable)
                {
                    try
                    {
                        result = pythonGenerator.Return; // Should be a list of dictionaries
                        message = "✅ Predictions completed and saved.";
                    }
                    catch { }
                }
                await InvokeAsync(() =>
                {
                    if (result != null)
                    {
                        List<PredictionRecord> parsedResults = result
                            .Select(dict => new PredictionRecord
                            {
                                Symbol = dict["Symbol"].As<string>(),
                                Date = DateTime.Parse(dict["Date"].As<string>()),
                                Prediction = dict["Prediction"].As<string>(),
                                LatestPrice = GetLatestPriceFromPyObject(dict["LatestPrice"])
                            })
                            .ToList();
                        _allPredictions = parsedResults;

                        // Save predictions to json file in Log directory
                        var logDir = Path.Combine(TraderTraining.UserDataDirectory, "logs");
                        if (!Directory.Exists(logDir))
                        {
                            Directory.CreateDirectory(logDir);
                        }
                        var predictionsFilePath = Path.Combine(logDir, "predictions.json");
                        var jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        var json = JsonSerializer.Serialize(parsedResults, jsonOptions);
                        File.WriteAllText(predictionsFilePath, json);

                        // Create mustBuyList 
                        CreateMustBuyList();
                    }
                    predictionsCompletionMessage = message ?? "✅ Predictions completed!";
                    StateHasChanged();
                });
            });
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ Prediction run failed: {ex}";
            predictionsCompletionMessage = errorMessage;
        }
        finally
        {
            isRunningPredictions = false;
            StateHasChanged();
        }
    }

    private static double? GetLatestPriceFromPyObject(PyObject pyObject)
    {
        try
        {
            // Check if the Python object is None
            if (pyObject.IsNone())
            {
                return null;
            }
            
            // Try to convert to double
            return pyObject.As<double>();
        }
        catch
        {
            // If conversion fails, return null
            return null;
        }
    }

    private string _tradingPlan = string.Empty;
    private bool _isGeneratingTradePlan = false;

    private async Task CreateTradePlanAsync()
    {
        _isGeneratingTradePlan = true;
        StateHasChanged();

        try
        {
            // Get the knowledge base as the prompt
            string prompt = GatherKnowledge();

            // Get the trading plan instructions as the system message
            string systemMessage = _tradingPlanInstructions;
            
            // TODO: Make LLM call here
            // For now, create a placeholder response
            await Task.Delay(2000); // Simulate LLM processing time

            // Use CallOpenaiModel wiyth o4 mini model
            // Set the OPENAI_API_KEY environment variable from config
            var openAiKey = Configuration["OpenAI:ApiKey"];

            // Show error if key is not set
            if (string.IsNullOrWhiteSpace(openAiKey))
            {
                _tradingPlan = "❌ missing OPENAI_API_KEY in configuration. Please set it to generate a trading plan.";
            }
            else
            {
                _tradingPlan = PythonEnv.GLlmKnowledge().CallOpenaiModel("o4-mini", _tradingPlan, prompt, openAiKey);
            }
        }
        catch (Exception ex)
        {
            _tradingPlan = $"Error generating trading plan: {ex}";
        }
        finally
        {
            _isGeneratingTradePlan = false;
            StateHasChanged();
        }
    }

    // Symbol search and dropdown methods
    private void FilterSymbols()
    {
        var allSymbols = GetSymbols();
        
        if (string.IsNullOrWhiteSpace(newTradeSymbol))
        {
            filteredSymbols = allSymbols.ToList();
        }
        else
        {
            filteredSymbols = allSymbols
                .Where(symbol => symbol.Contains(newTradeSymbol, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        StateHasChanged();
    }

    private void ShowSymbolDropdown()
    {
        // Cancel any pending hide timer
        hideDropdownTimer?.Dispose();
        
        showSymbolDropdown = true;
        FilterSymbols();
    }

    private void HideSymbolDropdownDelayed()
    {
        // Use a timer to delay hiding the dropdown to allow for click events
        hideDropdownTimer?.Dispose();
        hideDropdownTimer = new Timer(_ => 
        {
            InvokeAsync(() =>
            {
                showSymbolDropdown = false;
                StateHasChanged();
            });
        }, null, TimeSpan.FromMilliseconds(150), Timeout.InfiniteTimeSpan);
    }

    private void SelectSymbol(string symbol)
    {
        newTradeSymbol = symbol;
        showSymbolDropdown = false;
        StateHasChanged();
    }

    // Update the property to trigger filtering when the symbol changes
    private string NewTradeSymbol
    {
        get => newTradeSymbol;
        set
        {
            newTradeSymbol = value;
            FilterSymbols();
        }
    }

    /// <summary>
    /// Returns an array of stock symbols for trading
    /// </summary>
    public string[] GetSymbols()
    {
        var symbols = PythonEnv.ADownloadSp500Data().LoadSp500Symbols();

        return symbols.Order().ToArray();
    }

    public void Dispose()
    {
        hideDropdownTimer?.Dispose();
    }
}