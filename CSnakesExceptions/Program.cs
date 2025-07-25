using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CSnakesExceptions
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

            // Write the test name to console
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Testing PythonInvocationException with DivideByZeroException");
            Console.WriteLine(new string('-', 50));

            try
            {
                var msg = pythonEnv.ExeptionTest().TestDivideByZero();
            }
            catch (PythonInvocationException ex)
            {
                // Write exception to console with inner and stack trace
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                // Print inner exception details if available
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception - Message: {ex.InnerException.Message}");
                    Console.WriteLine($"   Inner Exception - Type: {ex.InnerException.GetType().Name}");
                    Console.WriteLine("   Inner Exception  - Stack Trace:");
                    Console.WriteLine(ex.InnerException.StackTrace);
                }
            }

            // Create spaces on console and write next test name
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Testing PythonInvocationException with InvalidCastException");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine();

            // Test InvalidCastException 
            try
            {
                pythonEnv.ExeptionTest().UmtimateQuestion();
            }
            catch (PythonInvocationException ex)
            {
                // Write exception to console with inner and stack trace
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                // Print inner exception details if available
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception - Message: {ex.InnerException.Message}");
                    Console.WriteLine($"   Inner Exception - Type: {ex.InnerException.GetType().Name}");
                    Console.WriteLine("   Inner Exception  - Stack Trace:");
                    Console.WriteLine(ex.InnerException.StackTrace);
                }
            }

        }
    }
}
