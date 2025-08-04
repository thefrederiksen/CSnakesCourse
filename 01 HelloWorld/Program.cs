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
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var builder = Host.CreateApplicationBuilder(args);
                var pythonHome = Environment.CurrentDirectory; /* Path to your Python modules */
                builder.Services
                    .WithPython()
                    .WithHome(pythonHome)
                    .FromRedistributable(); // Download Python 3.12 and store it locally

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
