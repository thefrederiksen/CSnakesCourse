using FaceVault.Services;
using FaceVault.Data;
using Microsoft.EntityFrameworkCore;
using CSnakes.Runtime;
using CSnakes.Runtime.PackageManagement;
using System.Windows.Forms;
using System.Runtime.InteropServices;

// Windows API imports for console management
[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

const int SW_HIDE = 0;

// Check if app should start minimized
var commandLineArgs = Environment.GetCommandLineArgs();
var startMinimized = commandLineArgs.Contains("--minimized");

var builder = WebApplication.CreateBuilder(args);

// Configure Blazor-recommended logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Check for verbose logging environment variable or configuration
var enableVerboseLogging = Environment.GetEnvironmentVariable("FACEVAULT_VERBOSE_LOGGING")?.ToLower() == "true" ||
                          builder.Configuration.GetValue<bool>("VerboseLogging");

// Set appropriate log levels - default to Information for important events only
if (enableVerboseLogging)
{
    // Verbose mode - show all logs including debug
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
    builder.Logging.AddFilter("FaceVault", Microsoft.Extensions.Logging.LogLevel.Debug);
}
else
{
    // Default mode - show Information and above for our app, errors only for framework
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    
    // Suppress verbose logs from framework components
    builder.Logging.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.AspNetCore", Microsoft.Extensions.Logging.LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", Microsoft.Extensions.Logging.LogLevel.Warning);
    builder.Logging.AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning);
    
    // Allow Information and above from our app
    builder.Logging.AddFilter("FaceVault", Microsoft.Extensions.Logging.LogLevel.Information);
}

// Initialize PathService for proper user data management
var pathService = new PathService();

// Initialize custom logging early using proper user data directory
var customLogLevel = enableVerboseLogging ? FaceVault.Services.LogLevel.Debug : FaceVault.Services.LogLevel.Info;
Logger.Initialize(pathService.GetLogsDirectory(), customLogLevel);
if (enableVerboseLogging)
{
    Logger.Info("FaceVault Blazor application starting (VERBOSE LOGGING ENABLED)");
}
else
{
    Logger.Info("FaceVault Blazor application starting");
}

// Ensure all application directories exist
pathService.EnsureDirectoriesExist();

// Migrate database from old location if needed
pathService.MigrateDatabaseIfNeeded();

// Initialize Python environment with CSnakes
Logger.Info("Initializing Python environment...");
var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
var pythonHome = Path.Join(exeDir, "Python");
var requirements = Path.Combine(pythonHome, "requirements.txt");

if (!File.Exists(requirements))
{
    Logger.Warning("No requirements.txt file found - Python packages may not be available");
}

var virtualDir = Path.Join(pythonHome, ".venv");
Logger.Info($"Python Home: {pythonHome}");
Logger.Info($"Virtual Environment Directory: {virtualDir}");

// Add Entity Framework with proper user data path and SQLite optimizations
var dbPath = pathService.GetDatabasePath();
var connectionString = $"Data Source={dbPath};Cache=Shared;";
Logger.Info($"Database path: {pathService.GetDisplayPath(dbPath)}");

// Configure Python services with CSnakes
builder.Services
    .WithPython()
    .WithHome(pythonHome)
    .FromRedistributable("3.12") // Use redistributable strategy to ensure Python is available
    .WithVirtualEnvironment(virtualDir)
    .WithUvInstaller(); // Install packages from requirements.txt on startup

builder.Services.AddDbContext<FaceVaultDbContext>(options =>
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(30); // 30 second timeout
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    // Only log EF Core warnings and errors to reduce console spam during scanning
    .LogTo(message => Logger.Debug($"EF Core: {message}"), Microsoft.Extensions.Logging.LogLevel.Warning));

// Add path service as singleton (same paths throughout app lifetime)
builder.Services.AddSingleton<IPathService>(pathService);

// Add only working services
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddSingleton<IDatabaseChangeNotificationService, DatabaseChangeNotificationService>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add system tray service
builder.Services.AddSingleton<SystemTrayService>();
builder.Services.AddHostedService<BackgroundTaskService>();

var app = builder.Build();

// Create a task to hide console window shortly after startup
_ = Task.Run(async () =>
{
    await Task.Delay(500); // Wait for console to be fully created
    var consoleWindow = GetConsoleWindow();
    if (consoleWindow != IntPtr.Zero)
    {
        ShowWindow(consoleWindow, SW_HIDE);
    }
});

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
    // Initialize Python environment
    Logger.Info("Creating Python environment...");
    var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
    Logger.Info("Python environment created successfully");

    // Install Python packages if requirements.txt exists
    if (File.Exists(requirements))
    {
        Logger.Info("Installing Python packages...");
        var installer = app.Services.GetRequiredService<IPythonPackageInstaller>();
        await installer.InstallPackagesFromRequirements(pythonHome);
        Logger.Info("Python packages installed successfully");
    }

    // Initialize database using SQL scripts
    using (var scope = app.Services.CreateScope())
    {
        var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<FaceVaultDbContext>();
        
        try
        {
            Logger.Debug($"Initializing database at: {dbPath}");
            
            var success = await dbInitService.InitializeDatabaseAsync(connectionString);
            if (success)
            {
                Logger.Info("Database initialized successfully");
                
                // Get current version
                var version = await dbInitService.GetCurrentVersionAsync(connectionString);
                Logger.Debug($"Database is at version: {version}");
            }
            else
            {
                Logger.Error("Failed to initialize database");
                throw new Exception("Database initialization failed");
            }
            
            // Configure SQLite optimizations
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
            Logger.Debug("SQLite optimizations applied");
            
            // Test connection
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                Logger.Debug("Database connection test successful");
                
                // Get initial counts - disabled until EF models are scaffolded
                // var imageCount = await dbContext.Images.CountAsync();
                // Logger.Info($"Database contains {imageCount} images");
                Logger.Debug("Database connection verified");
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
    
    // Initialize system tray
    var systemTrayService = app.Services.GetRequiredService<SystemTrayService>();
    systemTrayService.Initialize();
    
    // Hide console window after everything is initialized
    var consoleWindow = GetConsoleWindow();
    if (consoleWindow != IntPtr.Zero)
    {
        ShowWindow(consoleWindow, SW_HIDE);
        Logger.Info("Console window hidden after initialization");
    }
    
    // If started with --minimized, don't open browser
    if (!startMinimized)
    {
        // Open default browser with correct port from launchSettings
        var url = "http://localhost:5113";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to open browser: {ex.Message}");
        }
    }
    
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
