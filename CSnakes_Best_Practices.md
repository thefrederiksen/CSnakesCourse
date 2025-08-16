# CSnakes Integration Best Practices Guide

## Overview
This guide provides comprehensive best practices for integrating Python into C#/.NET applications using the CSnakes runtime, based on patterns observed across 15+ production projects.

## Table of Contents
1. [Project Structure](#project-structure)
2. [Python Environment Setup](#python-environment-setup)
3. [Package Management with UV](#package-management-with-uv)
4. [Configuration Patterns](#configuration-patterns)
5. [Error Handling](#error-handling)
6. [Performance Optimization](#performance-optimization)
7. [Production Deployment](#production-deployment)

---

## Project Structure

### Recommended Directory Layout
```
YourProject/
├── YourProject.csproj
├── Program.cs
├── Python/                    # All Python files in dedicated folder
│   ├── your_module.py
│   ├── requirements.txt
│   └── .venv/                # Virtual environment (auto-created)
└── bin/
    └── Debug/net9.0/
        └── Python/            # Python files copied here on build
```

### Key Principles
1. **Always use a `Python/` directory** - Keep Python files organized in a dedicated folder
2. **Place `requirements.txt` in Python folder** - Keep dependencies close to Python code
3. **Virtual environments go in `.venv` subfolder** - Consistent naming and location
4. **Use `AdditionalFiles` in .csproj** - Ensures Python files are included in build

### .csproj Configuration
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Include CSnakes Runtime -->
    <PackageReference Include="CSnakes.Runtime" Version="1.1.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Python files as AdditionalFiles for code generation -->
    <AdditionalFiles Include="Python\*.py">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </AdditionalFiles>
  </ItemGroup>

  <ItemGroup>
    <!-- Copy requirements.txt to output -->
    <None Update="Python\requirements.txt">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

---

## Python Environment Setup

### Standard Setup Pattern
```csharp
using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 1. Locate Python home directory (relative to executable)
var exeDir = Path.GetDirectoryName(
    System.Reflection.Assembly.GetExecutingAssembly().Location)!;
var pythonHomeDir = Path.Join(exeDir, "Python");
var virtualDir = Path.Join(pythonHomeDir, ".venv");
var requirements = Path.Combine(pythonHomeDir, "requirements.txt");

// 2. Configure CSnakes with UV installer
var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .WithPython()
    .WithHome(pythonHomeDir)
    .FromRedistributable("3.12")      // Auto-downloads Python
    .WithVirtualEnvironment(virtualDir)
    .WithUvInstaller(requirements);    // UV is faster than pip

// 3. Build and start
using var app = builder.Build();
var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
```

### Python Locator Strategies

#### 1. **FromRedistributable() - RECOMMENDED for Production**
```csharp
.FromRedistributable("3.12")  // Downloads & caches Python automatically
```
- Auto-downloads Python on first run
- Ensures consistent Python version
- No system Python dependency
- Best for deployment scenarios

#### 2. **FromEnvironmentVariable() - For Development**
```csharp
.FromEnvironmentVariable("PYTHON_HOME", "3.12")
```
- Uses system Python installation
- Good for development environments
- Requires PYTHON_HOME environment variable

#### 3. **FromFolder() - For Specific Installations**
```csharp
.FromFolder(@"C:\Python312", "3.12")
```
- Points to specific Python installation
- Useful for controlled environments

---

## Package Management with UV

### Why UV Over Pip
- **10-100x faster** than pip for package installation
- **Better dependency resolution**
- **Automatic cleanup** of unused packages
- **Parallel downloads** for faster setup

### Basic UV Setup
```csharp
builder.Services
    .WithPython()
    .WithHome(pythonHomeDir)
    .FromRedistributable("3.12")
    .WithVirtualEnvironment(virtualDir)
    .WithUvInstaller(requirements);  // Automatic installation
```

### Advanced UV Control
```csharp
// For manual control over package installation
builder.Services
    .WithPython()
    .WithHome(pythonHomeDir)
    .FromRedistributable("3.12")
    .WithVirtualEnvironment(virtualDir)
    .WithUvInstaller();  // No requirements file

// Then install manually
using var app = builder.Build();
var installer = app.Services.GetRequiredService<IPythonPackageInstaller>();
await installer.InstallPackagesFromRequirements(pythonHomeDir);
```

### Requirements.txt Best Practices
```txt
# Core dependencies with version pinning
numpy==1.24.3
pandas==2.0.3
scikit-learn==1.3.0

# Use >= for libraries with good backward compatibility
requests>=2.31.0

# Pin exact versions for ML models
xgboost==1.7.6
```

---

## Configuration Patterns

### Console Application Pattern
```csharp
static void Main(string[] args)
{
    try
    {
        var builder = Host.CreateApplicationBuilder(args);
        ConfigurePython(builder.Services);
        
        using var app = builder.Build();
        var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
        
        // Use Python modules
        var result = pythonEnv.YourModule().YourFunction();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

private static void ConfigurePython(IServiceCollection services)
{
    var exeDir = Path.GetDirectoryName(
        System.Reflection.Assembly.GetExecutingAssembly().Location)!;
    var pythonHomeDir = Path.Join(exeDir, "Python");
    var virtualDir = Path.Join(pythonHomeDir, ".venv");
    var requirements = Path.Combine(pythonHomeDir, "requirements.txt");
    
    services
        .WithPython()
        .WithHome(pythonHomeDir)
        .FromRedistributable("3.12")
        .WithVirtualEnvironment(virtualDir)
        .WithUvInstaller(requirements);
}
```

### Web Application Pattern (Blazor/ASP.NET)
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure Python
        var pythonHomeDir = Path.Join(exeDir, "PythonBackend\\Src");
        builder.Services
            .WithPython()
            .WithHome(pythonHomeDir)
            .FromRedistributable("3.12")
            .WithVirtualEnvironment(Path.Join(pythonHomeDir, ".venv"))
            .WithUvInstaller();
        
        // Add web services
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        
        var app = builder.Build();
        
        // Warm up Python environment
        var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
        
        app.Run();
    }
}
```

### Windows Forms/WPF Pattern
```csharp
public partial class MainForm : Form
{
    private IPythonEnvironment? _pythonEnv;
    private IHost? _host;
    
    public MainForm()
    {
        InitializeComponent();
        _ = InitializePythonAsync();  // Fire and forget
    }
    
    private async Task InitializePythonAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        // Configure Python...
        _host = builder.Build();
        _pythonEnv = await Task.Run(() => 
            _host.Services.GetRequiredService<IPythonEnvironment>());
    }
}
```

---

## Error Handling

### Comprehensive Error Handling Pattern
```csharp
try
{
    // Initialize Python environment
    var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
    
    try
    {
        // Call Python functions
        var result = pythonEnv.Module().Function();
    }
    catch (PythonRuntimeException prEx)
    {
        // Python-specific runtime errors
        Console.WriteLine($"Python error: {prEx.Message}");
        Console.WriteLine($"Python traceback: {prEx.StackTrace}");
    }
}
catch (FileNotFoundException fnfEx)
{
    Console.WriteLine($"Python module not found: {fnfEx.Message}");
    Console.WriteLine("Ensure Python files are in the correct directory");
}
catch (Exception ex)
{
    Console.WriteLine($"Initialization error: {ex.Message}");
    Console.WriteLine("This could happen if Python isn't available");
}
```

### Environment Validation
```csharp
// Validate Python environment before use
if (!File.Exists(requirements))
{
    throw new FileNotFoundException(
        $"requirements.txt not found at: {requirements}");
}

if (!Directory.Exists(pythonHomeDir))
{
    throw new DirectoryNotFoundException(
        $"Python directory not found at: {pythonHomeDir}");
}
```

---

## Performance Optimization

### 1. Use Virtual Environments with UV
```csharp
// UV caches packages and reuses them across projects
.WithVirtualEnvironment(virtualDir)
.WithUvInstaller(requirements)
```

### 2. Warm-up Python Environment
```csharp
// Measure and optimize startup time
var sw = Stopwatch.StartNew();
var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
Console.WriteLine($"Python initialized in {sw.ElapsedMilliseconds}ms");

// Pre-load critical modules
var criticalModule = pythonEnv.CriticalModule();
Console.WriteLine($"Module loaded in {sw.ElapsedMilliseconds}ms");
```

### 3. Use Zero-Copy Buffer Sharing for NumPy
```csharp
// Share memory between C# and Python without copying
IPyBuffer buffer = pythonEnv.Module().GetNumpyArray();
ReadOnlySpan<float> data = buffer.AsFloatReadOnlySpan();
// Process data without copying
```

### 4. Async Operations for Long-Running Tasks
```csharp
// Run Python operations asynchronously
var result = await Task.Run(() => 
    pythonEnv.Module().LongRunningOperation());
```

---

## Production Deployment

### 1. Configuration and Secrets Management

#### Development/Testing Only - .env Files
```csharp
// ONLY for local development and testing - NEVER for production
public static class EnvLoader
{
    public static void Load()
    {
        #if DEBUG
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }
        }
        #else
        throw new InvalidOperationException(".env files should not be used in production");
        #endif
    }
}
```

#### Production Secret Management
```csharp
// Use proper secret management for production
public class SecureConfiguration
{
    public static void ConfigureSecrets(WebApplicationBuilder builder)
    {
        // Azure Key Vault
        if (!string.IsNullOrEmpty(builder.Configuration["KeyVault:Uri"]))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(builder.Configuration["KeyVault:Uri"]),
                new DefaultAzureCredential());
        }
        
        // AWS Secrets Manager
        builder.Configuration.AddSecretsManager(configurator: options =>
        {
            options.SecretFilter = entry => entry.Name.StartsWith("myapp/");
            options.KeyGenerator = (entry, key) => key.Replace("myapp/", string.Empty);
        });
        
        // User Secrets (for local development only)
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>();
        }
    }
}

