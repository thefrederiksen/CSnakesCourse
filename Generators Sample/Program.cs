using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Generators_Sample
{
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
            var pyEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Console.WriteLine($"Done – {sw.ElapsedMilliseconds} ms\n");

            // 4. Show the version of the Python code and load the module
            sw.Restart();
            Console.WriteLine($"Code version: {pyEnv.GeneratorsSample().GetVersion()}");
            Console.WriteLine($"Module loaded in {sw.ElapsedMilliseconds} ms\n");

            // 5. Test the Python generator sample
            TestProgressGenerator(pyEnv);

            // 6. Test the Python async generator sample with progress dots
            await TestAsyncProgressBar(pyEnv);
        }

        static void TestProgressGenerator(IPythonEnvironment pyEnv)
        {
            Console.WriteLine("Testing Python progress_generator...");
            var generator = pyEnv.GeneratorsSample().ProgressGenerator();
            foreach (var progress in generator)
            {
                Console.Write($"{progress} ");
            }
            Console.WriteLine("\nDone!");
        }

        static async Task TestAsyncProgressBar(IPythonEnvironment pyEnv)
        {
            Console.WriteLine("Testing Python async progress_generator_async...");
            using var cts = new CancellationTokenSource();
            var dotTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.Write(".");
                    await Task.Delay(200, cts.Token);
                }
            }, cts.Token);

            // Await the async generator from Python (fix: use AsAsyncEnumerable<int>())
            var pyObj = await pyEnv.GeneratorsSample().AsyncProgressBar();
            foreach (var progress in pyObj.AsEnumerable<int>())
            {
                if (progress % 10 == 0)
                    Console.Write($" {progress}");
            }
            cts.Cancel();
            try { await dotTask; } catch (OperationCanceledException) { }
            Console.WriteLine("\nAsync generator done!");
        }
    }
}
