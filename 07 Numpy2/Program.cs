using CommunityToolkit.HighPerformance;
using CSnakes.Runtime;
using CSnakes.Runtime.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Numerics.Tensors; // .NET 9 feature

namespace Numpy2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== CSnakes Lab : NumPy2 ===\n");

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
            

            Console.WriteLine("== 2D Span ==");
            var buf2D = env.NdArrayDemo().ExampleArray2d();

            ReadOnlySpan2D<int> mat = buf2D.AsInt32ReadOnlySpan2D();
            Console.WriteLine($"mat[0,0]: {mat[0, 0]}"); // 1
            Console.WriteLine($"mat[1,2]: {mat[1, 2]}"); // 6

            Console.WriteLine("\n== TensorSpan (N-Dimensional) ==");
            var tensorBuf = env.NdArrayDemo().ExampleTensor();

            var tensor = tensorBuf.AsInt32TensorSpan();
            Console.WriteLine($"tensor[0,0,0,0]: {tensor[0, 0, 0, 0]}"); // 1
            Console.WriteLine($"tensor[1,2,3,4]: {tensor[1, 2, 3, 4]}"); // 3
        }
    }
}
