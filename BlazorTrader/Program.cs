using BlazorTrader.Components;
using CSnakes.Runtime;
using CSnakes.Runtime.PackageManagement;
using System;
using System.Diagnostics;

namespace BlazorTrader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

                Console.WriteLine("Initializing python...");

                var version = "3.12";


                var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var home = Path.Join(exeDir, "PythonTrader\\Src");
                var requirements = Path.Combine(home, "requirements.txt");

                if (!File.Exists(requirements))
                {
                    throw new Exception("No requirements.txt file found.");
                }

                var virtualDir = Path.Join(home, ".venv");
                Console.WriteLine($"   Virtual Environment Directory: {virtualDir}");

                //var builder = Host.CreateApplicationBuilder();
                var services = builder.Services
                    .WithPython()
                    .WithHome(home)
                    .FromRedistributable(version) // Use redistributable strategy to ensure Python is available

                    /* Add these methods to configure a virtual environment and install packages from requirements.txt */
                    .WithVirtualEnvironment(virtualDir)
                    .WithUvInstaller(); // Optional - installs packages listed in requirements.txt on startup            

                // Add Blazor Server services as normal
                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents();

                var app = builder.Build();

                var sw = Stopwatch.StartNew();

                Console.WriteLine($"Create python environment...");
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine($"Environment created - {sw.ElapsedMilliseconds} ms...");

                sw.Restart();
                // Show the packages in the requirements.txt file
                var packages = File.ReadAllLines(requirements);
                Console.WriteLine("Packages to be installed:");
                foreach (var package in packages)
                {
                    Console.WriteLine($"- {package}");
                }
                Console.WriteLine($"Installing python packages...");
                var installer = app.Services.GetRequiredService<IPythonPackageInstaller>();
                installer.InstallPackagesFromRequirements(home).GetAwaiter().GetResult();

                Console.WriteLine($"Packages finished installing - {sw.ElapsedMilliseconds} ms...");

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                }

                app.UseAntiforgery();

                app.MapStaticAssets();
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
