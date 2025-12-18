using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: DoNotParallelize]

namespace HelloWorld.Tests;

/// <summary>
/// CSnakes Course - Unit Testing with Python Integration
/// 
/// Learning Objectives:
/// - Write unit tests for CSnakes applications
/// - Set up Python environment in test context
/// - Handle test isolation with Python runtime
/// - Test Python function calls from C# unit tests
/// - Manage test lifecycle with Python dependencies
/// </summary>
[TestClass]
public class HelloWorldTests
{
    // Static fields to hold the Python environment for all tests
    private static IHost? _host;
    private static IPythonEnvironment? _pythonEnv;

    // Instance property - MSTest injects fresh TestContext for each test
    public TestContext TestContext { get; set; } = null!;

    [AssemblyInitialize]
    public static void AssemblySetup(TestContext context)
    {
        // Use the context parameter directly - don't store it statically
        context.WriteLine("Setting up Python environment...");

        var builder = Host.CreateApplicationBuilder();

        // Configure logging to reduce noise
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Python files are copied via project file configuration
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        builder.Services
            .WithPython()
            .WithHome(baseDir)
            .FromRedistributable();

        _host = builder.Build();
        _pythonEnv = _host.Services.GetRequiredService<IPythonEnvironment>();

        context.WriteLine("Python environment ready");
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        _host?.Dispose();
        _host = null;
        _pythonEnv = null;
    }

    [TestMethod]
    public void HelloWorld_ReturnsCorrectGreeting()
    {
        // Arrange
        const string name = "Test User";
        const string expected = "Hello, Test User - From Python!";

        // Act
        var result = _pythonEnv!.Hello().HelloWorld(name);

        // Output
        TestContext.WriteLine($"Input: {name}");
        TestContext.WriteLine($"Expected: {expected}");
        TestContext.WriteLine($"Actual: {result}");

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void HelloWorld_WithEmptyName_ReturnsGreetingWithEmptyName()
    {
        // Arrange
        const string name = "";
        const string expected = "Hello,  - From Python!";

        // Act
        var result = _pythonEnv!.Hello().HelloWorld(name);

        // Output
        TestContext.WriteLine($"Input: (empty string)");
        TestContext.WriteLine($"Expected: {expected}");
        TestContext.WriteLine($"Actual: {result}");

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void HelloWorld_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        const string name = "Test@#$%User123";
        const string expected = "Hello, Test@#$%User123 - From Python!";

        // Act
        var result = _pythonEnv!.Hello().HelloWorld(name);

        // Output
        TestContext.WriteLine($"Input: {name}");
        TestContext.WriteLine($"Expected: {expected}");
        TestContext.WriteLine($"Actual: {result}");

        // Assert
        Assert.AreEqual(expected, result);
    }
}