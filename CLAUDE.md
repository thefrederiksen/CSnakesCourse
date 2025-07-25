# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This repository contains a comprehensive CSnakes course that demonstrates C# and Python interoperability using the CSnakes runtime. The project is organized as multiple lessons that progressively introduce concepts from basic hello world applications to advanced trading systems with machine learning integration.

## Architecture

The repository is structured as a Visual Studio solution with multiple projects representing different lessons/labs:

- **Course Structure**: 13 distinct projects covering CSnakes fundamentals
- **Core Technology**: CSnakes.Runtime for C#/Python interoperability  
- **Target Framework**: .NET 9.0
- **Python Integration**: Uses CSnakes to execute Python code from C# applications

### Key Projects

1. **HelloWorld**: Basic CSnakes setup and Python function calling
2. **Primitives_And_Return_Types**: Data type handling between C# and Python
3. **CollectionsAndTuples**: Working with complex data structures
4. **Managing Python**: Python runtime and virtual environment management
5. **NumPy1/Numpy2**: NumPy array integration and buffer operations
6. **CSnakesExceptions**: Error handling across language boundaries
7. **BlazorTrader**: Full-stack trading application with Python ML backend
8. **TalkToMyCode**: WinForms application for code analysis
9. **TestPython/ProgressFromPython**: Testing and progress reporting patterns

### BlazorTrader Architecture

The most complex project demonstrating production patterns:
- **Frontend**: Blazor Server application with real-time trading UI
- **Backend**: C# application orchestrating Python ML pipeline
- **Python Pipeline**: Multi-stage ML workflow (data download → indicators → training → prediction)
- **Data**: S&P 500 stock data with technical indicators and XGBoost models

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
Python files are embedded as additional files:
```xml
<AdditionalFiles Include="hello.py">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</AdditionalFiles>
```

### Error Handling
CSnakes exceptions are handled through:
- `PythonRuntimeException` for runtime errors
- Environment validation before Python execution
- Fallback strategies for Python locator failures

## Testing

No centralized test framework is configured. Each project serves as a self-contained demonstration that can be run independently to verify functionality.

To test a project:
1. Navigate to the project directory
2. Run `dotnet run`
3. Observe console output for expected results

## Python Dependencies

Common packages used across projects:
- **numpy**: Array operations and buffer sharing
- **pandas**: Data manipulation and analysis  
- **xgboost**: Machine learning models
- **yfinance**: Financial data downloading
- **ta**: Technical analysis indicators

## Key CSnakes Concepts Demonstrated

1. **Python Locator Strategies**: FromRedistributable, FromEnvironmentVariable, FromFolder, FromVirtualEnvironment, FromConda
2. **Data Exchange**: Primitives, collections, NumPy arrays, custom objects
3. **Memory Management**: Buffer sharing between C# and Python
4. **Error Handling**: Exception propagation and handling
5. **Async Operations**: Progress reporting from Python to C#
6. **Production Deployment**: Environment configuration and dependency management