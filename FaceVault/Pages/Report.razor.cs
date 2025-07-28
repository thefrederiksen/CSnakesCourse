using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using FaceVault.Services;
using FaceVault.Models;

namespace FaceVault.Pages;

public partial class Report : ComponentBase
{
    [Inject] protected ILibraryReportService ReportService { get; set; } = default!;
    [Inject] protected IFileOpenService FileOpenService { get; set; } = default!;
    [Inject] protected ILogger<Report> Logger { get; set; } = default!;

    private LibraryReport? report;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadReport();
    }

    private async Task LoadReport()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            Logger.LogInformation("Loading library report...");
            report = await ReportService.GenerateReportAsync();
            Logger.LogInformation("Library report loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading library report");
            report = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task RefreshReport()
    {
        await LoadReport();
    }

    private void OpenImage(string filePath)
    {
        try
        {
            FileOpenService.OpenInDefaultViewer(filePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error opening image: {FilePath}", filePath);
        }
    }
}