# Lab 4: Managing Python Runtimes & Virtual Environments

## 🎯 Learning Objectives

By the end of this lab, you'll master:
- Different Python locator strategies in CSnakes
- Virtual environment configuration and management
- Production deployment scenarios
- Environment validation and troubleshooting

## 🏗️ Project Structure

```
Lab4_PythonRuntimeManagement/
├── Program.cs                    # Main application with locator demos
├── environment_test.py           # Python module for environment testing
├── Lab4_PythonRuntimeManagement.csproj
└── README.md                     # This file
```

## 🚀 Quick Start

### 1. Clone and Build
```bash
git clone <repository-url>
cd Lab4_PythonRuntimeManagement
dotnet build
```

### 2. Run the Lab
```bash
dotnet run
```

The application will test different Python locator strategies and show you which ones work in your environment.

## 🔧 Python Locator Strategies

### 1. FromRedistributable() ⭐ (Recommended for Development)
**When to use**: Development, testing, quick prototypes
**Pros**: Zero setup, automatic downloads, cross-platform
**Cons**: Initial download time, not suitable for production

```csharp
builder.Services
    .WithPython()
    .WithHome(home)
    .FromRedistributable(); // Downloads Python 3.12 automatically
```

### 2. FromEnvironmentVariable() 🌍 (CI/CD Friendly)
**When to use**: CI/CD pipelines, containerized deployments
**Pros**: Flexible, environment-specific configuration
**Cons**: Requires environment setup

```csharp
// Set PYTHON_HOME environment variable first
builder.Services
    .WithPython()
    .WithHome(home)
    .FromEnvironmentVariable("PYTHON_HOME");
```

**Setup:**
```bash
# Windows
set PYTHON_HOME=C:\Python312

# Linux/macOS
export PYTHON_HOME=/usr/bin/python3
```

### 3. FromFolder() 📁 (Direct Control)
**When to use**: Known Python installations, corporate environments
**Pros**: Direct control, no environment variables needed
**Cons**: Hard-coded paths, less flexible

```csharp
var pythonPath = @"C:\Python312"; // Windows
// var pythonPath = "/usr/bin/python3"; // Linux/macOS

builder.Services
    .WithPython()
    .WithHome(home)
    .FromFolder(pythonPath);
```

### 4. FromVirtualEnvironment() 🔒 (Dependency Isolation)
**When to use**: Project-specific dependencies, team collaboration
**Pros**: Dependency isolation, reproducible environments
**Cons**: Requires virtual environment setup

```csharp
var venvPath = Path.Combine(Environment.CurrentDirectory, ".venv");
builder.Services
    .WithPython()
    .WithHome(home)
    .WithVirtualEnvironment(venvPath);
```

**Setup:**
```bash
# Create virtual environment
python -m venv .venv

# Activate (Windows)
.venv\Scripts\activate

# Activate (Linux/macOS)
source .venv/bin/activate

# Install packages
pip install numpy pandas requests

# Deactivate
deactivate
```

### 5. FromConda() 🅒 (Data Science Teams)
**When to use**: Data science workflows, Anaconda environments
**Pros**: Rich package ecosystem, environment management
**Cons**: Requires Anaconda/Miniconda installation

```csharp
builder.Services
    .WithPython()
    .WithHome(home)
    .FromConda()
    .WithCondaEnvironment("myproject");
```

**Setup:**
```bash
# Create conda environment
conda create -n myproject python=3.12

# Activate
conda activate myproject

# Install packages
conda install numpy pandas scikit-learn
```

## 🛠️ Advanced Configuration Examples

### Multi-Environment Setup
```csharp
// Configure based on environment
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

if (isDevelopment)
{
    // Development: Use redistributable for convenience
    builder.Services
        .WithPython()
        .WithHome(home)
        .FromRedistributable();
}
else
{
    // Production: Use environment variable
    builder.Services
        .WithPython()
        .WithHome(home)
        .FromEnvironmentVariable("PYTHON_HOME");
}
```

### Fallback Strategy
```csharp
// Try multiple strategies with error handling
try
{
    builder.Services
        .WithPython()
        .WithHome(home)
        .FromEnvironmentVariable("PYTHON_HOME");
}
catch
{
    // Fallback to redistributable
    builder.Services
        .WithPython()
        .WithHome(home)
        .FromRedistributable();
}
```

## 🐛 Troubleshooting Guide

### Common Issues

#### ❌ "Python not found"
**Symptoms**: CSnakes can't locate Python installation
**Solutions**:
1. Verify the path exists and contains `python.exe` (Windows) or `python3` (Linux/macOS)
2. Check file permissions - CSnakes needs read access
3. Use absolute paths instead of relative paths

#### ❌ "Module not found" 
**Symptoms**: Python modules/packages aren't available
**Solutions**:
1. Ensure you're pointing to the correct environment
2. Verify packages are installed: `pip list` or `conda list`
3. Check virtual environment activation

#### ❌ "Version mismatch"
**Symptoms**: Unsupported Python version
**Solutions**:
1. CSnakes supports Python 3.9-3.13
2. Use `python --version` to verify
3. Update Python or use compatible version

### Environment Validation
The lab includes an environment validation function that checks:
- Python version and paths
- Virtual environment status  
- Package availability
- Configuration recommendations

```csharp
// Add debugging code to your application
var pythonInfo = pythonEnv.Information;
Console.WriteLine($"Python Version: {pythonInfo.Version}");
Console.WriteLine($"Python Path: {python