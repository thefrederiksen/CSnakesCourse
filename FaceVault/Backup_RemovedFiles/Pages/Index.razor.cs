using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using FaceVault.Services;

namespace FaceVault.Pages;

public partial class Index : ComponentBase, IDisposable
{
    [Inject] protected IMemoryService MemoryService { get; set; } = default!;
    [Inject] protected ILogger<Index> Logger { get; set; } = default!;
    [Inject] protected ISettingsService SettingsService { get; set; } = default!;

    private MemoryCollection? todaysMemories;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private DateTime currentDate = DateTime.Today;
    private bool excludeScreenshots = true;

    protected override async Task OnInitializedAsync()
    {
        // Load settings to check if we should exclude screenshots
        var settings = await SettingsService.GetSettingsAsync();
        excludeScreenshots = settings.ExcludeScreenshotsFromMemories;
        
        await LoadTodaysMemories();
    }

    private async Task LoadTodaysMemories()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;
            StateHasChanged();

            Logger.LogInformation("Loading memories for {Date:MMMM d} (excluding screenshots: {ExcludeScreenshots})", currentDate, excludeScreenshots);
            todaysMemories = await MemoryService.GetTodaysMemoriesAsync(currentDate, excludeScreenshots);
            
            Logger.LogInformation("Loaded {TotalPhotos} photos across {YearGroups} years", todaysMemories.TotalPhotos, todaysMemories.YearGroups.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading today's memories: {Message}", ex.Message);
            errorMessage = $"Unable to load memories: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }


    private async Task RefreshMemories()
    {
        await LoadTodaysMemories();
    }

    private async Task LoadPreviousDay()
    {
        currentDate = currentDate.AddDays(-1);
        await LoadTodaysMemories();
    }

    private async Task LoadNextDay()
    {
        if (currentDate < DateTime.Today)
        {
            currentDate = currentDate.AddDays(1);
            await LoadTodaysMemories();
        }
    }

    private async Task LoadToday()
    {
        currentDate = DateTime.Today;
        await LoadTodaysMemories();
    }

    // Image functionality is now handled by the ImageViewer component

    private string GetPageTitle()
    {
        if (currentDate.Date == DateTime.Today)
            return "FaceVault - Today's Memories";
        
        return $"FaceVault - Memories from {currentDate:MMMM d}";
    }

    private string GetDisplayTitle()
    {
        if (currentDate.Date == DateTime.Today)
            return "Today's Memories";
        
        return $"Memories from {currentDate:MMMM d}";
    }

    private string GetSubtitle()
    {
        var baseSubtitle = currentDate.Date == DateTime.Today 
            ? "Photos taken on this day in previous years"
            : $"Photos from {(DateTime.Today - currentDate.Date).Days} day{((DateTime.Today - currentDate.Date).Days == 1 ? "" : "s")} ago";
            
        return $"{baseSubtitle} (screenshots excluded)";
    }


    public void Dispose()
    {
        // No cleanup needed
    }
}