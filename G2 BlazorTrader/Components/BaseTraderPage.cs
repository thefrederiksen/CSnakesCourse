using CSnakes.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace BlazorTrader.Components
{
    public abstract class BaseTraderPage : ComponentBase
    {
        [Inject]
        public required IPythonEnvironment PythonEnv { get; set; }

        [Inject]
        public required IConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets the user data directory for storing S&P 500 data and indicators.
        /// Uses the standard Windows ApplicationData folder for user-specific data.
        /// </summary>
        public static string UserDataDirectory
        {
            get
            {
                var userDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "BlazorTrader"
                );
                
                // Ensure the directory exists
                Directory.CreateDirectory(userDataPath);
                
                return userDataPath;
            }
        }

        protected string HandleException(string operation, Exception ex)
        {
            Console.WriteLine($"Error {operation}: {ex.Message}");
            
            // Print inner exception details if available
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Exception Type: {ex.InnerException.GetType().Name}");
                Console.WriteLine("Inner Exception Stack Trace:");
                Console.WriteLine(ex.InnerException.StackTrace);
            }
            
            // Print full exception details
            Console.WriteLine("Full Exception:");
            Console.WriteLine(ex.ToString());

            return $"❌ {operation} failed. Please check the console for error details.";
        }
    }
}
