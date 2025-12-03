# Linux Setup Guide for CSnakes Course

This guide will help you set up your Linux development environment for the Master CSnakes course.

## Important Note: Visual Studio on Linux

**Visual Studio is NOT available for Linux.** Microsoft discontinued Visual Studio for Linux. Your options are:
- **VS Code** (Recommended) - Lightweight, full-featured code editor
- **JetBrains Rider** - Commercial IDE with excellent .NET support
- **Command Line + Text Editor** - Using dotnet CLI with vim/emacs/nano

This guide focuses on VS Code as it's free and provides excellent .NET development support.

## Prerequisites

### 1. Install .NET 9.0 SDK

The installation method varies by distribution:

#### Ubuntu/Debian
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

#### Fedora
```bash
sudo dnf install dotnet-sdk-9.0
```

#### Arch Linux
```bash
sudo pacman -S dotnet-sdk
```

#### Universal (Snap)
```bash
sudo snap install dotnet-sdk --classic --channel=9.0
```

Verify installation:
```bash
dotnet --version
# Should show 9.0.x or later
```

### 2. Install Python 3.12 (Required)

**IMPORTANT: This course requires Python 3.12 specifically.**

While CSnakes supports Python 3.9-3.13, this course standardizes on **Python 3.12** because:
- The course projects use `FromRedistributable()` which downloads Python 3.12
- Build-time binding generation needs a matching Python version
- Mixing Python versions causes initialization errors

**For teams/organizations:** Install Python 3.12 on all developer machines and build servers.

#### Ubuntu/Debian
```bash
# Add deadsnakes PPA for Python 3.12 (if not available in default repos)
sudo add-apt-repository ppa:deadsnakes/ppa
sudo apt update
sudo apt install python3.12 python3.12-venv python3.12-dev

# Make python3 point to 3.12
sudo update-alternatives --install /usr/bin/python3 python3 /usr/bin/python3.12 1
```

#### Fedora
```bash
sudo dnf install python3.12 python3.12-pip
```

#### Arch Linux
```bash
# Arch typically has latest Python
sudo pacman -S python python-pip
```

**WARNING: Do NOT set PYTHONPATH or PYTHONHOME**

Do not manually set the `PYTHONPATH` or `PYTHONHOME` environment variables. CSnakes manages Python paths automatically. Setting these variables to a different Python version will cause fatal initialization errors.

Verify installation:
```bash
python3 --version
# Must show Python 3.12.x specifically
```

If you see a different version, ensure Python 3.12 is first in your PATH or update alternatives.

### 3. Install VS Code

#### Ubuntu/Debian (using official repository)
```bash
# Add Microsoft GPG key
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
sudo install -D -o root -g root -m 644 packages.microsoft.gpg /etc/apt/keyrings/packages.microsoft.gpg

# Add VS Code repository
sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/keyrings/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'

# Install VS Code
sudo apt update
sudo apt install code
```

#### Fedora/RHEL/CentOS
```bash
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
sudo sh -c 'echo -e "[code]\nname=Visual Studio Code\nbaseurl=https://packages.microsoft.com/yumrepos/vscode\nenabled=1\ngpgcheck=1\ngpgkey=https://packages.microsoft.com/keys/microsoft.asc" > /etc/yum.repos.d/vscode.repo'
sudo dnf install code
```

#### Arch Linux
```bash
yay -S visual-studio-code-bin
# Or using snap
sudo snap install code --classic
```

#### Universal (Snap)
```bash
sudo snap install code --classic
```

### 4. Install Essential VS Code Extensions

```bash
# C# Development (required)
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit

# Python Development
code --install-extension ms-python.python
code --install-extension ms-python.vscode-pylance

# Helpful extras
code --install-extension eamodio.gitlens
```

### 5. Install Git

```bash
# Ubuntu/Debian
sudo apt install git

# Fedora
sudo dnf install git

# Arch
sudo pacman -S git
```

### 6. Install UV (Optional but Recommended)

UV is a fast Python package installer:

