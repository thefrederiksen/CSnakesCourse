using CSnakes.Runtime;
using CSnakes.Runtime.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NumPy1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== CSnakes Lab : NumPy1 ===\n");

            // ── 1. Locate the Python home folder (sits beside the EXE) ──────────
            var exeDir = Path.GetDirectoryName(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var pythonHomeDir = Path.Join(exeDir, "Python");          // contains kmeans_sample.py
            var virtualDir = Path.Join(pythonHomeDir, ".venv_uv"); // will be created on first run
            var requirements = Path.Combine(pythonHomeDir, "requirements.txt");

            // ── 2. Build the host & configure CSnakes runtime ──────────────────
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                   .WithPython()
                       .WithHome(pythonHomeDir)
                       .FromRedistributable("3.12")    // downloads CPython the very first time
                       .WithVirtualEnvironment(virtualDir)
                       .WithUvInstaller(requirements);

            using var host = builder.Build();
            await host.StartAsync();

            var env = host.Services.GetRequiredService<IPythonEnvironment>();
            var testModule = env.NumpyBuffer(); // Maps to numpy_buffer.py

            // Call example_array()
            IPyBuffer buffer = testModule.ExampleArray();
            ReadOnlySpan<bool> values = buffer.AsBoolReadOnlySpan();

            Console.WriteLine("Original NumPy Array:");
            for (int i = 0; i < values.Length; i++)
            {
                Console.WriteLine($"Index {i}: {values[i]}");
            }
        }
    }
}
