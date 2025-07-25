using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestPython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var home = Path.Join(Environment.CurrentDirectory, "."); /* Path to your Python modules */

            string requirements = "requirements.txt";

            builder.Services
                .WithPython()
                .WithHome(home)
                .FromRedistributable() // Ensures Python is available 

                .WithVirtualEnvironment(Path.Join(home, ".venv"))
                .WithUvInstaller(requirements); // Install packages like numpy automatically

            var app = builder.Build();

            // Ensure the IPythonEnvironment interface is defined and the required package is referenced
            IPythonEnvironment pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            pythonEnv.TestPython().FetchAndSaveSp500Symbols();
            Console.WriteLine("DONE");
        }
    }
}