```bash
# Using the official installer
curl -LsSf https://astral.sh/uv/install.sh | sh

# Add to PATH (add to ~/.bashrc or ~/.zshrc for persistence)
export PATH="$HOME/.cargo/bin:$PATH"
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

## Development Workflow with VS Code

### 1. Open the Project
```bash
cd /path/to/CSnakesCourse
code .
```

### 2. VS Code Setup
- When prompted, install recommended extensions
- Select "Yes" when asked to add required assets for build and debug
- The C# extension will automatically download OmniSharp

### 3. Running Projects

#### Option A: Using Integrated Terminal
1. Open terminal in VS Code: `Ctrl+`` 
2. Navigate to project: `cd HelloWorld`
3. Run: `dotnet run`

#### Option B: Using Run Configuration
1. Press `F5` or click "Run and Debug" in sidebar
2. Select ".NET Core" if prompted
3. Choose the project to run

#### Option C: Using Tasks
1. Press `Ctrl+Shift+P` to open command palette
2. Type "Tasks: Run Task"
3. Select "build" or create custom tasks in `.vscode/tasks.json`

## Troubleshooting

### Python Command Issues
```bash
# Create python symlink pointing to Python 3.12
sudo ln -s /usr/bin/python3.12 /usr/bin/python

# Or use alternatives
sudo update-alternatives --install /usr/bin/python python /usr/bin/python3.12 1

# Verify version
python --version
# Must show 3.12.x
```

### PYTHONPATH/PYTHONHOME Conflict
If you see errors like `ModuleNotFoundError: No module named 'encodings'`:

```bash
# Check if these variables are set
echo $PYTHONPATH
echo $PYTHONHOME

# If set, unset them
unset PYTHONPATH
unset PYTHONHOME

# Remove from shell config files (~/.bashrc, ~/.zshrc, /etc/environment) if present
# Then restart your terminal or source the config file
```

### Wrong Python Version
If `python3 --version` shows something other than 3.12:
```bash
# Check installed versions
ls /usr/bin/python*

# Update alternatives to use 3.12
sudo update-alternatives --install /usr/bin/python3 python3 /usr/bin/python3.12 2
sudo update-alternatives --config python3
# Select python3.12 from the menu
```

### Permission Issues
```bash
# Fix Python package permissions
chmod -R u+rw ~/.local/lib/python*/site-packages/

# For system-wide packages (not recommended)
sudo pip3 install --break-system-packages package_name
```

### Missing Dependencies
```bash
# Ubuntu/Debian - Install build essentials
sudo apt install build-essential

# Python development headers
sudo apt install python3-dev
```

### Virtual Environment Setup
```bash
# Create virtual environment
python3 -m venv .venv

# Activate it
source .venv/bin/activate  # bash/zsh
# or
. .venv/bin/activate.fish  # fish shell
```

### SELinux Issues (Fedora/RHEL)
```bash
# If you encounter SELinux denials
sudo setenforce 0  # Temporary
# Or configure proper SELinux policies
```

## Platform-Specific Notes

### Ubuntu/Debian
- Use `apt` for system packages
- Python 3 is default, `python` command may not exist
- May need to install `python-is-python3` package

### Fedora/RHEL
- SELinux may block some operations
- Use `dnf` for packages
- Newer Python versions available in repos

### Arch Linux
- Rolling release means latest packages
- AUR has additional development tools
- Python 3 is default `python`

## Alternative IDEs

### JetBrains Rider (Commercial)
```bash
# Using snap
sudo snap install rider --classic

# Or download from:
# https://www.jetbrains.com/rider/
```

### Command Line Development
```bash
# Build
dotnet build

# Run
dotnet run --project HelloWorld/01.\ HelloWorld.csproj

# Watch for changes
dotnet watch run
```

## VS Code Settings (Recommended)

Create `.vscode/settings.json` in the project root:
```json
{
    "dotnet.defaultSolution": "CSnakes Course.sln",
    "python.defaultInterpreterPath": "/usr/bin/python3",
    "python.terminal.activateEnvironment": true,
    "editor.formatOnSave": true,
    "files.exclude": {
        "**/bin": true,
        "**/obj": true
    }
}
```

## Next Steps

1. Verify setup by running HelloWorld project
2. Configure VS Code with recommended extensions
3. Familiarize yourself with dotnet CLI commands
4. Progress through numbered projects sequentially

## Getting Help

If you encounter Linux-specific issues:
1. Check your distribution's documentation
2. Verify all prerequisites with correct versions
3. Check file permissions and SELinux settings
4. Review main [README.md](../README.md) for general guidance
5. Reach out to the course instructor via LinkedIn

---

Happy coding with CSnakes on Linux! 🐍🐧