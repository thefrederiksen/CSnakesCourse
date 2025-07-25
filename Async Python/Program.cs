using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Threading;

namespace Async_Python
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            // ── 1. Locate the Python home folder (side-by-side with the EXE) ────
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var pythonHomeDir = Path.Join(exeDir, "Python");          // contains format_markdown.py
            var virtualDir = Path.Join(pythonHomeDir, ".venv_uv"); // created on first run
            var requirements = Path.Combine(pythonHomeDir, "requirements.txt");

            // ── 2. Build the host & configure CSnakes runtime ──────────────────
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                   .WithPython()
                       .WithHome(pythonHomeDir)
                       .FromRedistributable("3.12")       // downloads CPython on the first run
                       .WithVirtualEnvironment(virtualDir)
                       .WithUvInstaller(requirements);

            using var app = builder.Build();

            // ── 3. Warm-up: create env + install packages (idempotent) ─────────
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Creating environment and installing packages...");
            var pyEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Console.WriteLine($"Done – {sw.ElapsedMilliseconds} ms\n");

            // ── 4. Show the version of the Python code and load the module ─────
            sw.Restart();
            Console.WriteLine($"Code version: {pyEnv.AsyncSample().GetVersion()}");
            Console.WriteLine($"Module loaded in {sw.ElapsedMilliseconds} ms\n");

            // ── 5. Call and display the async method result with progress dots ─
            Console.WriteLine("Calling async Python coroutine (wait_five_seconds)...");
            using var cts = new CancellationTokenSource();
            var dotTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.Write(".");
                    await Task.Delay(500, cts.Token);
                }
            }, cts.Token);

            var result = await pyEnv.AsyncSample().WaitFiveSeconds("CSnakes User");
            cts.Cancel();
            try { await dotTask; } catch (OperationCanceledException) { }
            Console.WriteLine(); // Move to next line after dots
            Console.WriteLine($"Result: {result}");
        }
    }
}
