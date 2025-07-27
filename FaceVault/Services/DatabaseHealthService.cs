using Microsoft.EntityFrameworkCore;
using FaceVault.Data;

namespace FaceVault.Services;

public interface IDatabaseHealthService
{
    Task<DatabaseHealth> CheckHealthAsync();
    Task<bool> RepairDatabaseAsync();
    Task<bool> InitializeDatabaseAsync();
    Task<string> GetDatabasePathAsync();
}

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly FaceVaultDbContext _context;
    private readonly IConfiguration _configuration;

    public DatabaseHealthService(FaceVaultDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<DatabaseHealth> CheckHealthAsync()
    {
        var health = new DatabaseHealth();
        
        try
        {
            // Check if database file exists
            var dbPath = await GetDatabasePathAsync();
            health.DatabasePath = dbPath;
            health.DatabaseExists = File.Exists(dbPath);
            
            if (health.DatabaseExists)
            {
                var fileInfo = new FileInfo(dbPath);
                health.DatabaseSizeBytes = fileInfo.Length;
            }

            // Test connection
            health.CanConnect = await _context.Database.CanConnectAsync();
            
            if (health.CanConnect)
            {
                // Check if tables exist
                health.TablesExist = await CheckTablesExistAsync();
                
                if (health.TablesExist)
                {
                    // Get record counts
                    health.ImageCount = await _context.Images.CountAsync();
                    health.PeopleCount = await _context.People.CountAsync();
                    health.FaceCount = await _context.Faces.CountAsync();
                    health.TagCount = await _context.Tags.CountAsync();
                }
            }

            health.IsHealthy = health.CanConnect && health.TablesExist;
        }
        catch (Exception ex)
        {
            health.Error = ex.Message;
            health.IsHealthy = false;
            Logger.Error($"Database health check failed: {ex.Message}");
        }

        return health;
    }

    public async Task<bool> RepairDatabaseAsync()
    {
        try
        {
            Logger.Info("Attempting database repair...");
            
            // Try to recreate the database
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            
            // Verify repair worked
            var health = await CheckHealthAsync();
            if (health.IsHealthy)
            {
                Logger.Info("Database repair completed successfully");
                return true;
            }
            else
            {
                Logger.Error("Database repair failed - database still unhealthy");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Database repair failed");
            return false;
        }
    }

    public async Task<bool> InitializeDatabaseAsync()
    {
        try
        {
            Logger.Info("Initializing database...");
            
            var dbPath = await GetDatabasePathAsync();
            var dataDirectory = Path.GetDirectoryName(dbPath);
            
            if (!string.IsNullOrEmpty(dataDirectory) && !Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
                Logger.Info($"Created database directory: {dataDirectory}");
            }

            var created = await _context.Database.EnsureCreatedAsync();
            if (created)
            {
                Logger.Info("Database created successfully");
            }
            
            // Verify initialization
            var health = await CheckHealthAsync();
            return health.IsHealthy;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Database initialization failed");
            return false;
        }
    }

    public Task<string> GetDatabasePathAsync()
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                var path = connectionString.Split("Data Source=")[1].Split(';')[0];
                return Task.FromResult(Path.GetFullPath(path));
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Could not determine database path: {ex.Message}");
        }
        
        // Fallback to default path
        return Task.FromResult(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "facevault.db"));
    }

    private async Task<bool> CheckTablesExistAsync()
    {
        try
        {
            // Try to query each main table to verify schema exists
            await _context.Images.AnyAsync();
            await _context.People.AnyAsync();
            await _context.Faces.AnyAsync();
            await _context.Tags.AnyAsync();
            await _context.AppSettings.AnyAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Database tables check failed: {ex.Message}");
            return false;
        }
    }
}

public class DatabaseHealth
{
    public bool IsHealthy { get; set; }
    public bool DatabaseExists { get; set; }
    public bool CanConnect { get; set; }
    public bool TablesExist { get; set; }
    public string DatabasePath { get; set; } = "";
    public long DatabaseSizeBytes { get; set; }
    public int ImageCount { get; set; }
    public int PeopleCount { get; set; }
    public int FaceCount { get; set; }
    public int TagCount { get; set; }
    public string? Error { get; set; }
    
    public string DatabaseSizeFormatted
    {
        get
        {
            if (DatabaseSizeBytes < 1024) return $"{DatabaseSizeBytes} B";
            if (DatabaseSizeBytes < 1024 * 1024) return $"{DatabaseSizeBytes / 1024.0:F1} KB";
            if (DatabaseSizeBytes < 1024L * 1024 * 1024) return $"{DatabaseSizeBytes / (1024.0 * 1024):F1} MB";
            return $"{DatabaseSizeBytes / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}