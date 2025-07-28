using FaceVault.Services;
using FaceVault.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace FaceVault.Pages;

public partial class Duplicates : ComponentBase
{
    [Inject] private IDuplicateDetectionService DuplicateDetectionService { get; set; } = null!;
    [Inject] private IDuplicateCleanupService DuplicateCleanupService { get; set; } = null!;
    [Inject] private IFileOpenService FileOpenService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private ILogger<Duplicates> Logger { get; set; } = null!;

    private List<DuplicateGroup>? duplicateGroups;
    private bool isLoading = true;
    private bool isDeleting = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDuplicateGroups();
    }

    private async Task LoadDuplicateGroups()
    {
        try
        {
            isLoading = true;
            // Load ALL duplicate groups for cleanup, not just a page
            duplicateGroups = await DuplicateDetectionService.GetDuplicateGroupsAsync(0, 1000);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading duplicate groups: {Message}", ex.Message);
            duplicateGroups = new List<DuplicateGroup>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task DeleteAllRecommended()
    {
        if (duplicateGroups == null || !duplicateGroups.Any())
            return;

        var confirmMessage = $"⚠️ This will DELETE {duplicateGroups.Sum(g => g.Count - 1)} duplicate files and free up {FormatBytes(duplicateGroups.Sum(g => g.WastedSpace))} of storage space.\n\n" +
                           "✅ The OLDEST copy of each photo will be kept.\n" +
                           "🗑️ All other copies will be PERMANENTLY DELETED.\n\n" +
                           "Are you sure you want to continue?";

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", confirmMessage);
        if (!confirmed)
            return;

        try
        {
            isDeleting = true;
            StateHasChanged();

            var result = await DuplicateCleanupService.DeleteAllDuplicatesAsync();

            if (result.WasSuccessful)
            {
                Logger.LogInformation("Successfully deleted {FilesDeleted} duplicate files, freed {BytesFreed} of storage space", result.FilesDeleted, FormatBytes(result.BytesFreed));

                // Show success message
                await JSRuntime.InvokeVoidAsync("alert", 
                    $"✅ SUCCESS!\n\n" +
                    $"Deleted: {result.FilesDeleted} duplicate files\n" +
                    $"Freed: {FormatBytes(result.BytesFreed)} of storage space\n" +
                    $"Processed: {result.GroupsProcessed} duplicate groups\n" +
                    $"Duration: {result.Duration:mm\\:ss}\n\n" +
                    $"Your photo collection is now cleaned up!");
            }
            else
            {
                Logger.LogError("Duplicate cleanup completed with {ErrorCount} errors", result.ErrorCount);
                await JSRuntime.InvokeVoidAsync("alert", 
                    $"⚠️ PARTIALLY COMPLETED\n\n" +
                    $"Deleted: {result.FilesDeleted} files\n" +
                    $"Errors: {result.ErrorCount}\n" +
                    $"Some files could not be deleted.");
            }

            // Refresh the page to show updated results
            await LoadDuplicateGroups();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during bulk duplicate deletion: {Message}", ex.Message);
            await JSRuntime.InvokeVoidAsync("alert", $"❌ Error: {ex.Message}");
        }
        finally
        {
            isDeleting = false;
            StateHasChanged();
        }
    }

    private async Task DeleteDuplicatesInGroup(string hash)
    {
        var group = duplicateGroups?.FirstOrDefault(g => g.Hash == hash);
        if (group == null)
            return;

        var keepImage = group.Images.OrderBy(i => i.DateCreated).First();
        var deleteImages = group.Images.Where(i => i.Id != keepImage.Id).ToList();

        var confirmMessage = $"Delete {deleteImages.Count} duplicate(s) of this photo?\n\n" +
                           $"✅ Keep: {keepImage.FileName}\n" +
                           $"🗑️ Delete: {string.Join(", ", deleteImages.Take(3).Select(i => i.FileName))}" +
                           (deleteImages.Count > 3 ? $" and {deleteImages.Count - 3} more" : "");

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", confirmMessage);
        if (!confirmed)
            return;

        try
        {
            var result = await DuplicateCleanupService.DeleteDuplicatesInGroupAsync(hash);

            if (result.WasSuccessful)
            {
                Logger.LogInformation("Successfully deleted {FilesDeleted} duplicates in group {Hash}", result.FilesDeleted, hash);
                await JSRuntime.InvokeVoidAsync("alert", 
                    $"✅ Deleted {result.FilesDeleted} duplicate(s)\n" +
                    $"Freed: {FormatBytes(result.BytesFreed)} of storage space");
            }
            else
            {
                Logger.LogError("Error deleting duplicates in group {Hash}: {ErrorCount} errors", hash, result.ErrorCount);
                await JSRuntime.InvokeVoidAsync("alert", $"⚠️ Some files could not be deleted. Check logs for details.");
            }

            // Refresh the page
            await LoadDuplicateGroups();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting duplicates in group: {Message}", ex.Message);
            await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
        }
    }

    private void OpenImage(string filePath)
    {
        try
        {
            FileOpenService.OpenInDefaultViewer(filePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error opening image: {Message}", ex.Message);
        }
    }

    private void GoBackToScan()
    {
        Navigation.NavigateTo("/database-scan");
    }

    private string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
        };
    }
}