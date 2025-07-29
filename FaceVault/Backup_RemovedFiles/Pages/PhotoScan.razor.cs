using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using FaceVault.Services;

namespace FaceVault.Pages;

public partial class PhotoScan : ComponentBase, IDisposable
{
    [Inject] protected IFastPhotoScannerService PhotoScannerService { get; set; } = default!;
    [Inject] protected ISettingsService SettingsService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected ILogger<PhotoScan> Logger { get; set; } = default!;

    private string scanDirectory = "";
    private bool includeSubdirectories = true;
    private bool autoScanOnStartup = false;
    private int batchSize = 100;
    private string[] supportedExtensions = Array.Empty<string>();
    private DateTime? lastScanInfo;

    private bool isScanning = false;
    private ScanProgress? scanProgress;
    private ScanResult? scanResult;
    private CancellationTokenSource? cancellationTokenSource;
    private DateTime scanStartTime;

    protected override async Task OnInitializedAsync()
    {
        await LoadSettings();
        supportedExtensions = PhotoScannerService.GetSupportedExtensions();
    }

    private async Task LoadSettings()
    {
        try
        {
            var settings = await SettingsService.GetSettingsAsync();
            scanDirectory = settings.PhotoDirectory;
            includeSubdirectories = settings.ScanSubdirectories;
            autoScanOnStartup = settings.AutoScanOnStartup;
            batchSize = settings.BatchSize;
            lastScanInfo = settings.LastScanDate;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading scanner settings: {Message}", ex.Message);
        }
    }



    private async Task StartScan()
    {
        try
        {
            isScanning = true;
            scanResult = null;
            scanProgress = null;
            cancellationTokenSource = new CancellationTokenSource();
            scanStartTime = DateTime.Now;
            
            // Force immediate UI update to show scanning state
            await InvokeAsync(StateHasChanged);

            Logger.LogInformation("Starting full scan of directory: {ScanDirectory}", scanDirectory);
            Logger.LogInformation("Reducing logging verbosity during scan for better performance...");

            var lastUpdateTime = DateTime.Now;
            var updateInterval = TimeSpan.FromMilliseconds(250); // Update UI every 250ms minimum
            var lastUpdateCount = 0;
            
            var progress = new Progress<ScanProgress>(async progressUpdate =>
            {
                scanProgress = progressUpdate;
                
                // Always update the UI at regular intervals and milestones
                var now = DateTime.Now;
                var timeSinceLastUpdate = now - lastUpdateTime;
                var filesSinceLastUpdate = progressUpdate.ProcessedCount - lastUpdateCount;
                
                // Update if: enough time passed, processed 10 files, or phase changed
                // This matches the FastPhotoScannerService reporting frequency
                if (timeSinceLastUpdate >= updateInterval || 
                    filesSinceLastUpdate >= 10 || 
                    progressUpdate.Phase == ScanPhase.Complete ||
                    progressUpdate.Phase == ScanPhase.Discovery)
                {
                    lastUpdateTime = now;
                    lastUpdateCount = progressUpdate.ProcessedCount;
                    
                    // Force UI update on UI thread
                    await InvokeAsync(() =>
                    {
                        StateHasChanged();
                    });
                }
            });

            scanResult = await PhotoScannerService.ScanDirectoryAsync(
                scanDirectory, 
                progress, 
                cancellationTokenSource.Token);

            // Force final UI update
            await InvokeAsync(StateHasChanged);
            
            if (scanResult.IsSuccess)
            {
                Logger.LogInformation("Full scan completed successfully: {NewImagesCount} new images added, {SkippedCount} skipped", scanResult.NewImagesCount, scanResult.SkippedCount);
            }
            else if (scanResult.IsCancelled)
            {
                Logger.LogInformation("Scan was cancelled by user");
            }
            else
            {
                Logger.LogError("Scan failed: {Error}", scanResult.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Full scan failed: {Message}", ex.Message);
            scanResult = new ScanResult
            {
                DirectoryPath = scanDirectory,
                Error = ex.Message,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }
        finally
        {
            isScanning = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            StateHasChanged();
        }
    }

    private void CancelScan()
    {
        cancellationTokenSource?.Cancel();
        Logger.LogInformation("Scan cancellation requested");
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}