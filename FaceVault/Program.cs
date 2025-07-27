using FaceVault.Data;
using FaceVault.Services;
using FaceVault.Models;
using FaceVault.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Blazor-recommended logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
}

// Initialize PathService for proper user data management
var pathService = new PathService();

// Initialize custom logging early using proper user data directory
Logger.Initialize(pathService.GetLogsDirectory(), FaceVault.Services.LogLevel.Debug);
Logger.Info("FaceVault Blazor application starting");

// Ensure all application directories exist
pathService.EnsureDirectoriesExist();

// Migrate database from old location if needed
pathService.MigrateDatabaseIfNeeded();

// Add Entity Framework with proper user data path and SQLite optimizations
var dbPath = pathService.GetDatabasePath();
var connectionString = $"Data Source={dbPath};Cache=Shared;";
Logger.Info($"Database path: {pathService.GetDisplayPath(dbPath)}");

builder.Services.AddDbContext<FaceVaultDbContext>(options =>
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(30); // 30 second timeout
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    // Only log EF Core warnings and errors to reduce console spam during scanning
    .LogTo(message => Logger.Debug($"EF Core: {message}"), Microsoft.Extensions.Logging.LogLevel.Warning));

// Add repositories
builder.Services.AddScoped<IRepository<Image>, Repository<Image>>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IRepository<Person>, Repository<Person>>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IRepository<Face>, Repository<Face>>();
builder.Services.AddScoped<IRepository<Tag>, Repository<Tag>>();
builder.Services.AddScoped<IRepository<ImageTag>, Repository<ImageTag>>();

// Add path service as singleton (same paths throughout app lifetime)
builder.Services.AddSingleton<IPathService>(pathService);

// Add services
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPhotoScannerService, PhotoScannerService>();
builder.Services.AddScoped<IFastPhotoScannerService, FastPhotoScannerService>();
builder.Services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
builder.Services.AddScoped<IDatabaseStatsService, DatabaseStatsService>();
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();
builder.Services.AddSingleton<IDatabaseChangeNotificationService, DatabaseChangeNotificationService>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<ImageHashService>();

var app = builder.Build();

// Get logger for structured logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

try
{
    // Initialize database on startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<FaceVaultDbContext>();
        try
        {
            Logger.Info($"Initializing database at: {dbPath}");
            
            // Ensure database is created
            var created = await dbContext.Database.EnsureCreatedAsync();
            if (created)
            {
                Logger.Info("Database created successfully");
            }
            else
            {
                Logger.Info("Database already exists");
            }
            
            // Configure SQLite optimizations
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
            Logger.Info("SQLite optimizations applied");
            
            // Test connection
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                Logger.Info("Database connection test successful");
                
                // Get initial counts
                var imageCount = await dbContext.Images.CountAsync();
                Logger.Info($"Database contains {imageCount} images");
            }
            else
            {
                Logger.Warning("Database connection test failed");
            }
        }
        catch (Exception dbEx)
        {
            Logger.LogException(dbEx, $"Database initialization failed. DB Path: {dbPath}");
            
            // Log detailed exception information
            Logger.Error($"Exception Type: {dbEx.GetType().Name}");
            Logger.Error($"Exception Message: {dbEx.Message}");
            Logger.Error($"Inner Exception: {dbEx.InnerException?.Message ?? "None"}");
            Logger.Error($"Stack Trace: {dbEx.StackTrace}");
            
            // Try to provide more specific error information
            if (dbEx.Message.Contains("database is locked"))
            {
                Logger.Error("Database is locked by another process. Please close any other instances of FaceVault.");
            }
            else if (dbEx.Message.Contains("no such table"))
            {
                Logger.Error("Database schema is missing. Attempting to recreate...");
                try
                {
                    await dbContext.Database.EnsureDeletedAsync();
                    await dbContext.Database.EnsureCreatedAsync();
                    Logger.Info("Database recreated successfully");
                }
                catch (Exception recreateEx)
                {
                    Logger.LogException(recreateEx, "Failed to recreate database");
                    Logger.Error($"Recreate Exception Type: {recreateEx.GetType().Name}");
                    Logger.Error($"Recreate Exception Message: {recreateEx.Message}");
                }
            }
            else if (dbEx.Message.Contains("readonly database"))
            {
                Logger.Error("Database file is readonly. Check file permissions.");
            }
            else if (dbEx.Message.Contains("disk I/O error"))
            {
                Logger.Error("Disk I/O error. Check disk space and file system health.");
            }
            else if (dbEx.Message.Contains("database disk image is malformed"))
            {
                Logger.Error("Database file is corrupted. Consider recreating the database.");
            }
        }
    }

    Logger.Info("FaceVault Blazor application running");
    logger.LogInformation("FaceVault server started successfully and is ready to accept requests");
    app.Run();
}
catch (Exception ex)
{
    Logger.LogException(ex, "Fatal application error");
    throw;
}
finally
{
    Logger.Info("FaceVault Blazor application shutting down");
    Logger.Close();
}
