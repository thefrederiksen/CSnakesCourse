using CSnakes.Runtime;
using CSnakes.Runtime.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CSnakesLab2;

/// <summary>
/// CSnakes Course - Primitives & Return Types
/// 
/// Learning Objectives:
/// - Understand C# ↔ Python primitive type mapping (int→long, float→double, etc.)
/// - Work with strings, booleans, and numeric types across languages
/// - Handle default parameters and optional values
/// - Use nullable types and None values
/// - Work with DateTime objects and conversion strategies
/// - Understand bytes interop between C# and Python
/// - Handle void/None returning functions
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        // Set console encoding to UTF-8 for proper character display
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Ignore encoding errors on some systems
        }

        Console.WriteLine("=== CSnakes Lab 2: Primitives & Return Types ===\n");

        // Create the host with CSnakes configuration using ApplicationBuilder pattern
        var builder = Host.CreateApplicationBuilder(args);
        var pythonHome = Environment.CurrentDirectory; // Path to your Python modules

        builder.Services
            .WithPython()
            .WithHome(pythonHome)
            .FromRedistributable(); // Download Python 3.12 and store it locally

        var app = builder.Build();

        // Get the Python environment
        IPythonEnvironment pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();

        Console.WriteLine("Python environment initialized successfully!\n");

        // Test basic arithmetic operations
        TestArithmeticOperations(pythonEnv);

        // Test string operations
        TestStringOperations(pythonEnv);

        // Test boolean operations
        TestBooleanOperations(pythonEnv);

        // Test complex function with multiple parameters
        TestComplexFunction(pythonEnv);

        // Test default parameters and nullable types
        TestDefaultParametersAndNullables(pythonEnv);

        // Test datetime functions
        TestDateTimeFunctions(pythonEnv);

        // Test bytes interop
        TestBytesInterop(pythonEnv);

        // Test dummy None return
        TestWriteToFile(pythonEnv);

        Console.WriteLine("\n=== Lab 2 Complete! ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        app.StopAsync().Wait();
    }

    static void TestArithmeticOperations(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing Arithmetic Operations ---");

        // Test add_numbers (int + float -> float)
        var sum = pythonEnv.Primitives().AddNumbers(42, 3.14f);
        Console.WriteLine($"add_numbers(42, 3.14): {sum} (Type: {sum.GetType().Name})");

        // Test multiply_integers (int * int -> int)
        var product = pythonEnv.Primitives().MultiplyIntegers(7, 8);
        Console.WriteLine($"multiply_integers(7, 8): {product} (Type: {product.GetType().Name})");

        // Test divide_with_default (with default parameter)
        var division1 = pythonEnv.Primitives().DivideWithDefault(10);
        Console.WriteLine($"divide_with_default(10): {division1} (using default divisor)");

        var division2 = pythonEnv.Primitives().DivideWithDefault(10, 3);
        Console.WriteLine($"divide_with_default(10, 3): {division2}");

        // Test division by zero handling
        var divisionByZero = pythonEnv.Primitives().DivideWithDefault(10, 0);
        Console.WriteLine($"divide_with_default(10, 0): {divisionByZero} (should be Infinity)");

        Console.WriteLine();
    }

    static void TestStringOperations(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing String Operations ---");

        // Test string concatenation
        var greeting = pythonEnv.Primitives().ConcatenateStrings("Hello, ", "CSnakes!");
        Console.WriteLine($"concatenate_strings('Hello, ', 'CSnakes!'): {greeting}");

        // Test string with international characters (more reliable than emojis)
        var internationalChars = pythonEnv.Primitives().ConcatenateStrings("Héllo: ", "Wörld! 你好");
        Console.WriteLine($"concatenate_strings with international chars: {internationalChars}");

        // Test string with special characters
        var specialChars = pythonEnv.Primitives().ConcatenateStrings("Special: ", "!@#$%^&*()");
        Console.WriteLine($"concatenate_strings with special chars: {specialChars}");

        Console.WriteLine();
    }

    static void TestBooleanOperations(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing Boolean Operations ---");

        // Test boolean returns
        var isPositive1 = pythonEnv.Primitives().IsPositive(5.5);
        Console.WriteLine($"is_positive(5.5): {isPositive1}");

        var isPositive2 = pythonEnv.Primitives().IsPositive(-2.1);
        Console.WriteLine($"is_positive(-2.1): {isPositive2}");

        // Test range validation with default parameters
        var inRange1 = pythonEnv.Primitives().ValidateRange(50);
        Console.WriteLine($"validate_range(50): {inRange1} (using defaults 0-100)");

        var inRange2 = pythonEnv.Primitives().ValidateRange(150, 0, 100);
        Console.WriteLine($"validate_range(150, 0, 100): {inRange2}");

        var inRange3 = pythonEnv.Primitives().ValidateRange(75, 50, 100);
        Console.WriteLine($"validate_range(75, 50, 100): {inRange3}");

        Console.WriteLine();
    }

    static void TestComplexFunction(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing Complex Function with Multiple Types ---");

        // Test function with multiple parameter types
        var greeting1 = pythonEnv.Primitives().FormatGreeting("Alice", 25, 1.65f, true);
        Console.WriteLine($"Student greeting: {greeting1}");

        var greeting2 = pythonEnv.Primitives().FormatGreeting("Bob", 35, 1.80f, false);
        Console.WriteLine($"Professional greeting: {greeting2}");

        // Test percentage calculation
        var percentage = pythonEnv.Primitives().CalculatePercentage(75.0, 200.0);
        Console.WriteLine($"calculate_percentage(75, 200): {percentage}%");

        Console.WriteLine();
    }

    static void TestDefaultParametersAndNullables(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing Default Parameters and Nullable Types ---");

        // Test optional message function
        var message1 = pythonEnv.Primitives().GetOptionalMessage(true);
        Console.WriteLine($"get_optional_message(true): {message1 ?? "NULL"}");

        var message2 = pythonEnv.Primitives().GetOptionalMessage(false);
        Console.WriteLine($"get_optional_message(false): {message2 ?? "NULL"}");

        // Test nullable input processing
        var result1 = pythonEnv.Primitives().ProcessNullableInput(42);
        Console.WriteLine($"process_nullable_input(42): {result1}");

        var result2 = pythonEnv.Primitives().ProcessNullableInput(null);
        Console.WriteLine($"process_nullable_input(null): {result2}");

        // Demonstrate passing PyObject.None explicitly
        Console.WriteLine("\n--- Testing PyObject.None ---");
        try
        {
            // This demonstrates how to pass None from C# to Python
            var noneResult = pythonEnv.Primitives().ProcessNullableInput(null);
            Console.WriteLine($"Passing null from C# maps to None in Python: {noneResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling None: {ex.Message}");
        }

        Console.WriteLine();
    }

    static void TestDateTimeFunctions(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing DateTime Functions ---");

        try
        {
            // Test GetCurrentTime() -> datetime (returns PyObject representing Python datetime)
            PyObject currentTime = pythonEnv.Primitives().GetCurrentTime();
            Console.WriteLine($"GetCurrentTime(): {currentTime} (Type: {currentTime.GetType().Name})");

            // Test GetCurrentTimeAsText() -> string
            var currentTimeText = pythonEnv.Primitives().GetCurrentTimeAsText();
            Console.WriteLine($"GetCurrentTimeAsText(): {currentTimeText}");

            Console.WriteLine("\n--- Converting Python datetime to C# DateTime ---");

            // Method 1: Using CSnakes-generated C# wrapper (string approach)
            var dtText = pythonEnv.Primitives().GetCurrentTimeAsText();
            DateTime dt = DateTime.Parse(dtText);
            Console.WriteLine($"Method 1 - Parse from string:");
            Console.WriteLine($"  Original string: {dtText}");
            Console.WriteLine($"  Parsed DateTime: {dt}");
            Console.WriteLine($"  DateTime Kind: {dt.Kind}");

            // Method 2: For GetCurrentTime (returns PyObject), use PyObject methods
            var pyDt = pythonEnv.Primitives().GetCurrentTime();
            string dtIso = pyDt.GetAttr("isoformat").Call().As<string>();
            DateTime dtParsed = DateTime.Parse(dtIso);
            Console.WriteLine($"\nMethod 2 - PyObject isoformat:");
            Console.WriteLine($"  PyObject: {pyDt}");
            Console.WriteLine($"  ISO format: {dtIso}");
            Console.WriteLine($"  Parsed DateTime: {dtParsed}");
            Console.WriteLine($"  DateTime Kind: {dtParsed.Kind}");

            // Method 3: Alternative - using strftime for custom formatting
            string customFormat = pyDt.GetAttr("strftime").Call(CSnakes.Runtime.Python.PyObject.From("%Y-%m-%d %H:%M:%S")).As<string>();
            Console.WriteLine($"\nMethod 3 - Custom strftime format:");
            Console.WriteLine($"  Custom format: {customFormat}");
            if (DateTime.TryParseExact(customFormat, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out DateTime customParsed))
            {
                Console.WriteLine($"  Parsed DateTime: {customParsed}");
            }

            // Demonstrate the difference between Python datetime object and string representation
            Console.WriteLine("\n--- Comparison of All Methods ---");
            Console.WriteLine($"Python datetime object: {currentTime}");
            Console.WriteLine($"ISO string representation: {currentTimeText}");
            Console.WriteLine($"C# DateTime (Method 1): {dt}");
            Console.WriteLine($"C# DateTime (Method 2): {dtParsed}");
            Console.WriteLine($"Python datetime type: {currentTime.GetType().Name}");
            Console.WriteLine($"String type: {currentTimeText.GetType().Name}");
            Console.WriteLine($"C# DateTime type: {dt.GetType().Name}");

            // Show time difference calculations
            Console.WriteLine("\n--- Time Calculations ---");
            var timeDiff = dtParsed - dt;
            Console.WriteLine($"Time difference between methods: {timeDiff.TotalMilliseconds} ms");
            
            // Show how to work with the C# DateTime
            Console.WriteLine($"Add 1 hour: {dt.AddHours(1)}");
            Console.WriteLine($"Format as local time: {dt.ToLocalTime()}");
            Console.WriteLine($"Format as UTC: {dt.ToUniversalTime()}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing datetime functions: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine();
    }

    static void TestBytesInterop(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing Bytes Interop ---");
        // Create a byte array with values 1-10
        byte[] original = Enumerable.Range(1, 10).Select(i => (byte)i).ToArray();
        Console.WriteLine($"Original bytes:   [{string.Join(", ", original)}]");

        // Call Python reverse_bytes
        var reversed = pythonEnv.Primitives().ReverseBytes(original);
        Console.WriteLine($"Reversed bytes:   [{string.Join(", ", reversed)}]");
        Console.WriteLine();
    }

    static void TestWriteToFile(IPythonEnvironment pythonEnv)
    {
        Console.WriteLine("--- Testing WriteToFile (void/None return) ---");
        string filename = "dummy.txt";
        string fileText = "This is a test file text.";
        pythonEnv.Primitives().WriteToFile(filename, fileText);
        Console.WriteLine($"Called WriteToFile('{filename}', '{fileText}') in Python (no return value expected).");
        Console.WriteLine();
    }
}