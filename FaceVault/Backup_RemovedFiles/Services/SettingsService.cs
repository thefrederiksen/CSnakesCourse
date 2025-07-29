using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Models;

namespace FaceVault.Services;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task<AppSettings> SaveSettingsAsync(AppSettings settings);
    Task<AppSettings> ResetToDefaultsAsync();
    Task<bool> IsFirstRunAsync();
    Task<T> GetSettingAsync<T>(string settingName, T defaultValue);
    Task SetSettingAsync<T>(string settingName, T value);
    Task<bool> IsFeatureEnabledAsync(string featureName);
    Task EnableFeatureAsync(string featureName, bool enabled);
    Task<Dictionary<string, object>> GetFeatureStatusAsync();
    
    // Additional interface methods
    Task<string> GetPhotoDirectoryAsync();
    Task SetPhotoDirectoryAsync(string directory);
    Task<string> ExportSettingsAsync();
    Task ImportSettingsAsync(string settingsJson);
    Task<int> GetBatchSizeAsync();
}

public class SettingsService : ISettingsService
{
    private readonly FaceVaultDbContext _context;
    private AppSettings? _cachedSettings;
    private readonly object _lock = new();

    public SettingsService(FaceVaultDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        lock (_lock)
        {
            if (_cachedSettings != null)
                return _cachedSettings;
        }

        var settings = await _context.AppSettings.FirstOrDefaultAsync(s => s.Id == 1);
        
        if (settings == null)
        {
            // Create default settings if none exist
            settings = new AppSettings();
            _context.AppSettings.Add(settings);
            await _context.SaveChangesAsync();
            
            Logger.Info("Created default application settings");
        }

        lock (_lock)
        {
            _cachedSettings = settings;
        }

        return settings;
    }

    public async Task<AppSettings> SaveSettingsAsync(AppSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        settings.Id = 1; // Ensure singleton ID
        settings.MarkAsModified();

        var existingSettings = await _context.AppSettings.FirstOrDefaultAsync(s => s.Id == 1);
        
        if (existingSettings == null)
        {
            _context.AppSettings.Add(settings);
        }
        else
        {
            _context.Entry(existingSettings).CurrentValues.SetValues(settings);
        }

        await _context.SaveChangesAsync();

        // Update cache
        lock (_lock)
        {
            _cachedSettings = settings;
        }

        Logger.Info("Application settings saved successfully");
        return settings;
    }

    public async Task<AppSettings> ResetToDefaultsAsync()
    {
        var settings = await GetSettingsAsync();
        settings.ResetToDefaults();
        return await SaveSettingsAsync(settings);
    }

