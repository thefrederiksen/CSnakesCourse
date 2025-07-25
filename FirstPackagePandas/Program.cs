using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FirstPackagePandas
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Configuring Python");
            var builder = Host.CreateApplicationBuilder(args);
            var home = Path.Join(Environment.CurrentDirectory, "."); /* Path to your Python modules */
            builder.Services
                .WithPython()
                .WithHome(home)
                .FromRedistributable() // Download Python 3.12 and store it locally
                .WithVirtualEnvironment(Path.Join(home, ".venv"))
                .WithUvInstaller(); // Installs packages from requirements.txt

            var app = builder.Build();

            // Ensure the IPythonEnvironment interface is defined and the required package is referenced
            IPythonEnvironment pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Console.WriteLine("Python is ready");

            CSnakes.Runtime.Python.PyObject salesData = pythonEnv.Mypandas().CreateSalesData(200);
            Console.WriteLine($"Sales Data: {salesData.ToString()}");

            var pivotData = pythonEnv.Mypandas().PivotRevenueByProductRegion(salesData);
            Console.WriteLine($"Pivot Data: {pivotData.ToString()}");
        }
    }
}
