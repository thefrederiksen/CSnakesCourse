# Logging Configuration

## Overview

FaceVault uses a dual logging system:
1. **ASP.NET Core Logging** - Standard .NET logging for framework and application logs
2. **Custom File Logger** - Application-specific file logging with structured output

## Default Logging Behavior

By default, FaceVault runs with **balanced logging** to improve performance while showing important milestones:
- **Information** level shows important operations (directory creation, database initialization, major steps)
- **Debug** level (hidden by default) contains repetitive operations (individual file processing, hash calculations)
- Framework components (ASP.NET, EF Core) only show warnings and errors
- File-by-file operations during scanning are logged at Debug level (not shown)

This ensures you see important progress updates while avoiding console spam during scanning.

## Enabling Verbose Logging

There are two ways to enable verbose logging when debugging issues:

### Method 1: Environment Variable
Set the environment variable before starting the application:
```bash
# Windows Command Prompt
set FACEVAULT_VERBOSE_LOGGING=true
dotnet run

# Windows PowerShell
$env:FACEVAULT_VERBOSE_LOGGING = "true"
dotnet run

# Linux/macOS
export FACEVAULT_VERBOSE_LOGGING=true
dotnet run
```

### Method 2: Configuration File
Add to `appsettings.json` or `appsettings.Development.json`:
```json
{
  "VerboseLogging": true
}
```

## What Verbose Logging Shows

When enabled, verbose logging includes:
- All file operations during scanning
- Database connection details
- Python environment initialization
- Detailed progress during operations
- Framework-level debug information
- SQL queries (in development mode)

## Log Files

All logs are written to files regardless of console verbosity:
- Location: `%APPDATA%\FaceVault\Logs\` (Windows)
- Format: `FaceVault_YYYYMMDD_HHMMSS.log`
- Content: Structured logs with timestamps, levels, and source locations

## Log Levels

The application uses these log levels:
- **Debug**: Detailed information for debugging (only shown in verbose mode)
- **Info**: General informational messages
- **Warning**: Warning conditions that don't prevent operation
- **Error**: Error conditions and exceptions
- **Critical**: Fatal errors requiring immediate attention

## Performance Considerations

Verbose logging can significantly impact performance during scanning operations:
- Each file operation generates a log entry
- Console output slows down the scanning process
- Recommended to use verbose logging only when debugging

## Example Log Output

### Normal Mode (Information and above):
```
[INFO] FaceVault Blazor application starting
[INFO] Created directory: C:\Users\Name\AppData\Roaming\FaceVault\Database
[INFO] All application directories verified/created successfully
[INFO] Python environment created successfully
[INFO] Database initialized successfully
[INFO] FaceVault Blazor application running
[WARNING] Duplicate found: 15 files with hash ABC123...
[ERROR] Failed to process image: Access denied to C:\Protected\image.jpg
```

### Verbose Mode (includes Debug):
```
[INFO] FaceVault Blazor application starting (VERBOSE LOGGING ENABLED)
[DEBUG] PathService initialized with user data directory: C:\Users\Name\AppData\Roaming\FaceVault
[INFO] Created directory: C:\Users\Name\AppData\Roaming\FaceVault\Database
[INFO] Database initialized successfully
[DEBUG] Processing: IMG_001.jpg
[DEBUG] Calculating hash for: IMG_001.jpg
[DEBUG] New image added to database: IMG_001.jpg
[DEBUG] Duplicate found: IMG_002.jpg matches existing file
```

## Troubleshooting

If you're not seeing expected logs:
1. Check the environment variable is set correctly
2. Verify the log file location exists and is writable
3. Ensure the application has proper permissions
4. Check for log files in `%APPDATA%\FaceVault\Logs\`