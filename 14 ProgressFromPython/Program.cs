using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProgressFromPython
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Testing Progress from Python");
            Console.WriteLine(new string('-', 50));

            try
            {
                foreach (long progress in pythonEnv.ProgressTest().ProgressBar())
                {
                    // Write the progress to console and the time
                    Console.WriteLine($"Progress: {progress}% at {DateTime.Now}");

                }
            }
            catch (Exception ex)
            {
                // Write everything about exception, also the stack trace and inner exceptions
                Console.WriteLine($"An error occurred: {ex}");

            }


            //Console.WriteLine(new string('-', 50));
            //Console.WriteLine("Testing Progress from Python using Async");
            //Console.WriteLine(new string('-', 50));

            //await RunAsyncProgressBar(pythonEnv);

            app.Dispose();
        }

        static async Task RunAsyncProgressBar(IPythonEnvironment pythonEnv)
        {
            try
            {
                // TODO: I have written this issue to Anthony. It seems to generate incorrect code!?

                //await foreach (var progress in pythonEnv.ProgressTest().AsyncProgressBar())
                //{
                //    Console.WriteLine($"[ASYNC] Progress: {progress}% at {DateTime.Now}");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An async error occurred: {ex}");
            }
        }
    }
}
