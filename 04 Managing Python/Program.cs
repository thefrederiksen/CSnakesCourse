using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CSnakes.Runtime;
using CSnakes.Runtime.PackageManagement;
using System.Diagnostics;

namespace Managing_Python
{
    class Program
    {
        public static void Main(string[] args)
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

            Console.WriteLine("=== CSnakes Lab : Managing Python Runtimes & Virtual Environments ===\n");

            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pythonHomeDir = Path.Join(exeDir, "Python");

            Console.WriteLine("Select a test to run:");
            Console.WriteLine("1. Redistributable Strategy");
            Console.WriteLine("2. Environment Variable Strategy");
            Console.WriteLine("3. Folder Strategy");
            Console.WriteLine("4. Run With Pip Installer - Simple");
            Console.WriteLine("5. Run With Uv Installer - Simple");
            Console.WriteLine("6. Run With Uv Installer - Control");
            Console.Write("Enter 1, 2, 3, 4, 5 or 6: ");

            var key = Console.ReadKey(intercept: true);
            Console.WriteLine();

            switch (key.KeyChar)
            {
                case '1':
                    TestRedistributableStrategy(pythonHomeDir, "3.13");
                    break;
                case '2':
                    TestEnvironmentVariableStrategy(pythonHomeDir, "3.11");
                    break;
                case '3':
                    TestFolderStrategy(pythonHomeDir, "3.11");
                    break;
                case '4':
                    TestRunWithPipInstaller(pythonHomeDir, "3.12");
                    break;
                case '5':
                    TestRunWithUVInstaller(pythonHomeDir, "3.12");
                    break;
                case '6':
                    TestRunWithUvInstallerControl(pythonHomeDir, "3.13");
                    break;
                default:
                    Console.WriteLine("Invalid selection. Exiting.");
                    break;
            }
        }

        private static void TestRedistributableStrategy(string pythonHomeDir, string version)
        {
            Console.WriteLine("Testing FromRedistributable()");
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                .WithPython()
                .WithHome(pythonHomeDir)
                .FromRedistributable(version);
            TestPython("Redistributable", builder);
        }

        private static void TestEnvironmentVariableStrategy(string pythonHomeDir, string version)
        {
            Console.WriteLine("Testing FromEnvironmentVariable()");
            var pythonHome = Environment.GetEnvironmentVariable("PYTHON_HOME");
            if (!string.IsNullOrEmpty(pythonHome))
            {
                var builder = Host.CreateApplicationBuilder();
                builder.Services
                    .WithPython()
                    .WithHome(pythonHomeDir)
                    .FromEnvironmentVariable("PYTHON_HOME", version);
                TestPython("Environment Variable", builder);
            }
            else
            {
                Console.WriteLine("PYTHON_HOME not set. Skipping this test.");
            }
        }

        private static void TestFolderStrategy(string pythonHomeDir, string version)
        {
            string? pythonPath = @"c:\program files\python311";
            if (string.IsNullOrEmpty(pythonPath) || !Directory.Exists(pythonPath))
            {
                Console.WriteLine("Invalid Python path. Skipping this test.");
                return;
            }
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                .WithPython()
                .WithHome(pythonHomeDir)
                .FromFolder(pythonPath, version);
            TestPython("Direct Folder", builder);
        }

        public static void TestPython(string strategyName, HostApplicationBuilder builder)
        {
            try
            {
                Console.WriteLine($"Testing {strategyName} strategy...");
                using var app = builder.Build();
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine("Python environment created.");
                try
                {
                    var simpleDemo = pythonEnv.SimpleDemo();
                    var version = simpleDemo.GetPythonVersion();
                    Console.WriteLine($"Python Version: {version.Split('\n')[0]}");
                }
                catch (Exception moduleEx)
                {
                    Console.WriteLine($"Module loading error: {moduleEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void TestRunWithPipInstaller(string pythonHomeDir, string version)
        {
            try
            {
                var requirements = Path.Combine(pythonHomeDir, "requirements.txt");
                var virtualDir = Path.Join(pythonHomeDir, ".venv_installer");
                var builder = Host.CreateApplicationBuilder();
                builder.Services
                    .WithPython()
                    .WithHome(pythonHomeDir)
                    .FromRedistributable(version)
                    .WithVirtualEnvironment(virtualDir)
                    .WithPipInstaller(requirements);
                using var app = builder.Build();
                var sw = Stopwatch.StartNew();
                Console.WriteLine("Creating environment and installing packages...");
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine($"Done - {sw.ElapsedMilliseconds} ms");
                TestPandas(pythonEnv);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during package installation: {ex.Message}");
            }
        }

        static void TestRunWithUVInstaller(string pythonHomeDir, string version)
        {
            try
            {
                var requirements = Path.Combine(pythonHomeDir, "requirements.txt");
                var virtualDir = Path.Join(pythonHomeDir, ".venv_uv_installer");
                var builder = Host.CreateApplicationBuilder();
                builder.Services
                    .WithPython()
                    .WithHome(pythonHomeDir)
                    .FromRedistributable(version)
                    .WithVirtualEnvironment(virtualDir)
                    .WithUvInstaller(requirements);
                using var app = builder.Build();
                var sw = Stopwatch.StartNew();
                Console.WriteLine("Creating environment and installing packages...");
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine($"Done - {sw.ElapsedMilliseconds} ms");
                TestPandas(pythonEnv);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during package installation: {ex.Message}");
            }
        }

        static void TestRunWithUvInstallerControl(string pythonHomeDir, string version)
        {
            try
            {
                var requirements = Path.Combine(pythonHomeDir, "requirements.txt");
                var virtualDir = Path.Join(pythonHomeDir, ".venv_control");
                Console.WriteLine($"Virtual Environment Directory: {virtualDir}");
                var builder = Host.CreateApplicationBuilder();
                builder.Services
                    .WithPython()
                    .WithHome(pythonHomeDir)
                    .FromRedistributable(version)
                    .WithVirtualEnvironment(virtualDir)
                    .WithUvInstaller();
                using var app = builder.Build();
                var sw = Stopwatch.StartNew();
                Console.WriteLine("Creating environment...");
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine($"Environment created - {sw.ElapsedMilliseconds} ms");
                sw.Restart();
                Console.WriteLine("Installing packages...");
                var installer = app.Services.GetRequiredService<IPythonPackageInstaller>();
                installer.InstallPackagesFromRequirements(pythonHomeDir).GetAwaiter().GetResult();
                Console.WriteLine($"Packages installed - {sw.ElapsedMilliseconds} ms");
                TestPandas(pythonEnv);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during package installation: {ex.Message}");
            }
        }

        private static void TestPandas(IPythonEnvironment pythonEnv)
        {
            Console.WriteLine($"Python Version: {pythonEnv.SimpleDemo().GetPythonVersion()}");
            try
            {
                var dfText = pythonEnv.PandasDemo().CreateSampleDataframe();
                Console.WriteLine("Sample DataFrame created.");
                Console.WriteLine($"DataFrame Info: {dfText}");
            }
            catch (Exception dfEx)
            {
                Console.WriteLine($"Error creating DataFrame: {dfEx.Message}");
            }
        }

    }
}