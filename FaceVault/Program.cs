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

// Initialize custom logging early  
var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
Logger.Initialize(logDirectory, FaceVault.Services.LogLevel.Debug);
Logger.Info("FaceVault Blazor application starting");

// Ensure Data directory exists
var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
if (!Directory.Exists(dataDirectory))
{
    Directory.CreateDirectory(dataDirectory);
    Logger.Info($"Created Data directory: {dataDirectory}");
}

// Add Entity Framework with absolute path
var dbPath = Path.Combine(dataDirectory, "facevault.db");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<FaceVaultDbContext>(options =>
    options.UseSqlite(connectionString));

// Add repositories
builder.Services.AddScoped<IRepository<Image>, Repository<Image>>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IRepository<Person>, Repository<Person>>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IRepository<Face>, Repository<Face>>();
builder.Services.AddScoped<IRepository<Tag>, Repository<Tag>>();
builder.Services.AddScoped<IRepository<ImageTag>, Repository<ImageTag>>();

// Add services
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPhotoScannerService, PhotoScannerService>();
builder.Services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

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
                }
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
