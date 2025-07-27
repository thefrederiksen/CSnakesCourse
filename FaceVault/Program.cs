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

// Add Entity Framework
builder.Services.AddDbContext<FaceVaultDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
        "Data Source=Data/facevault.db"));

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

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
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
            await dbContext.EnsureDatabaseCreatedAsync();
            Logger.Info("Database initialization completed");
        }
        catch (Exception dbEx)
        {
            Logger.LogException(dbEx, "Database initialization failed");
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
