using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HelloWorld;

namespace HelloWorld.Tests;

[TestClass]
[DoNotParallelize] // Prevent parallel execution to avoid Python runtime conflicts
public class HelloWorldTests
{
    // Static fields to hold the Python environment for all tests
    private static IHost? _host;
    private static IPythonEnvironment? _pythonEnv;

    [AssemblyInitialize]
    public static void AssemblySetup(TestContext context)
    {
        try
        {
            Console.WriteLine("Setting up Python environment for HelloWorld tests...");
            
            var builder = Host.CreateApplicationBuilder();
            
            // Configure logging to reduce noise
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            
            // Get the base directory of the test output
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Copy the Python file from HelloWorld project output
            var pythonFile = Path.Combine(baseDir, "hello.py");
            if (!File.Exists(pythonFile))
            {
                var sourceFile = Path.Combine(baseDir, "..", "..", "..", "..", "HelloWorld", "hello.py");
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, pythonFile, true);
                    Console.WriteLine($"Copied hello.py to test directory");
                }
            }
            
            builder.Services
                .WithPython()
                .WithHome(baseDir)
                .FromRedistributable();

            _host = builder.Build();
            _pythonEnv = _host.Services.GetRequiredService<IPythonEnvironment>();
            
            Console.WriteLine("Python environment setup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to setup Python environment: {ex.Message}");
            throw;
        }
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Console.WriteLine("Cleaning up Python environment...");
        _host?.Dispose();
        _host = null;
        _pythonEnv = null;
    }

    [TestMethod]
    public void HelloWorld_ReturnsCorrectGreeting()
    {
        // Arrange
        const string name = "Test User";
        const string expectedMessage = "Hello, Test User - From Python!";

        // Act
        var result = _pythonEnv!.Hello().HelloWorld(name);

        // Assert
        Assert.AreEqual(expectedMessage, result);
    }

    [TestMethod]
    public void HelloWorld_WithEmptyName_ReturnsGreetingWithEmptyName()
    {
        // Arrange
        const string name = "";
        const string expectedMessage = "Hello,  - From Python!";

        // Act
        var result = _pythonEnv!.Hello().HelloWorld(name);

        // Assert
        Assert.AreEqual(expectedMessage, result);
    }

    [TestMethod]
    public void HelloWorld_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        const string name = "Test@#$%User123";
        const string expectedMessage = "Hello, Test@#$%User123 - From Python!";

        // Act
        var result = _pythonEnv!.Hello().HelloWorld(name);

        // Assert
        Assert.AreEqual(expectedMessage, result);
    }
}