using Microsoft.AspNetCore.Components;
using FaceVault.Services;

namespace FaceVault.Pages;

public partial class Index : ComponentBase, IDisposable
{
    [Inject] protected IMemoryService MemoryService { get; set; } = default!;
    [Inject] protected IImageService ImageService { get; set; } = default!;
    [Inject] protected IFileOpenService FileOpenService { get; set; } = default!;

    private MemoryCollection? todaysMemories;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private DateTime currentDate = DateTime.Today;
    private bool excludeScreenshots = true;

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
            todaysMemories = await MemoryService.GetTodaysMemoriesAsync(currentDate, excludeScreenshots);
            
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

    private async Task ToggleScreenshotFilter()
    {
        excludeScreenshots = !excludeScreenshots;
        await LoadTodaysMemories();
    }

    private void OpenImage(Models.Image image)
    {
        if (image != null && !string.IsNullOrEmpty(image.FilePath))
        {
            Logger.Info($"Opening image: {image.FilePath}");
            FileOpenService.OpenInDefaultViewer(image.FilePath);
        }
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
        var baseSubtitle = currentDate.Date == DateTime.Today 
            ? "Photos taken on this day in previous years"
            : $"Photos from {(DateTime.Today - currentDate.Date).Days} day{((DateTime.Today - currentDate.Date).Days == 1 ? "" : "s")} ago";
            
        return excludeScreenshots 
            ? $"{baseSubtitle} (screenshots excluded)"
            : baseSubtitle;
    }


    public void Dispose()
    {
        // No cleanup needed
    }
}