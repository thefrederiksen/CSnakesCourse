using Microsoft.AspNetCore.Components;
using FaceVault.Services;

namespace FaceVault.Pages;

public partial class Index : ComponentBase, IDisposable
{
    [Inject] protected IMemoryService MemoryService { get; set; } = default!;
    [Inject] protected IImageService ImageService { get; set; } = default!;

    private MemoryCollection? todaysMemories;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private DateTime currentDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        await LoadTodaysMemories();
    }

    private async Task LoadTodaysMemories()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;
            StateHasChanged();

            Logger.Info($"Loading memories for {currentDate:MMMM d}");
            todaysMemories = await MemoryService.GetTodaysMemoriesAsync(currentDate);
            
            Logger.Info($"Loaded {todaysMemories.TotalPhotos} photos across {todaysMemories.YearGroups.Count} years");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading today's memories: {ex.Message}");
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
        if (currentDate.Date == DateTime.Today)
            return "Photos taken on this day in previous years";
        
        var daysAgo = (DateTime.Today - currentDate.Date).Days;
        return $"Photos from {daysAgo} day{(daysAgo == 1 ? "" : "s")} ago";
    }


    public void Dispose()
    {
        // No cleanup needed
    }
}