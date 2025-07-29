# System Tray Troubleshooting Guide

## Issue: System Tray Menu Not Appearing

### Symptoms
- System tray icon is visible
- Clicking (left or right) on the icon does not show the context menu
- Console window may or may not be hidden

### Possible Causes

1. **Windows Forms Message Pump**: The system tray requires a Windows Forms message loop to process events
2. **Thread Apartment State**: Windows Forms controls must run on an STA (Single Threaded Apartment) thread
3. **Icon Creation**: The icon might not be properly initialized

### Current Implementation

The SystemTrayService now:
1. Creates the system tray icon on a separate STA thread
2. Runs its own Windows Forms message loop with `Application.Run()`
3. Properly handles the context menu assignment

### Debugging Steps

1. **Check Windows Notifications Area Settings**
   - Right-click on taskbar → Taskbar settings
   - Click "Select which icons appear on the taskbar"
   - Ensure FaceVault is set to "On"

2. **Check if Icon is in Hidden Area**
   - Click the up arrow (^) in the system tray
   - Look for FaceVault icon there
   - If found, drag it to the main tray area

3. **Verify Icon Creation**
   - Check logs for "System tray icon created successfully"
   - Look for the balloon notification on startup

4. **Test Basic Windows Forms**
   - Run the TestSystemTray.cs file independently to verify Windows Forms works

### Alternative Approach

If the current implementation still doesn't work, consider using the Windows Forms ApplicationContext approach:

```csharp
public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    
    public TrayApplicationContext()
    {
        // Initialize tray icon here
        trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true
        };
    }
    
    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (s, e) => OpenBrowser());
        menu.Items.Add("Exit", null, (s, e) => ExitApplication());
        return menu;
    }
}
```

### Manual Testing

1. Close all instances of FaceVault
2. Run `FaceVault.exe` from command line to see console output
3. Look for the system tray icon
4. Try right-clicking on the icon
5. Check console for any error messages

### Known Issues

- Some antivirus software may interfere with system tray functionality
- Windows 11 has different system tray behavior than Windows 10
- High DPI displays may affect icon rendering

### Next Steps

If the menu still doesn't appear:
1. Try running as administrator (temporarily for testing)
2. Check Event Viewer for any Windows Forms errors
3. Ensure .NET Desktop Runtime is properly installed
4. Test with a simpler icon (e.g., SystemIcons.Application)