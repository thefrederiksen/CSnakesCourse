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
            var home = Path.Join(Environment.CurrentDirectory, ".");
            string requirements = "requirements.txt";

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
