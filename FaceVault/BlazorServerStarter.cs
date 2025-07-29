using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using FaceVault.Forms;
using FaceVault.Services;
using FaceVault.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CSnakes.Runtime;
using CSnakes.Runtime.PackageManagement;

namespace FaceVault
{
    public class BlazorServerStarter
    {
        private StartupForm? startupForm;
        private WebApplication? app;
        
        public void Start(string[] args)
        {
            // Check if app should start minimized
            var startMinimized = args.Contains("--minimized");
            
            // Show startup form
            startupForm = new StartupForm();
            if (!startMinimized)
            {
                startupForm.Show();
                Application.DoEvents(); // Process Windows messages
            }
            
            // Start initialization on background thread
            Task.Run(async () =>
            {
                try
                {
                    await InitializeAndStartBlazorServer(args, startMinimized);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failed to start Blazor server");
                    ShowError("Startup Failed", ex.Message);
                    Application.Exit();
                }
            });
            
            // Run Windows Forms message loop
            Application.Run();
        }
        
        private async Task InitializeAndStartBlazorServer(string[] args, bool startMinimized)
        {
            UpdateStatus("Initializing directories...", 10);
            
            // All the original initialization code from Program.cs goes here
            // Initialize PathService
            var pathService = new PathService();
            Logger.Initialize(pathService.GetLogsDirectory(), FaceVault.Services.LogLevel.Info);
            Logger.Info("FaceVault starting (WinForms launcher)");
            
            pathService.EnsureDirectoriesExist();
            pathService.MigrateDatabaseIfNeeded();
            
            UpdateStatus("Creating web application...", 20);
            
            // Create the Blazor web application
            var builder = WebApplication.CreateBuilder(args);
            
            // Configure Kestrel
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenLocalhost(5113, listenOptions =>
                {
                    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                });
            });
            
            UpdateStatus("Configuring services...", 30);
            
            // Configure all the services (logging, EF, Python, etc.)
            ConfigureServices(builder, pathService);
            
            UpdateStatus("Building application...", 40);
            
            // Build the app
            app = builder.Build();
            
            // Configure the HTTP pipeline
            ConfigureApp(app);
            
            UpdateStatus("Initializing Python...", 50);
            
            // Initialize Python
            await InitializePython(app);
            
            UpdateStatus("Initializing database...", 70);
            
            // Initialize database
            await InitializeDatabase(app, pathService);
            
            UpdateStatus("Starting system tray...", 85);
            
            // Initialize system tray
            var systemTrayService = app.Services.GetRequiredService<SystemTrayService>();
            systemTrayService.Initialize();
            
            UpdateStatus("Starting web server...", 95);
            
            // Start the Blazor server in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await app.RunAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Blazor server error");
                }
            });
            
            // Wait for server to be ready
            await Task.Delay(1000);
            
            UpdateStatus("Ready!", 100);
            await Task.Delay(500);
            
            // Hide startup form and open browser
            startupForm?.Invoke(new Action(() =>
            {
                startupForm.Hide();
                
                if (!startMinimized)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "http://localhost:5113",
                            UseShellExecute = true
                        });
                        Logger.Info("Opened browser to Blazor application");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to open browser: {ex.Message}");
                    }
                }
            }));
        }
        
        private void ConfigureServices(WebApplicationBuilder builder, IPathService pathService)
        {
            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();
            builder.Logging.AddEventLog();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            
            // Configure Python
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pythonHome = Path.Join(exeDir, "Python");
            var virtualDir = Path.Join(pythonHome, ".venv");
            
            builder.Services
                .WithPython()
                .WithHome(pythonHome)
                .FromRedistributable("3.12")
                .WithVirtualEnvironment(virtualDir)
                .WithUvInstaller();
            
            // Configure Entity Framework
            var dbPath = pathService.GetDatabasePath();
            var connectionString = $"Data Source={dbPath};Cache=Shared;";
            
            builder.Services.AddDbContext<FaceVaultDbContext>(options =>
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(30);
                })
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                .EnableDetailedErrors(builder.Environment.IsDevelopment()));
            
            // Add services
            builder.Services.AddSingleton<IPathService>(pathService);
            builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
            builder.Services.AddSingleton<IDatabaseChangeNotificationService, DatabaseChangeNotificationService>();
            builder.Services.AddSingleton<SystemTrayService>();
            builder.Services.AddHostedService<BackgroundTaskService>();
            
            // Add Blazor services
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
        }
        
        private void ConfigureApp(WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            
            app.UseStaticFiles();
            app.UseRouting();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
        }
        
        private async Task InitializePython(WebApplication app)
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pythonHome = Path.Join(exeDir, "Python");
            var requirements = Path.Combine(pythonHome, "requirements.txt");
            
            var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Logger.Info("Python environment created");
            
            if (File.Exists(requirements))
            {
                UpdateStatus("Installing Python packages...", 60);
                var installer = app.Services.GetRequiredService<IPythonPackageInstaller>();
                await installer.InstallPackagesFromRequirements(pythonHome);
                Logger.Info("Python packages installed");
            }
        }
        
        private async Task InitializeDatabase(WebApplication app, IPathService pathService)
        {
            var dbPath = pathService.GetDatabasePath();
            var connectionString = $"Data Source={dbPath};Cache=Shared;";
            
            using var scope = app.Services.CreateScope();
            var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<FaceVaultDbContext>();
            
            var success = await dbInitService.InitializeDatabaseAsync(connectionString);
            if (!success)
            {
                throw new Exception("Database initialization failed");
            }
            
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
            
            Logger.Info("Database initialized successfully");
        }
        
        private void UpdateStatus(string message, int progress)
        {
            startupForm?.UpdateStatus(message, progress);
            Logger.Info($"Startup: {message} ({progress}%)");
        }
        
        private void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}