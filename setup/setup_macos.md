# macOS Setup Guide for CSnakes Course

This guide will help you set up your macOS development environment for the Master CSnakes course.

## Prerequisites

### 1. Install .NET 9.0 SDK

Download and install the .NET 9.0 SDK for macOS from the official Microsoft website:

```bash
# Using Homebrew (recommended)
brew install --cask dotnet-sdk

# Or download directly from:
# https://dotnet.microsoft.com/download/dotnet/9.0
```

Verify installation:
```bash
dotnet --version
# Should show 9.0.x or later
```

### 2. Install Python 3.9+

macOS typically comes with Python, but you'll want to ensure you have Python 3.9 or later:

```bash
# Using Homebrew (recommended)
brew install python@3.12

# Or download from python.org:
# https://www.python.org/downloads/
```

Verify installation:
```bash
python3 --version
# Should show Python 3.9.x or later
```

### 3. Install Visual Studio for Mac or VS Code

#### Option A: Visual Studio for Mac (Recommended for Full Experience)
- Download from: https://visualstudio.microsoft.com/vs/mac/
- Select the ".NET" workload during installation
- Note: Visual Studio for Mac is being retired in 2024, but still works for this course

#### Option B: Visual Studio Code (Lightweight Alternative)
```bash
# Using Homebrew
brew install --cask visual-studio-code

# Install required extensions:
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-python.python
```

### 4. Install Git

```bash
# Check if Git is already installed
git --version

# If not installed, macOS will prompt to install Xcode Command Line Tools
# Or install via Homebrew:
brew install git
```

### 5. Install UV (Optional but Recommended)

UV is a fast Python package installer used by some projects in this course:

```bash
# Using curl
curl -LsSf https://astral.sh/uv/install.sh | sh

# Or using Homebrew
brew install uv
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-repo/CSnakesCourse.git
cd CSnakesCourse
```

### 2. Build the Solution

```bash
# Build all projects
dotnet build "CSnakes Course.sln"
```

### 3. Run Your First Project

```bash
# Navigate to HelloWorld project
cd HelloWorld
dotnet run

# Expected output:
# Hello from Python: World
```

## Running Projects in Visual Studio for Mac

1. Open Visual Studio for Mac
2. Click "Open..." and select `CSnakes Course.sln`
3. In the Solution Explorer, right-click on "01. HelloWorld"
4. Select "Set as Startup Project"
5. Press ⌘+↩ (Cmd+Enter) or click the Run button

## Running Projects in VS Code

1. Open the course folder in VS Code:
   ```bash
   code .
   ```

2. Open the integrated terminal (⌃` or View → Terminal)

3. Navigate to a project and run:
   ```bash
   cd HelloWorld
   dotnet run
   ```

## Troubleshooting

### Python Not Found
If CSnakes can't find Python:
```bash
# Create a symbolic link for python
sudo ln -s /usr/local/bin/python3 /usr/local/bin/python

# Or set the PYTHON_HOME environment variable
export PYTHON_HOME=/usr/local/bin/python3
```

### Permission Issues
If you encounter permission issues:
```bash
# Fix permissions for Python packages
chmod -R u+rw ~/.local/lib/python*/site-packages/
```

### Virtual Environment Issues
Some projects use virtual environments. If you have issues:
```bash
# Install virtualenv
pip3 install virtualenv

# Or use Python's built-in venv
python3 -m venv .venv
source .venv/bin/activate
```

### UV Installation Issues
If UV commands fail:
```bash
# Add UV to PATH
echo 'export PATH="$HOME/.cargo/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

## Platform-Specific Notes

- **Apple Silicon (M1/M2/M3)**: All tools support ARM64 architecture natively
- **Python Package Compatibility**: Most packages work on Apple Silicon, but some may require Rosetta 2
- **File Paths**: Use forward slashes (/) in all paths
- **Case Sensitivity**: macOS is case-insensitive by default, but Python import statements are case-sensitive

## VS Code Extensions (Recommended)

```bash
# C# Development
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit

# Python Development
code --install-extension ms-python.python
code --install-extension ms-python.vscode-pylance

# General Development
code --install-extension eamodio.gitlens
code --install-extension streetsidesoftware.code-spell-checker
```

## Next Steps

1. Verify your setup by running the HelloWorld project
2. Progress through the numbered projects sequentially
3. Each project builds on concepts from previous ones
4. Check individual project README files for specific requirements

## Getting Help

If you encounter issues:
1. Check the main [README.md](../README.md) for general guidance
2. Review the [CLAUDE.md](../CLAUDE.md) file for architecture details
3. Ensure all prerequisites are installed with correct versions
4. Reach out to the course instructor via LinkedIn

---

Happy coding with CSnakes on macOS! 🐍🍎