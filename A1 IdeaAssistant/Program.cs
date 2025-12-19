using IdeaAssistant.Components;
using CSnakes.Runtime;
using CSnakes.Runtime.PackageManagement;
using System.Diagnostics;

namespace IdeaAssistant
{
    /// <summary>
    /// CSnakes Course - Agent SDK Demo: Idea Assistant
    ///
    /// Learning Objectives:
    /// - Use OpenAI Agents SDK from C# via CSnakes
    /// - Implement multi-agent system with triage routing
    /// - Use function tools and hosted tools (WebSearchTool)
    /// - Handle agent handoffs between specialists
    /// - Add voice input with Whisper transcription
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // Load environment variables from .env file
                EnvLoader.Load();

                WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

                // Add environment variable configuration
                builder.Configuration.AddEnvironmentVariables();

                Console.WriteLine("Initializing Python...");

                var version = "3.12";

                var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var home = Path.Join(exeDir, "PythonAgents\\Src");
                var requirements = Path.Combine(home, "requirements.txt");

                if (!File.Exists(requirements))
                {
                    throw new Exception("No requirements.txt file found.");
                }

                var virtualDir = Path.Join(home, ".venv");
                Console.WriteLine($"   Virtual Environment Directory: {virtualDir}");

                var services = builder.Services
                    .WithPython()
                    .WithHome(home)
                    .FromRedistributable(version)
                    .WithVirtualEnvironment(virtualDir)
                    .WithUvInstaller();

                // Add Blazor Server services
                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents();

                var app = builder.Build();

                var sw = Stopwatch.StartNew();

                Console.WriteLine("Creating Python environment...");
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine($"Environment created - {sw.ElapsedMilliseconds} ms...");

                sw.Restart();
                var packages = File.ReadAllLines(requirements);
                Console.WriteLine("Packages to be installed:");
                foreach (var package in packages)
                {
                    Console.WriteLine($"- {package}");
                }
                Console.WriteLine("Installing Python packages...");
                var installer = app.Services.GetRequiredService<IPythonPackageInstaller>();
                installer.InstallPackagesFromRequirements(home).GetAwaiter().GetResult();

                Console.WriteLine($"Packages installed - {sw.ElapsedMilliseconds} ms...");

                // Configure the HTTP request pipeline
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                }

                app.UseAntiforgery();

                app.MapStaticAssets();

                // Transcription API endpoint
                app.MapPost("/api/audio/transcribe", async (HttpContext context, IPythonEnvironment pythonEnv, IConfiguration config) =>
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        await context.Request.Body.CopyToAsync(ms);
                        var audioBytes = ms.ToArray();

                        if (audioBytes.Length == 0)
                            return Results.BadRequest(new { error = "No audio data" });

                        var apiKey = config["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                        if (string.IsNullOrEmpty(apiKey))
                            return Results.BadRequest(new { error = "OPENAI_API_KEY not configured" });

                        var text = await Task.Run(() =>
                        {
                            var transcribe = pythonEnv.Transcribe();
                            return transcribe.TranscribeAudio(audioBytes, apiKey);
                        });

                        return Results.Ok(new { text });
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new { error = ex.Message });
                    }
                });

                app.MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode();

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex}");
            }
        }
    }
}
