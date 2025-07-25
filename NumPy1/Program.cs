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
            var home = Path.Join(Environment.CurrentDirectory, ".");
            string requirements = "requirements.txt"; // Text file listing numpy, etc.

            var builder = Host.CreateApplicationBuilder();

            var services = builder.Services
                .WithPython()
                .WithHome(home)
                .FromRedistributable() // Ensures Python is available 

                .WithVirtualEnvironment(Path.Join(home, ".venv"))
                .WithUvInstaller(requirements); // Install packages like numpy automatically

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
