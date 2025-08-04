# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This repository contains a comprehensive CSnakes course that demonstrates C# and Python interoperability using the CSnakes runtime. The project is organized as 15+ lessons that progressively introduce concepts from basic hello world applications to advanced trading systems with machine learning integration.

## Architecture

The repository is structured as a Visual Studio solution with multiple projects representing different lessons:

- **Course Structure**: 15+ distinct projects covering CSnakes fundamentals
- **Core Technology**: CSnakes.Runtime for C#/Python interoperability  
- **Target Framework**: .NET 9.0
- **Python Integration**: Uses CSnakes to execute Python code from C# applications
- **Python Versions**: Supports Python 3.9 through 3.13

### Key Project Types

1. **Foundation Projects (01-04)**: Basic CSnakes setup, data type handling, collections, and Python environment management
2. **Intermediate Projects (06-10)**: NumPy integration, exception handling, data processing, and ML with scikit-learn
3. **Advanced Projects (12-15)**: Text processing, generators, async operations, and progress reporting
4. **Production Applications**: 
   - **BlazorTrader**: Full-stack trading application with Python ML backend
   - **PythonTextAnalytics**: WinForms application for AI-powered code analysis

### BlazorTrader Architecture

The most complex project demonstrating production patterns:
- **Frontend**: Blazor Server application with real-time trading UI
- **Backend**: C# application orchestrating Python ML pipeline
- **Python Pipeline**: Multi-stage ML workflow (data download → indicators → training → prediction)
- **ML Stack**: XGBoost models with S&P 500 stock data and technical indicators

## Common Development Commands

### Building the Solution
```bash
dotnet build "CSnakes Course.sln"
```

### Running Individual Projects
```bash
# Navigate to specific project directory
cd "HelloWorld"
dotnet run

# Or run from solution root
dotnet run --project "HelloWorld/01. HelloWorld.csproj"
```

### Running Tests
```bash
# Run all tests in the solution
dotnet test "CSnakes Course.sln"

# Run tests for specific project
dotnet test HelloWorld.Tests/HelloWorld.Tests.csproj
```

### Python Environment Setup
Most projects use CSnakes' redistributable Python approach:
```csharp
builder.Services
    .WithPython()
    .WithHome(home)
    .FromRedistributable(); // Downloads Python 3.12 automatically
```

### Installing Python Dependencies
For projects with requirements.txt:
```bash
# Navigate to project with Python files
cd "BlazorTrader/PythonTrader/Src"
pip install -r requirements.txt
```

## Development Patterns

### CSnakes Project Structure
Each lesson follows this pattern:
- `Program.cs`: C# entry point with CSnakes setup
- `*.py`: Python modules containing business logic
- `*.csproj`: Project file with CSnakes.Runtime package reference
- Python files configured as `AdditionalFiles` with `CopyToOutputDirectory`

### Python Module Integration
Python files are embedded as additional files in .csproj:
```xml
<AdditionalFiles Include="hello.py">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</AdditionalFiles>
```

### Python Locator Strategies
Different approaches demonstrated across projects:
- `FromRedistributable()`: Downloads and caches Python locally
- `FromEnvironmentVariable()`: Uses system Python installation
- `FromFolder()`: Specific Python installation path
- `FromVirtualEnvironment()`: Uses Python venv
- `FromConda()`: Uses Conda environment

### Error Handling
CSnakes exceptions are handled through:
- `PythonRuntimeException` for runtime errors
- Environment validation before Python execution
- Fallback strategies for Python locator failures

## Testing

Testing is implemented through MSTest projects:
- `HelloWorld.Tests`: Unit tests for the basic HelloWorld project
- Test projects use assembly-level setup for Python environment initialization
- Tests are marked with `[DoNotParallelize]` to avoid Python runtime conflicts

To run tests for a specific project:
```bash
dotnet test HelloWorld.Tests/HelloWorld.Tests.csproj
```

## Python Dependencies

Common packages used across projects:
- **numpy**: Array operations and buffer sharing
- **pandas**: Data manipulation and analysis  
- **xgboost**: Machine learning models
- **yfinance**: Financial data downloading
- **ta/pandas_ta**: Technical analysis indicators
- **scikit-learn**: Machine learning algorithms
- **openai**: AI integration for result explanation

## Key CSnakes Concepts Demonstrated

1. **Data Exchange**: Primitives, collections, NumPy arrays, custom objects
2. **Memory Management**: Zero-copy buffer sharing between C# and Python
3. **Error Handling**: Exception propagation across language boundaries
4. **Async Operations**: Progress reporting from Python to C#
5. **Package Management**: Virtual environments and requirements.txt handling
6. **Production Deployment**: Environment configuration strategies

## Project-Specific Notes

### BlazorTrader
- Requires large Python dependencies (XGBoost, pandas, etc.)
- Downloads ~500MB of S&P 500 historical data on first run
- Uses environment variables from .env file (EnvLoader.cs)
- Implements virtual environment with UV installer

### NumPy Projects (06-07)
- Demonstrate zero-copy buffer sharing
- Show performance optimization techniques
- Include timing measurements

### Managing Python (04)
- Shows different Python locator strategies
- Demonstrates package installation patterns
- Includes both simple and pandas examples

## Build System Integration

CSnakes generates bindings at build time, requiring Python to be installed on the build machine. For consistent builds:
- Install the same Python version (e.g., 3.10.x) on all developer machines and build servers
- Keep Python dependencies version-controlled in requirements.txt files
- Consider using pyenv or pyenv-win for consistent Python versions

## Recommended VS Code Settings

For optimal development experience, recommend these VS Code workspace settings:

### Window Title Simplification
```json
{
  "window.title": "${rootName}"
}
```
This removes filenames from the window title, showing only the project name for cleaner workspace switching.

### Complete Recommended Settings
```json
{
  "window.title": "${rootName}",
  "explorer.compactFolders": false,
  "files.trimTrailingWhitespace": true,
  "editor.formatOnSave": true
}
```

Add these to `.vscode/settings.json` in any CSnakes project workspace.