using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace CSnakesExceptions
{
    /// <summary>
    /// Comprehensive Exception Handling Demo for CSnakes
    /// 
    /// This program demonstrates:
    /// 1. Basic Python exceptions (ZeroDivisionError, TypeError)
    /// 2. Catching and analyzing Python stack traces in C#
    /// 3. Handling custom Python exception types
    /// 4. Exception chaining and context preservation
    /// 5. Best practices for Python-C# error handling
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            // ===== SETUP CSNAKES ENVIRONMENT =====
            var builder = Host.CreateApplicationBuilder(args);
            var home = Path.Join(Environment.CurrentDirectory, ".");
            
            builder.Services
                .WithPython()
                .WithHome(home)
                .FromRedistributable(); // Download Python 3.12 automatically

            var app = builder.Build();
            IPythonEnvironment pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();

            Console.WriteLine("=== CSnakes Exception Handling Demo ===\n");

            // ===== BASIC EXCEPTION DEMOS =====
            Console.WriteLine("PART 1: BASIC EXCEPTION HANDLING");
            Console.WriteLine("================================\n");

            // Demo 1: Basic ZeroDivisionError
            DemoBasicDivisionByZero(pythonEnv);
            
            // Demo 2: Type mismatch (InvalidCastException)
            DemoTypeMismatch(pythonEnv);

            // ===== ADVANCED EXCEPTION DEMOS =====
            Console.WriteLine("\n\nPART 2: ADVANCED EXCEPTION HANDLING");
            Console.WriteLine("===================================\n");

            // Demo 3: Exception with detailed stack trace
            DemoExceptionWithStackTrace(pythonEnv);
            
            // Demo 4: Nested function errors
            DemoNestedFunctionErrors(pythonEnv);
            
            // Demo 5: Custom exception types
            DemoCustomExceptionTypes(pythonEnv);
            
            // Demo 6: Network timeout exception
            DemoNetworkTimeoutException(pythonEnv);
            
            // Demo 7: Exception chaining
            DemoExceptionChaining(pythonEnv);
            
            // Demo 8: Summary of all exception types
            DemoAllExceptionTypes(pythonEnv);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        #region Basic Exception Demos

        /// <summary>
        /// Demo 1: Basic division by zero exception
        /// </summary>
        static void DemoBasicDivisionByZero(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 1: Basic Division by Zero");
            
            try
            {
                // This uses the original test_divide_by_zero function
                pythonEnv.ExeptionTest().TestDivideByZero();
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught PythonInvocationException in C#");
                Console.WriteLine($"  Message: {ex.Message}");
                Console.WriteLine($"  Type: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner Exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"  Inner Message: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Demo 2: Type mismatch exception
        /// </summary>
        static void DemoTypeMismatch(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 2: Type Mismatch (InvalidCastException)");
            
            try
            {
                // Function returns int but is typed as returning string
                string result = pythonEnv.ExeptionTest().UmtimateQuestion();
                Console.WriteLine($"Unexpected success: {result}");
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught type mismatch exception");
                Console.WriteLine($"  Message: {ex.Message}");
                
                if (ex.InnerException is InvalidCastException)
                {
                    Console.WriteLine("  ✓ Inner exception is InvalidCastException as expected");
                }
            }
        }

        #endregion

        #region Advanced Exception Demos

        /// <summary>
        /// Demo 3: Exception with full stack trace information
        /// </summary>
        static void DemoExceptionWithStackTrace(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 3: Exception with Stack Trace");
            
            try
            {
                var result = pythonEnv.ExeptionTest().SimpleDivisionError(10.0, 0.0);
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught exception with stack trace");
                Console.WriteLine("\nPython output (includes stack trace):");
                
                // The Python stack trace is often in the message
                var lines = ex.Message.Split('\n');
                foreach (var line in lines)
                {
                    Console.WriteLine($"  {line}");
                }
                
                Console.WriteLine("\nC# Stack Trace:");
                var stackLines = ex.StackTrace?.Split('\n') ?? Array.Empty<string>();
                // Show only first few lines of C# stack
                for (int i = 0; i < Math.Min(3, stackLines.Length); i++)
                {
                    Console.WriteLine($"  {stackLines[i].Trim()}");
                }
                if (stackLines.Length > 3)
                {
                    Console.WriteLine("  ...");
                }
            }
        }

        /// <summary>
        /// Demo 4: Nested function errors with detailed stack info
        /// </summary>
        static void DemoNestedFunctionErrors(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 4: Nested Function Errors");
            
            try
            {
                pythonEnv.ExeptionTest().NestedFunctionError();
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught nested function error");
                Console.WriteLine($"\nError shows progression through function calls:");
                
                // Extract key information from the error message
                if (ex.Message.Contains("stack_frames"))
                {
                    Console.WriteLine("  ✓ Stack frame information included in error");
                    Console.WriteLine("  ✓ Shows level_1 → level_2 → level_3 call chain");
                }
                
                // Show just the error type and step
                if (ex.Message.Contains("Processing failed at step"))
                {
                    var stepMatch = System.Text.RegularExpressions.Regex.Match(
                        ex.Message, @"step '([^']+)'"
                    );
                    if (stepMatch.Success)
                    {
                        Console.WriteLine($"  Failed at step: {stepMatch.Groups[1].Value}");
                    }
                }
            }
        }

        /// <summary>
        /// Demo 5: Custom Python exception types
        /// </summary>
        static void DemoCustomExceptionTypes(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 5: Custom Exception Types");
            
            // Test 1: Invalid data type
            Console.WriteLine("Test 1: Invalid data type for 'age' field");
            
            try
            {
                // Pass individual parameters - simpler type conversion
                var result = pythonEnv.ExeptionTest().ValidateUserDataSimple(
                    "John Doe",
                    "twenty-five", // This should be a valid number string!
                    "john@example.com"
                );
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught DataValidationError");
                
                // Extract field name from error message
                var fieldMatch = System.Text.RegularExpressions.Regex.Match(
                    ex.Message, @"field '([^']+)'"
                );
                if (fieldMatch.Success)
                {
                    Console.WriteLine($"  Invalid field: {fieldMatch.Groups[1].Value}");
                }
                
                // Extract type mismatch info
                if (ex.Message.Contains("expected int, got str"))
                {
                    Console.WriteLine("  ✓ Correctly identified type mismatch");
                }
            }
            
            // Test 2: Valid data
            Console.WriteLine("\nTest 2: Valid data");
            
            try
            {
                // Pass individual parameters with valid data
                var result = pythonEnv.ExeptionTest().ValidateUserDataSimple(
                    "Jane Doe",
                    "25", // Valid age as string
                    "jane@example.com"
                );
                Console.WriteLine("✓ Validation successful!");
                Console.WriteLine($"  Result: {result}");
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine($"✗ Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Demo 6: Network timeout with retry information
        /// </summary>
        static void DemoNetworkTimeoutException(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 6: Network Timeout Exception");
            
            try
            {
                var result = pythonEnv.ExeptionTest().SimulateNetworkOperation(
                    "https://api.example.com/data", 
                    0.5  // Very short timeout
                );
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught NetworkTimeoutError");
                
                // Extract retry count
                var retryMatch = System.Text.RegularExpressions.Regex.Match(
                    ex.Message, @"(\d+) retries"
                );
                if (retryMatch.Success)
                {
                    Console.WriteLine($"  Retries attempted: {retryMatch.Groups[1].Value}");
                }
                
                // Extract URL
                if (ex.Message.Contains("api.example.com"))
                {
                    Console.WriteLine("  ✓ URL information preserved in exception");
                }
            }
        }

        /// <summary>
        /// Demo 7: Exception chaining
        /// </summary>
        static void DemoExceptionChaining(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 7: Exception Chaining");
            
            // Test Case 1: Empty list
            Console.WriteLine("Test 1: Empty list");
            try
            {
                var emptyList = new List<long>();
                // Pass list directly - CSnakes will handle conversion
                var result = pythonEnv.ExeptionTest().ExceptionChainingDemo(emptyList);
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught chained exception");
                if (ex.Message.Contains("Cannot calculate average of empty list"))
                {
                    Console.WriteLine("  ✓ Original ValueError preserved");
                }
                if (ex.Message.Contains("Processing failed"))
                {
                    Console.WriteLine("  ✓ Wrapped in ProcessingError");
                }
            }
            
            // Test Case 2: Division by zero
            Console.WriteLine("\nTest 2: Average equals 50 (causes division by zero)");
            try
            {
                var problematicList = new List<long> { 50, 50, 50 };
                // Pass list directly - CSnakes will handle conversion
                var result = pythonEnv.ExeptionTest().ExceptionChainingDemo(problematicList);
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine("✓ Caught chained exception");
                if (ex.Message.Contains("100 / (average - 50)"))
                {
                    Console.WriteLine("  ✓ Formula details included");
                }
                if (ex.Message.Contains("ZeroDivisionError"))
                {
                    Console.WriteLine("  ✓ Original error type preserved");
                }
            }
        }

        /// <summary>
        /// Demo 8: Summary of all exception types
        /// </summary>
        static void DemoAllExceptionTypes(IPythonEnvironment pythonEnv)
        {
            PrintDemoHeader("Demo 8: Summary of All Exception Types");
            
            try
            {
                var examples = pythonEnv.ExeptionTest().DemonstrateAllExceptions();
                
                Console.WriteLine("Successfully caught and handled multiple exception types:");
                
                // Parse the returned PyObject
                var jsonStr = examples.ToString();
                if (!string.IsNullOrEmpty(jsonStr) && jsonStr != "None")
                {
                    try
                    {
                        // Simple parsing - in production use proper JSON deserialization
                        if (jsonStr.Contains("Division by Zero"))
                            Console.WriteLine("  ✓ Division by Zero");
                        if (jsonStr.Contains("Nested Function Error"))
                            Console.WriteLine("  ✓ Nested Function Error");
                        if (jsonStr.Contains("Data Validation Error"))
                            Console.WriteLine("  ✓ Data Validation Error");
                        if (jsonStr.Contains("Network Timeout"))
                            Console.WriteLine("  ✓ Network Timeout");
                    }
                    catch
                    {
                        Console.WriteLine("  (Could not parse example details)");
                    }
                }
            }
            catch (PythonInvocationException ex)
            {
                Console.WriteLine($"Error running examples: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to print demo headers
        /// </summary>
        static void PrintDemoHeader(string title)
        {
            Console.WriteLine($"\n{new string('-', 50)}");
            Console.WriteLine(title);
            Console.WriteLine(new string('-', 50));
        }

        #endregion
    }

    /// <summary>
    /// Best Practices for Python-C# Exception Handling:
    /// 
    /// 1. Always wrap Python calls in try-catch blocks
    /// 2. Use PythonInvocationException as the primary catch type
    /// 3. Check InnerException for additional details
    /// 4. Parse error messages for structured data when available
    /// 5. Log full stack traces for debugging
    /// 6. Implement retry logic at the C# level for transient errors
    /// 7. Create user-friendly error messages
    /// 8. Consider creating matching C# exception types for custom Python exceptions
    /// 9. Test exception scenarios thoroughly
    /// 10. Document expected exceptions in method comments
    /// </summary>
}