using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            var home = Path.Join(Environment.CurrentDirectory, "."); /* Path to your Python modules */
            builder.Services
                .WithPython()
                .WithHome(home)
                .FromRedistributable(); // Download Python 3.12 and store it locally

            var app = builder.Build();

            // Ensure the IPythonEnvironment interface is defined and the required package is referenced
            IPythonEnvironment pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            var msg = pythonEnv.Hello().HelloWorld("Soren");
            Console.WriteLine(msg);
        }
    }
}
