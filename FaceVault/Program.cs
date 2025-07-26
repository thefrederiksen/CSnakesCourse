using FaceVault.Data;
using FaceVault.Services;

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
