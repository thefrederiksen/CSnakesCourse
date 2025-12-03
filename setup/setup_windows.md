# CSnakes Course Setup Guide

Welcome to the CSnakes Course! This guide will walk you through setting up your development environment to run the complete course that demonstrates C# and Python interoperability using the CSnakes runtime.

## Prerequisites

Before starting the course, you'll need to install the following software:

### 1. .NET 9.0 SDK

This course requires .NET 9.0 or later.

**Download and Install:**
- Visit the [.NET 9.0 download page](https://dotnet.microsoft.com/download/dotnet/9.0)
- Download the SDK (not just the runtime)
- Follow the installation instructions for your operating system

**Verify Installation:**
```bash
dotnet --version
```
You should see version 9.0.0 or later.

### 2. Python 3.12 (Required)

**IMPORTANT: This course requires Python 3.12 specifically.**

While CSnakes supports Python 3.9-3.13, this course standardizes on **Python 3.12** because:
- The course projects use `FromRedistributable()` which downloads Python 3.12
- Build-time binding generation needs a matching Python version
- Mixing Python versions causes initialization errors that are difficult to debug

**For teams/organizations:** Install Python 3.12 on all developer machines and build servers to ensure consistency.

**Download and Install:**
- Visit [Python 3.12 download page](https://www.python.org/downloads/release/python-3129/)
- Download **Python 3.12.9** (or latest 3.12.x)
- During installation: **CHECK "Add Python to PATH"**

**WARNING: Do NOT set PYTHONPATH or PYTHONHOME**

Do not manually set the `PYTHONPATH` or `PYTHONHOME` environment variables. CSnakes manages Python paths automatically. Setting these variables (especially to a different Python version) will cause fatal initialization errors like:
```
ModuleNotFoundError: No module named 'encodings'
```

If you have these variables set from a previous Python installation, remove them before running the course projects. See the Troubleshooting section for instructions.

**Verify Installation:**
```bash
python --version
```
You should see **Python 3.12.x** specifically. If you see a different version (e.g., 3.11, 3.10), you need to either:
- Uninstall the old version and install Python 3.12
- Update your PATH to point to Python 3.12

### 3. Development Environment

**Visual Studio 2022 (Required):**
- Download [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (Community edition is free)
- During installation, select the ".NET desktop development" workload
- Visual Studio provides excellent support for working with .NET solutions and makes this course much easier to follow

**Note:** While VS Code can technically be used for this course, we strongly recommend Visual Studio 2022. This course walkthrough assumes you're using Visual Studio, as it provides better solution management and debugging experience for the CSnakes projects.

## Course Setup

### 1. Get the Course Files

You need to download or clone the course repository to your local machine. Choose one of the following methods:

**Option A: Download ZIP (Easiest for beginners)**
1. Go to the course repository on GitHub
2. Click the green "Code" button
3. Select "Download ZIP"
4. Extract the ZIP file to a folder on your computer (e.g., `C:\CSnakesCourse`)
5. Remember the location where you extracted the files

**Option B: Clone with Git (Recommended if you have Git installed)**
1. Open Command Prompt or PowerShell
2. Navigate to where you want to store the course (e.g., `cd C:\`)
3. Run the clone command:
   ```bash
   git clone [REPOSITORY_URL]
   ```
4. This will create a folder with all the course files

**Option C: Clone with Visual Studio**
1. Open Visual Studio 2022
2. Click "Clone a repository" on the start screen
3. Enter the repository URL
4. Choose a local path where you want to store the course
5. Click "Clone"

**Verify You Have the Files:**
After downloading/cloning, you should see a folder containing:
- `CSnakes Course.sln` (the main solution file)
- Multiple project folders (HelloWorld, BlazorTrader, etc.)
- This setup guide

### 2. Open the Solution

1. Open Visual Studio 2022
2. Click "Open a project or solution"
3. Navigate to the course directory
4. Select `CSnakes Course.sln`

### 3. Build the Solution

- Right-click on the solution in Solution Explorer
- Select "Build Solution" or press `Ctrl+Shift+B`

You should see a successful build with no errors.

## Python Dependencies

Most course projects use CSnakes' automatic Python distribution feature, which means Python packages are installed automatically when needed. However, some advanced projects (like BlazorTrader) may require additional setup.

For projects with a `requirements.txt` file:
1. Navigate to the project directory containing the Python files
2. Install dependencies:
```bash
pip install -r requirements.txt
```

## Running Your First Project

Let's verify everything is working by running the HelloWorld project:

1. In Solution Explorer, right-click on the "01. HelloWorld" project
2. Select "Set as Startup Project"
3. Press F5 or click the "Start" button

You should see output similar to:
```
Hello from Python: World
```

If you see this output, congratulations! Your environment is properly configured.

## Course Structure

The course contains 13 projects that progressively introduce CSnakes concepts:

1. **HelloWorld** - Basic setup and function calling
2. **Primitives_And_Return_Types** - Data type handling
3. **CollectionsAndTuples** - Complex data structures
4. **Managing Python** - Runtime and environment management
5. **NumPy1/Numpy2** - NumPy integration
6. **CSnakesExceptions** - Error handling
7. **BlazorTrader** - Full-stack trading application
8. **TalkToMyCode** - WinForms code analysis tool
9. **TestPython/ProgressFromPython** - Testing and progress patterns

## Running Individual Projects

Each project can be run independently:

1. Right-click on the desired project in Solution Explorer
2. Select "Set as Startup Project"
3. Press F5 to run

## Troubleshooting

### Common Issues:

**"Python not found" errors:**
- Ensure Python is installed and added to your PATH
- Try running `python --version` or `python3 --version` in your terminal
- On some systems, you may need to use `python3` instead of `python`

**Build errors:**
- Ensure you have .NET 9.0 SDK installed (not just the runtime)
- Try cleaning and rebuilding: `dotnet clean` followed by `dotnet build`

**Missing Python packages:**
- Most projects handle this automatically via CSnakes
- For projects with requirements.txt, manually install: `pip install -r requirements.txt`

### PYTHONPATH/PYTHONHOME Conflict

If you see errors like:
```
Fatal Python error: init_fs_encoding: failed to get the Python codec of the filesystem encoding
ModuleNotFoundError: No module named 'encodings'
```

This means you have a `PYTHONPATH` or `PYTHONHOME` environment variable pointing to a different Python version than 3.12.

**To fix this:**

1. Open PowerShell and check if these variables are set:
   ```powershell
   [System.Environment]::GetEnvironmentVariable('PYTHONPATH', 'User')
   [System.Environment]::GetEnvironmentVariable('PYTHONPATH', 'Machine')
   [System.Environment]::GetEnvironmentVariable('PYTHONHOME', 'User')
   [System.Environment]::GetEnvironmentVariable('PYTHONHOME', 'Machine')
   ```

2. Remove any that return a value:
   ```powershell
   # Remove user-level (most common)
   [System.Environment]::SetEnvironmentVariable('PYTHONPATH', '', 'User')
   [System.Environment]::SetEnvironmentVariable('PYTHONHOME', '', 'User')

   # Remove machine-level (requires Admin PowerShell)
   [System.Environment]::SetEnvironmentVariable('PYTHONPATH', '', 'Machine')
   [System.Environment]::SetEnvironmentVariable('PYTHONHOME', '', 'Machine')
   ```

3. **Restart Visual Studio** - environment variable changes require a restart.

### Wrong Python Version

If `python --version` shows a version other than 3.12:
- Uninstall other Python versions, or
- Ensure Python 3.12 appears first in your PATH environment variable

### Platform-Specific Notes:

**Windows:**
- Make sure Python 3.12 was added to PATH during installation
- You may need to restart your terminal/IDE after installing Python
- Do NOT set PYTHONPATH or PYTHONHOME environment variables

## Next Steps

Once your environment is set up:
1. Start with the HelloWorld project to verify everything works
2. Progress through the projects in numerical order
3. Each project builds on concepts from previous ones
4. The BlazorTrader project demonstrates production-ready patterns

## Support

If you encounter issues:
1. Check that all prerequisites are properly installed
2. Verify the versions meet the minimum requirements
3. Try rebuilding the solution
4. Check the project's individual README or documentation

Happy coding with CSnakes!