// Access secrets safely
var apiKey = builder.Configuration["OpenAI:ApiKey"];  // From secure store
var connectionString = builder.Configuration.GetConnectionString("Database");
```

#### Environment-Specific Configuration
```csharp
// appsettings.json - Public configuration
{
  "Python": {
    "Version": "3.12",
    "VirtualEnvPath": ".venv"
  }
}

// appsettings.Production.json - Production-specific (no secrets)
{
  "Python": {
    "UseRedistributable": true,
    "EnableLogging": false
  }
}

// Secrets stored in:
// - Azure Key Vault
// - AWS Secrets Manager  
// - HashiCorp Vault
// - Environment variables (from secure CI/CD)
// - Kubernetes secrets
```

### 2. Logging and Monitoring
```csharp
private void LogPythonInitialization()
{
    Console.WriteLine($"Python Home: {pythonHomeDir}");
    Console.WriteLine($"Virtual Env: {virtualDir}");
    Console.WriteLine($"Requirements: {requirements}");
    
    // Log package versions
    var packages = File.ReadAllLines(requirements);
    Console.WriteLine("Python packages:");
    foreach (var package in packages)
    {
        Console.WriteLine($"  - {package}");
    }
}
```

### 3. Deployment Checklist
- ✅ Use `FromRedistributable()` for consistent Python version
- ✅ Include all Python files as `AdditionalFiles` in .csproj
- ✅ Copy `requirements.txt` to output directory
- ✅ Use UV installer for faster deployment
- ✅ Test virtual environment creation on clean machine
- ✅ Implement comprehensive error handling
- ✅ Add logging for diagnostics
- ✅ Consider Python warm-up in application startup
- ✅ **NEVER use .env files in production** - use proper secret stores
- ✅ Configure secrets via Azure Key Vault, AWS Secrets Manager, or similar
- ✅ Use environment-specific configuration files (appsettings.Production.json)
- ✅ Validate all secrets are loaded from secure sources before startup

### 4. Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Python will be auto-downloaded by FromRedistributable()
# No need to install Python in Docker image

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

---

## Quick Reference

### Minimal CSnakes Setup
```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services
    .WithPython()
    .WithHome(Path.Join(AppContext.BaseDirectory, "Python"))
    .FromRedistributable("3.12")
    .WithVirtualEnvironment(".venv")
    .WithUvInstaller("requirements.txt");

using var app = builder.Build();
var python = app.Services.GetRequiredService<IPythonEnvironment>();
```

### Common Issues and Solutions

| Issue | Solution |
|-------|----------|
| Python module not found | Ensure .py files are marked as `AdditionalFiles` with `CopyToOutputDirectory` |
| Packages not installing | Check requirements.txt path and format |
| Slow startup | Use UV instead of pip, cache virtual environment |
| Version conflicts | Pin package versions in requirements.txt |
| Deployment failures | Use `FromRedistributable()` for consistent Python |

---

## Summary

The key to successful CSnakes integration is:
1. **Consistent project structure** with Python/ directory
2. **UV package manager** for fast, reliable installations
3. **Virtual environments** for isolation
4. **FromRedistributable()** for deployment
5. **Comprehensive error handling** at all levels
6. **Performance monitoring** and optimization

Follow these patterns for robust Python integration in your C#/.NET applications.