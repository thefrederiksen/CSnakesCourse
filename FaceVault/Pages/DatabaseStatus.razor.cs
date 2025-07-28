using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using FaceVault.Data;
using FaceVault.Services;

namespace FaceVault.Pages;

public partial class DatabaseStatus : ComponentBase, IDisposable
{
    [Inject] protected FaceVaultDbContext DbContext { get; set; } = default!;
    [Inject] protected IDatabaseHealthService DatabaseHealthService { get; set; } = default!;
    [Inject] protected IDatabaseStatsService DatabaseStatsService { get; set; } = default!;
    [Inject] protected IDatabaseSyncService DatabaseSyncService { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected ILogger<DatabaseStatus> Logger { get; set; } = default!;

    private bool isLoading = true;
    private bool canConnect = false;
    private bool databaseExists = false;
    private bool migrationsApplied = false;
    private string databasePath = "";
    private string databaseSizeFormatted = "0 B";
    private Dictionary<string, int> tableCounts = new();
    private IEnumerable<string> pendingMigrations = new List<string>();
    private string schemaVersion = "Unknown";
    private string connectionError = "";
    private DateTime lastUpdated = DateTime.Now;
    private double lastQueryTime = 0;
    private double statusCheckTime = 0;
    private string statusMessage = "";
    private string statusMessageType = "info";
    
    // Auto-refresh functionality
    private bool autoRefresh = false;
    private int autoRefreshInterval = 5; // seconds
    private Timer? refreshTimer;

    protected override async Task OnInitializedAsync()
    {
        await RefreshStatus();
    }

    private async Task RefreshStatus()
    {
        isLoading = true;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            connectionError = "";
            
            // Clear any cached data by forcing Entity Framework to reload
            DbContext.ChangeTracker.Clear();
            
            // Test database connection
            canConnect = await TestDatabaseConnection();
            
            if (canConnect)
            {
                // Get database info
                await GetDatabaseInfo();
                
                // Get fresh table counts directly from database
                await GetTableCounts();
                
                // Check migrations
                await CheckMigrations();
                
                Logger.LogDebug("DatabaseStatus: Refresh completed - Images count: {ImageCount}", tableCounts.GetValueOrDefault("Images", 0));
            }
        }
        catch (Exception ex)
        {
            connectionError = $"{ex.GetType().Name}: {ex.Message}";
            canConnect = false;
            
            // Log detailed error information
            Logger.LogError(ex, "Database Status page connection error: {Message}", ex.Message);
            Logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
            Logger.LogError("Exception Details: {Exception}", ex);
            Logger.LogError("Inner Exception: {InnerException}", ex.InnerException?.Message ?? "None");
        }
        finally
        {
            stopwatch.Stop();
            statusCheckTime = stopwatch.ElapsedMilliseconds;
            lastUpdated = DateTime.Now;
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task<bool> TestDatabaseConnection()
    {
        try
        {
            var queryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await DbContext.Database.CanConnectAsync();
            queryStopwatch.Stop();
            lastQueryTime = queryStopwatch.ElapsedMilliseconds;
            return true;
        }
        catch (Exception ex)
        {
            connectionError = $"{ex.GetType().Name}: {ex.Message}";
            
            // Log detailed connection test error
            Logger.LogError(ex, "Database connection test failed: {Message}", ex.Message);
            Logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
            Logger.LogError("Inner Exception: {InnerException}", ex.InnerException?.Message ?? "None");
            
            return false;
        }
    }

    private Task GetDatabaseInfo()
    {
        try
        {
            var connectionString = DbContext.Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                databasePath = connectionString.Split("Data Source=")[1].Split(';')[0];
                databaseExists = File.Exists(databasePath);
                
                if (databaseExists)
                {
                    var fileInfo = new FileInfo(databasePath);
                    var sizeBytes = fileInfo.Length;
                    databaseSizeFormatted = FormatFileSize(sizeBytes);
                }
            }
        }
        catch (Exception ex)
        {
            connectionError = $"Database info error: {ex.Message}";
        }
        
        return Task.CompletedTask;
    }

    private async Task GetTableCounts()
    {
        try
        {
            // Always get fresh counts directly from database - no caching
            Logger.LogDebug("DatabaseStatus: Getting fresh counts directly from database...");
            
            // Clear any tracked entities to ensure fresh data
            DbContext.ChangeTracker.Clear();
            
            // Get all counts directly from database using AsNoTracking for accuracy
            var totalImages = await DbContext.Images.AsNoTracking().CountAsync();
            var processedImages = await DbContext.Images.AsNoTracking().CountAsync(i => i.IsProcessed);
            var imagesWithFaces = await DbContext.Images.AsNoTracking().CountAsync(i => i.HasFaces);
            var screenshots = await DbContext.Images.AsNoTracking().CountAsync(i => i.IsScreenshot);
            var totalPeople = await DbContext.People.AsNoTracking().CountAsync();
            var totalFaces = await DbContext.Faces.AsNoTracking().CountAsync();
            var totalTags = await DbContext.Tags.AsNoTracking().CountAsync();
            var totalImageTags = await DbContext.ImageTags.AsNoTracking().CountAsync();
            var totalSettings = await DbContext.AppSettings.AsNoTracking().CountAsync();
            
            // Log the actual counts
            Logger.LogDebug("DatabaseStatus: Direct database counts - Images: {TotalImages}, People: {TotalPeople}, Faces: {TotalFaces}", totalImages, totalPeople, totalFaces);
            
            // Calculate derived counts
            var unprocessedImages = totalImages - processedImages;
            var imagesWithoutFaces = totalImages - imagesWithFaces;
            
            // Display only the actual database counts (no cached or stats service data)
            tableCounts = new Dictionary<string, int>
            {
                ["Images"] = totalImages,
                ["People"] = totalPeople,
                ["Faces"] = totalFaces,
                ["Tags"] = totalTags,
                ["ImageTags"] = totalImageTags,
                ["Settings"] = totalSettings,
                ["Processed Images"] = processedImages,
                ["Images with Faces"] = imagesWithFaces,
                ["Screenshots"] = screenshots,
                ["Unprocessed Images"] = unprocessedImages
            };
        }
        catch (Exception ex)
        {
            tableCounts = new Dictionary<string, int>
            {
                ["Images"] = 0,
                ["People"] = 0,
                ["Faces"] = 0,
                ["Tags"] = 0,
                ["ImageTags"] = 0,
                ["Settings"] = 0
            };
            connectionError = $"Table count error: {ex.Message}";
        }
    }

    private async Task CheckMigrations()
    {
        try
        {
            pendingMigrations = await DbContext.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await DbContext.Database.GetAppliedMigrationsAsync();
            migrationsApplied = appliedMigrations.Any();
            schemaVersion = appliedMigrations.LastOrDefault() ?? "None";
        }
        catch (Exception ex)
        {
            connectionError = $"Migration check error: {ex.Message}";
        }
    }

    private async Task InitializeDatabase()
    {
        try
        {
            var created = await DbContext.EnsureDatabaseCreatedAsync();
            if (created)
            {
                ShowStatusMessage("Database initialized successfully!", "success");
            }
            else
            {
                ShowStatusMessage("Database already exists", "info");
            }
            await RefreshStatus();
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error initializing database: {ex.Message}", "danger");
        }
    }

    private async Task ApplyMigrations()
    {
        try
        {
            await DbContext.Database.MigrateAsync();
            ShowStatusMessage("Migrations applied successfully!", "success");
            await RefreshStatus();
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error applying migrations: {ex.Message}", "danger");
        }
    }

    private async Task TestConnection()
    {
        var canConnect = await TestDatabaseConnection();
        var message = canConnect ? "Database connection successful!" : $"Connection failed: {connectionError}";
        var type = canConnect ? "success" : "danger";
        ShowStatusMessage(message, type);
    }

    private Task ViewSchema()
    {
        ShowStatusMessage("Schema viewer not implemented yet", "info");
        return Task.CompletedTask;
    }

    private async Task SeedTestData()
    {
        try
        {
            // Add a few test tags if none exist
            if (!await DbContext.Tags.AnyAsync())
            {
                ShowStatusMessage("No additional test data to seed - default tags already exist", "info");
            }
            else
            {
                ShowStatusMessage("Test data seeding not implemented yet", "info");
            }
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error seeding test data: {ex.Message}", "danger");
        }
    }

    private async Task RepairDatabase()
    {
        try
        {
            ShowStatusMessage("Repairing database... This may take a moment.", "warning");
            var success = await DatabaseHealthService.RepairDatabaseAsync();
            
            if (success)
            {
                ShowStatusMessage("Database repaired successfully!", "success");
                await RefreshStatus();
            }
            else
            {
                ShowStatusMessage("Database repair failed. Check logs for details.", "danger");
            }
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error repairing database: {ex.Message}", "danger");
        }
    }

    private async Task ShowCountReport()
    {
        try
        {
            var report = await DatabaseSyncService.GetDetailedCountReportAsync();
            ShowStatusMessage($"Count Report Generated - Check logs for details", "info");
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error generating count report: {ex.Message}", "danger");
        }
    }

    private void ShowStatusMessage(string message, string type)
    {
        statusMessage = message;
        statusMessageType = type;
        StateHasChanged();
    }

    private void ClearStatusMessage()
    {
        statusMessage = "";
        StateHasChanged();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }

    private void OnAutoRefreshChanged(bool value)
    {
        autoRefresh = value;
        if (autoRefresh)
        {
            StartAutoRefresh();
        }
        else
        {
            StopAutoRefresh();
        }
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh(); // Stop any existing timer
        refreshTimer = new Timer(async _ => await InvokeAsync(async () => await RefreshStatus()), 
            null, TimeSpan.FromSeconds(autoRefreshInterval), TimeSpan.FromSeconds(autoRefreshInterval));
    }

    private void StopAutoRefresh()
    {
        refreshTimer?.Dispose();
        refreshTimer = null;
    }

    private string GetCardClass(string tableName)
    {
        return tableName switch
        {
            "Images" => "border-primary",
            "People" => "border-success", 
            "Faces" => "border-info",
            "Processed Images" => "border-warning",
            "Images with Faces" => "border-secondary",
            _ => "border-light"
        };
    }

    private string GetTextClass(string tableName)
    {
        return tableName switch
        {
            "Images" => "text-primary",
            "People" => "text-success",
            "Faces" => "text-info", 
            "Processed Images" => "text-warning",
            "Images with Faces" => "text-secondary",
            _ => "text-muted"
        };
    }

    /// <summary>
    /// Forces an immediate refresh of database counts - can be called from other pages
    /// </summary>
    public async Task ForceRefreshAsync()
    {
        await RefreshStatus();
    }

    /// <summary>
    /// Gets the current image count from the database
    /// </summary>
    public async Task<int> GetCurrentImageCountAsync()
    {
        try
        {
            DbContext.ChangeTracker.Clear();
            return await DbContext.Images.AsNoTracking().CountAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting current image count: {Message}", ex.Message);
            return 0;
        }
    }

    public void Dispose()
    {
        StopAutoRefresh();
    }
}