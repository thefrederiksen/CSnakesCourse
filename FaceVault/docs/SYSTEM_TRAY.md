# FaceVault System Tray Documentation

## Overview

FaceVault now runs as a system tray application that:
1. **Automatically adds itself to Windows startup** (no admin required)
2. **Minimizes to system tray** with a custom icon
3. **Runs background tasks** for photo scanning
4. **Provides quick access** via right-click menu

## Features

### Auto-Startup
- Adds itself to user's Startup folder on first run
- Also adds registry entry as backup
- No administrator privileges required
- Works with `--minimized` flag to start hidden

### System Tray Icon
- Shows FaceVault icon in system tray
- Console window is hidden by default
- Double-click opens web interface
- Right-click shows context menu:
  - **Open FaceVault** - Opens browser to http://localhost:5000
  - **Show Console** - Shows/hides console window for viewing logs
  - **Exit** - Close application

### Background Services
- Runs periodic photo scans (every hour)
- Can perform AI analysis in background
- Continues running when main window closed

## Usage

### First Run
```bash
# Run normally - opens browser
FaceVault.exe

# Run minimized to tray
FaceVault.exe --minimized
```

### Daily Use
1. Application starts with Windows
2. Runs minimized in system tray
3. Click icon to access interface
4. Background scanning continues

### Removing from Startup
To stop auto-startup:
1. Open Run dialog (Win+R)
2. Type `shell:startup`
3. Delete FaceVault shortcut

Or via Registry:
1. Open Registry Editor
2. Navigate to: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
3. Delete FaceVault entry

## Technical Details

### Implementation
- Uses Windows Forms for system tray
- No Windows service (no admin required)
- User-level registry/startup folder
- Background tasks via hosted services

### Files
- `SystemTrayService.cs` - Tray icon management
- `BackgroundTaskService.cs` - Background scanning
- Modified `Program.cs` - Startup logic

### Requirements
- Windows OS (for system tray)
- .NET 9.0 Windows runtime
- No administrator privileges