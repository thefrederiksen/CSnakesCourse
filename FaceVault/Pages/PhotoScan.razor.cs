using Microsoft.AspNetCore.Components;
using FaceVault.Services;

namespace FaceVault.Pages;

public partial class PhotoScan : ComponentBase, IDisposable
{

    private string scanDirectory = "";
    private bool includeSubdirectories = true;
    private bool quickScanFirst = true;
    private bool autoScanOnStartup = false;
    private int batchSize = 100;
    private string[] supportedExtensions = Array.Empty<string>();
    private DateTime? lastScanInfo;

    private bool isScanning = false;
    private bool hasRunQuickScan = false;
    private ScanProgress? scanProgress;
    private ScanResult? scanResult;
    private QuickScanResult? quickScanResult;
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
            Logger.Error($"Error loading scanner settings: {ex.Message}");
        }
    }

    private Task BrowseForDirectory()
    {
        // Placeholder for folder browser dialog
        // In a real implementation, this would open a native folder browser
        return Task.CompletedTask;
    }

    private async Task RunQuickScan()
    {
        try
        {
            isScanning = true;
            quickScanResult = null;
            StateHasChanged();

            Logger.Info($"Starting quick scan of directory: {scanDirectory}");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2-minute timeout for quick scan
            quickScanResult = await PhotoScannerService.QuickScanAsync(scanDirectory, cts.Token);
            hasRunQuickScan = true;

            Logger.Info($"Quick scan completed: {quickScanResult.TotalFilesFound} files found");
        }
        catch (Exception ex)
        {
            Logger.Error($"Quick scan failed: {ex.Message}");
            quickScanResult = new QuickScanResult
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
            StateHasChanged();
        }
    }

    private async Task StartFullScan()
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

            Logger.Info($"Starting full scan of directory: {scanDirectory}");
            Logger.Info("Reducing logging verbosity during scan for better performance...");

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
                Logger.Info($"Full scan completed successfully: {scanResult.NewImagesCount} new images added, {scanResult.SkippedCount} skipped");
            }
            else if (scanResult.IsCancelled)
            {
                Logger.Info("Scan was cancelled by user");
            }
            else
            {
                Logger.Error($"Scan failed: {scanResult.Error}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Full scan failed: {ex.Message}");
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
        Logger.Info("Scan cancellation requested");
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