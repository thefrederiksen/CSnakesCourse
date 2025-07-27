using System;
using System.IO;

namespace FaceVault.Services;

public interface IPathService
{
    string GetUserDataDirectory();
    string GetDatabasePath();
    string GetLogsDirectory();
    string GetSettingsDirectory();
    string GetTempDirectory();
    void EnsureDirectoriesExist();
}

public class PathService : IPathService
{
    private const string ApplicationName = "FaceVault";
    
    private readonly string _userDataDirectory;
    private readonly string _databasePath;
    private readonly string _logsDirectory;
    private readonly string _settingsDirectory;
    private readonly string _tempDirectory;

    public PathService()
    {
        // Use %APPDATA%\FaceVault for application data
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _userDataDirectory = Path.Combine(appDataPath, ApplicationName);
        
        // Define all paths relative to user data directory
        _databasePath = Path.Combine(_userDataDirectory, "Database", "facevault.db");
        _logsDirectory = Path.Combine(_userDataDirectory, "Logs");
        _settingsDirectory = Path.Combine(_userDataDirectory, "Settings");
        _tempDirectory = Path.Combine(_userDataDirectory, "Temp");
        
        Logger.Info($"PathService initialized with user data directory: {_userDataDirectory}");
    }

    public string GetUserDataDirectory() => _userDataDirectory;

    public string GetDatabasePath() => _databasePath;

    public string GetLogsDirectory() => _logsDirectory;

    public string GetSettingsDirectory() => _settingsDirectory;

    public string GetTempDirectory() => _tempDirectory;

    public void EnsureDirectoriesExist()
    {
        try
        {
            // Create all necessary directories
            var directories = new[]
            {
                _userDataDirectory,
                Path.GetDirectoryName(_databasePath)!, // Database directory
                _logsDirectory,
                _settingsDirectory,
                _tempDirectory
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Logger.Info($"Created directory: {directory}");
                }
            }

            Logger.Info("All application directories verified/created successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create application directories: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the old database path (for migration purposes)
    /// </summary>
    public string GetOldDatabasePath()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dataDirectory = Path.Combine(baseDirectory, "Data");
        return Path.Combine(dataDirectory, "facevault.db");
    }

    /// <summary>
    /// Migrates the database from the old location to the new user data location
    /// </summary>
    public bool MigrateDatabaseIfNeeded()
    {
        try
        {
            var oldDatabasePath = GetOldDatabasePath();
            var newDatabasePath = GetDatabasePath();

            // If new database already exists, no migration needed
            if (File.Exists(newDatabasePath))
            {
                Logger.Info("Database already exists in user data directory");
                return true;
            }

            // If old database exists, migrate it
            if (File.Exists(oldDatabasePath))
            {
                Logger.Info($"Migrating database from {oldDatabasePath} to {newDatabasePath}");
                
                // Ensure the directory exists
                var newDatabaseDirectory = Path.GetDirectoryName(newDatabasePath);
                if (!Directory.Exists(newDatabaseDirectory))
                {
                    Directory.CreateDirectory(newDatabaseDirectory!);
                }

                // Copy the database file
                File.Copy(oldDatabasePath, newDatabasePath, overwrite: false);
                Logger.Info("Database migration completed successfully");
                
                // Optionally delete the old file (commented out for safety)
                // File.Delete(oldDatabasePath);
                
                return true;
            }

            Logger.Info("No existing database found - will create new database in user data directory");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Database migration failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets user-friendly display paths for showing to users
    /// </summary>
    public string GetDisplayPath(string fullPath)
    {
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (fullPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Replace(userProfile, "~", StringComparison.OrdinalIgnoreCase);
            }
            return fullPath;
        }
        catch
        {
            return fullPath;
        }
    }
}