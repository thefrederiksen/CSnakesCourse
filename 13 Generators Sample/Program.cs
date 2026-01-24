using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Generators_Sample
{
    /// <summary>
    /// CSnakes Course - Python Generators & Iterators
    /// 
    /// Learning Objectives:
    /// - Work with Python generators from C#
    /// - Handle lazy evaluation and streaming data
    /// - Iterate over Python generator objects
    /// - Understand memory efficiency of generators
    /// - Process large datasets without loading all data
    /// - Compare Python generators to C# IEnumerable
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Locate the Python home folder (side-by-side with the EXE)
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var pythonHomeDir = Path.Join(exeDir, "Python");
            var virtualDir = Path.Join(pythonHomeDir, ".venv_uv");
            var requirements = Path.Combine(pythonHomeDir, "requirements.txt");

            // 2. Build the host & configure CSnakes runtime
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                   .WithPython()
                       .WithHome(pythonHomeDir)
                       .FromRedistributable("3.12")
                       .WithVirtualEnvironment(virtualDir)
                       .WithUvInstaller(requirements);

            using var app = builder.Build();

            // 3. Warm-up: create env + install packages (idempotent)
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Creating environment and installing packages...");
            var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Console.WriteLine($"Done – {sw.ElapsedMilliseconds} ms\n");

            // 4. Show the version of the Python code and load the module
            sw.Restart();
            Console.WriteLine($"Code version: {pythonEnv.GeneratorsSample().GetVersion()}");
            Console.WriteLine($"Module loaded in {sw.ElapsedMilliseconds} ms\n");

            // 5. Test the Python generator sample
            TestProgressGenerator(pythonEnv);

            // 6. Test the Python async generator sample with progress dots
            await TestAsyncProgressBar(pythonEnv);
        }

        static void TestProgressGenerator(IPythonEnvironment pythonEnv)
        {
            Console.WriteLine("Testing Python progress_generator...");
            var generator = pythonEnv.GeneratorsSample().ProgressGenerator();
            foreach (var progress in generator)
            {
                Console.Write($"{progress} ");
            }
            Console.WriteLine("\nDone!");
        }

        static async Task TestAsyncProgressBar(IPythonEnvironment pythonEnv)
        {
            // Note: CSnakes does NOT support Python async generators (AsyncGenerator).
            // The workaround is to use a synchronous generator and wrap it in Task.Run().
            // This gives the same user experience (non-blocking streaming) without
            // requiring CSnakes async generator support. This is the pattern used
            // in BlazorTrader production code.

            Console.WriteLine("Testing Python progress_bar_with_delay (sync generator, async C# wrapper)...");
            using var cts = new CancellationTokenSource();
            var dotTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.Write(".");
                    await Task.Delay(200, cts.Token);
                }
            }, cts.Token);

            // Run the sync generator in a background task for non-blocking behavior
            await Task.Run(() =>
            {
                foreach (var progress in pythonEnv.GeneratorsSample().ProgressBarWithDelay())
                {
                    Console.Write($" {progress}");
                }
            });

            cts.Cancel();
            try { await dotTask; } catch (OperationCanceledException) { }
            Console.WriteLine("\nAsync-wrapped generator done!");
        }
    }
}
