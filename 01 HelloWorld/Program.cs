using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelloWorld
{
    /// <summary>
    /// CSnakes Course - Hello World
    ///
    /// Learning Objectives:
    /// - Set up a basic CSnakes environment
    /// - Import a Python module from C#
    /// - Call a simple Python function with parameters
    /// - Understand automatic string marshaling between C# and Python
    /// - Use redistributable Python for easy deployment
    ///
    /// IMPORTANT - Python Version and Deployment:
    ///
    /// FromRedistributable() automatically downloads Python at runtime and caches it locally.
    /// This means:
    /// - NO Python installation required on deployment/target machines
    /// - Python is downloaded on first run (~50-80MB) to %APPDATA%\CSnakes\
    /// - Subsequent runs use the cached version (fast startup)
    /// - Works on Windows, macOS, and Linux
    ///
    /// Version options:
    /// - FromRedistributable()           -> Downloads Python 3.12 (default)
    /// - FromRedistributable("3.12")     -> Explicit version string
    /// - FromRedistributable("3.13")     -> Python 3.13
    /// - Supported versions: 3.9, 3.10, 3.11, 3.12, 3.13, 3.14
    ///
    /// WARNING: System PYTHONPATH/PYTHONHOME environment variables can interfere!
    /// If you have these set (pointing to a different Python version), you may get:
    ///   "ModuleNotFoundError: No module named 'encodings'"
    /// Solution: Remove/unset PYTHONPATH and PYTHONHOME environment variables.
    ///
    /// For build time: Python must be installed on developer/build machines for
    /// CSnakes to generate bindings. Use the same version specified here (3.12).
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var builder = Host.CreateApplicationBuilder(args);
                var pythonHome = Environment.CurrentDirectory; // Path to your Python modules (.py files)

                // Configure CSnakes to use Python
                // WithPython()              - Registers Python services with dependency injection
                // WithHome(pythonHome)      - Sets where to find your .py module files
                // FromRedistributable()     - Downloads Python automatically (no install needed!)
                builder.Services
                    .WithPython()
                    .WithHome(pythonHome)
                    .FromRedistributable(); // Downloads Python 3.12 and caches locally

                var app = builder.Build();

                // Ensure the IPythonEnvironment interface is defined and the required package is referenced
                IPythonEnvironment pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                var msg = pythonEnv.Hello().HelloWorld("Soren");
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine("This could happen if Python isn't available or if the Python module has errors.");
            }
        }
    }
}