    public async Task<bool> IsFirstRunAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.IsFirstRun;
    }

    public async Task<T> GetSettingAsync<T>(string settingName, T defaultValue)
    {
        try
        {
            var settings = await GetSettingsAsync();
            var property = typeof(AppSettings).GetProperty(settingName);
            
            if (property != null)
            {
                var value = property.GetValue(settings);
                if (value is T typedValue)
                    return typedValue;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to get setting '{settingName}': {ex.Message}");
        }

        return defaultValue;
    }

    public async Task SetSettingAsync<T>(string settingName, T value)
    {
        try
        {
            var settings = await GetSettingsAsync();
            var property = typeof(AppSettings).GetProperty(settingName);
            
            if (property != null && property.CanWrite)
            {
                property.SetValue(settings, value);
                await SaveSettingsAsync(settings);
            }
            else
            {
                throw new ArgumentException($"Setting '{settingName}' not found or not writable");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set setting '{settingName}': {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        var settings = await GetSettingsAsync();
        return settings.IsFeatureEnabled(featureName);
    }

    public async Task EnableFeatureAsync(string featureName, bool enabled)
    {
        var settings = await GetSettingsAsync();
        
        var updated = featureName.ToLowerInvariant() switch
        {
            "face" or "facerecognition" => UpdateSetting(() => settings.EnableFaceRecognition = enabled),
            "onthisday" or "memories" => UpdateSetting(() => settings.EnableOnThisDay = enabled),
            "screenshots" or "screenshotfiltering" => UpdateSetting(() => settings.EnableScreenshotFiltering = enabled),
            "duplicates" or "duplicatedetection" => UpdateSetting(() => settings.EnableDuplicateDetection = enabled),
            "collage" or "collagegeneration" => UpdateSetting(() => settings.EnableCollageGeneration = enabled),
            "llm" or "llmquery" => UpdateSetting(() => settings.EnableLLMQuery = enabled),
            _ => false
        };

        if (updated)
        {
            await SaveSettingsAsync(settings);
        }
        else
        {
            throw new ArgumentException($"Unknown feature: {featureName}");
        }
    }

    public async Task<Dictionary<string, object>> GetFeatureStatusAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.GetFeatureStatus();
    }

    // Directory and path helpers
    public async Task<string> GetPhotoDirectoryAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.PhotoDirectory;
    }

    public async Task SetPhotoDirectoryAsync(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory cannot be empty", nameof(directory));

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        await SetSettingAsync(nameof(AppSettings.PhotoDirectory), directory);
        Logger.Info($"Photo directory updated to: {directory}");
    }

    // LLM Configuration helpers
    public async Task<bool> HasLLMConfigurationAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.HasLLMConfiguration;
    }

    public async Task SetLLMConfigurationAsync(string provider, string apiKey, string? endpoint = null, string? model = null)
    {
        var settings = await GetSettingsAsync();
        settings.LLMProvider = provider;
        settings.LLMApiKey = apiKey;
        settings.LLMApiEndpoint = endpoint;
        settings.LLMModel = model;
        
        await SaveSettingsAsync(settings);
        Logger.Info($"LLM configuration updated for provider: {provider}");
    }

    // Performance settings helpers
    public async Task<int> GetBatchSizeAsync()
    {
        return await GetSettingAsync(nameof(AppSettings.BatchSize), 100);
    }

    public async Task<long> GetMaxMemoryUsageBytesAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.MaxMemoryUsageBytes;
    }

    public async Task<string[]> GetSupportedExtensionsAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.SupportedExtensions;
    }

    // Privacy settings helpers
    public async Task<bool> IsEncryptionEnabledAsync()
    {
        return await GetSettingAsync(nameof(AppSettings.EncryptFaceEncodings), true);
    }

    public async Task<bool> IsTelemetryEnabledAsync()
    {
        return await GetSettingAsync(nameof(AppSettings.AllowTelemetry), false);
    }

    // Notification and cleanup
    public void InvalidateCache()
    {
        lock (_lock)
        {
            _cachedSettings = null;
        }
    }

    private static bool UpdateSetting(Action updateAction)
    {
        try
        {
            updateAction();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Export/Import settings
    public async Task<string> ExportSettingsAsync()
    {
        var settings = await GetSettingsAsync();
        // Remove sensitive information
        var exportSettings = new AppSettings
        {
            // Copy all settings except sensitive ones
            EnableOnThisDay = settings.EnableOnThisDay,
            EnableFaceRecognition = settings.EnableFaceRecognition,
            EnableScreenshotFiltering = settings.EnableScreenshotFiltering,
            EnableDuplicateDetection = settings.EnableDuplicateDetection,
            EnableCollageGeneration = settings.EnableCollageGeneration,
            EnableLLMQuery = settings.EnableLLMQuery,
            PhotoDirectory = settings.PhotoDirectory,
            // Don't export: LLMApiKey, sensitive settings
        };

        return System.Text.Json.JsonSerializer.Serialize(exportSettings, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    public async Task ImportSettingsAsync(string settingsJson)
    {
        try
        {
            var importedSettings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(settingsJson);
            if (importedSettings != null)
            {
                var currentSettings = await GetSettingsAsync();
                
                // Safely update non-sensitive settings
                currentSettings.EnableOnThisDay = importedSettings.EnableOnThisDay;
                currentSettings.EnableFaceRecognition = importedSettings.EnableFaceRecognition;
                currentSettings.EnableScreenshotFiltering = importedSettings.EnableScreenshotFiltering;
                currentSettings.EnableDuplicateDetection = importedSettings.EnableDuplicateDetection;
                currentSettings.EnableCollageGeneration = importedSettings.EnableCollageGeneration;
                currentSettings.EnableLLMQuery = importedSettings.EnableLLMQuery;
                
                // Validate directory before importing
                if (!string.IsNullOrEmpty(importedSettings.PhotoDirectory) && 
                    Directory.Exists(importedSettings.PhotoDirectory))
                {
                    currentSettings.PhotoDirectory = importedSettings.PhotoDirectory;
                }

                await SaveSettingsAsync(currentSettings);
                Logger.Info("Settings imported successfully");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to import settings: {ex.Message}");
            throw;
        }
    }
}