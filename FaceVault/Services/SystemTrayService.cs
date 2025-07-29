using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FaceVault.Services
{
    public class SystemTrayService : IDisposable
    {
        // Windows API imports for console management
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWNA = 8;

        private NotifyIcon? _trayIcon;
        private readonly ILogger<SystemTrayService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private bool _disposed = false;
        private static bool _consoleVisible = false;
        private static IntPtr _consoleWindow = IntPtr.Zero;
        private Thread? _trayThread;

        public SystemTrayService(
            ILogger<SystemTrayService> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
        }

        public void Initialize()
        {
            try
            {
                // Get console window handle
                _consoleWindow = GetConsoleWindow();
                
                // Console is already hidden from Program.cs
                _consoleVisible = false;
                
                _logger.LogInformation($"System tray service initializing. Console handle: {_consoleWindow}");

                // Add to startup automatically (user-level, no admin required)
                AddToStartup();

                // Create system tray icon on a separate STA thread for Windows Forms
                _trayThread = new Thread(() =>
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    
                    CreateSystemTrayIcon();
                    
                    // Run Windows Forms message loop
                    Application.Run();
                });
                
                _trayThread.SetApartmentState(ApartmentState.STA);
                _trayThread.IsBackground = true;
                _trayThread.Start();

                _logger.LogInformation("System tray service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize system tray service");
            }
        }

        private void AddToStartup()
        {
            try
            {
                // Method 1: Add to user's Startup folder (most reliable)
                var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    var shortcutPath = Path.Combine(startupPath, "FaceVault.lnk");
                    
                    // Create shortcut using Windows Script Host
                    CreateShortcut(shortcutPath, exePath);
                    
                    _logger.LogInformation("Added FaceVault to startup folder: {Path}", shortcutPath);
                }

                // Method 2: Also add to registry as backup
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null && !string.IsNullOrEmpty(exePath))
                {
                    // Add with --minimized flag
                    key.SetValue("FaceVault", $"\"{exePath}\" --minimized");
                    _logger.LogInformation("Added FaceVault to registry startup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add application to startup");
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath)
        {
            try
            {
                // Use Windows Script Host to create shortcut
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    var shell = Activator.CreateInstance(shellType);
                    if (shell != null)
                    {
                        dynamic? shortcut = shell.GetType().InvokeMember("CreateShortcut", 
                            System.Reflection.BindingFlags.InvokeMethod, 
                            null, shell, new object[] { shortcutPath });
                        
                        if (shortcut != null)
                        {
                            shortcut.TargetPath = targetPath;
                            shortcut.Arguments = "--minimized";
                            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                            shortcut.IconLocation = targetPath;
                            shortcut.Description = "FaceVault - AI-powered photo organization";
                            shortcut.Save();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create shortcut");
            }
        }

        private void CreateSystemTrayIcon()
        {
            try
            {
                // Try to use system default icon first for testing
                var icon = SystemIcons.Application;
                
                _trayIcon = new NotifyIcon
                {
                    Icon = icon,
                    Visible = true,
                    Text = "FaceVault - AI-powered photo organization",
                    BalloonTipTitle = "FaceVault",
                    BalloonTipText = "FaceVault is running in the background",
                    BalloonTipIcon = ToolTipIcon.Info
                };
                
                _logger.LogInformation("Creating system tray icon with context menu");

                // Create context menu with simplified options
                var contextMenu = new ContextMenuStrip();
                
                // Open FaceVault - Opens browser
                var openMenuItem = new ToolStripMenuItem("Open FaceVault");
                openMenuItem.Click += (s, e) => OpenApplication();
                openMenuItem.Font = new Font(openMenuItem.Font, FontStyle.Bold); // Make default action bold
                contextMenu.Items.Add(openMenuItem);
                
                // Show/Hide Console - Toggles console visibility
                var consoleMenuItem = new ToolStripMenuItem("Show Console");
                consoleMenuItem.Click += (s, e) => ToggleConsole();
                contextMenu.Items.Add(consoleMenuItem);
                
                contextMenu.Items.Add(new ToolStripSeparator());
                
                // Exit application
                var exitMenuItem = new ToolStripMenuItem("Exit");
                exitMenuItem.Click += (s, e) => ExitApplication();
                contextMenu.Items.Add(exitMenuItem);

                _trayIcon.ContextMenuStrip = contextMenu;
                
                // Handle left-click to show menu (Windows 11 style)
                _trayIcon.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        // Show context menu on left click
                        var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        mi?.Invoke(_trayIcon, null);
                    }
                };
                
                // Double-click to open application
                _trayIcon.DoubleClick += (s, e) => OpenApplication();

                // Show balloon tip on first run
                _trayIcon.ShowBalloonTip(3000);
                
                _logger.LogInformation($"System tray icon created successfully. Icon visible: {_trayIcon.Visible}, Has context menu: {_trayIcon.ContextMenuStrip != null}, Menu items: {_trayIcon.ContextMenuStrip?.Items.Count ?? 0}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create system tray icon");
            }
        }

        private Icon CreateDefaultIcon()
        {
            // Create a simple icon programmatically
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                
                // Draw a simple camera/photo icon
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 122, 204)), 2, 4, 12, 9);
                g.FillEllipse(new SolidBrush(Color.White), 5, 6, 6, 5);
                g.FillPolygon(new SolidBrush(Color.FromArgb(0, 122, 204)), new[]
                {
                    new Point(4, 4),
                    new Point(7, 2),
                    new Point(9, 2),
                    new Point(12, 4)
                });
            }
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void ToggleConsole()
        {
            try
            {
                if (_consoleWindow == IntPtr.Zero)
                {
                    _consoleWindow = GetConsoleWindow();
                }
                
                if (_consoleWindow != IntPtr.Zero)
                {
                    _consoleVisible = !_consoleVisible;
                    
                    if (_consoleVisible)
                    {
                        // Show and restore the console window
                        ShowWindow(_consoleWindow, SW_RESTORE);
                        ShowWindow(_consoleWindow, SW_SHOW);
                    }
                    else
                    {
                        // Hide the console window
                        ShowWindow(_consoleWindow, SW_HIDE);
                    }
                    
                    // Update menu item text
                    if (_trayIcon?.ContextMenuStrip != null && _trayIcon.ContextMenuStrip.Items.Count > 1)
                    {
                        var consoleMenuItem = _trayIcon.ContextMenuStrip.Items[1] as ToolStripMenuItem;
                        if (consoleMenuItem != null)
                        {
                            consoleMenuItem.Text = _consoleVisible ? "Hide Console" : "Show Console";
                        }
                    }
                    
                    _logger.LogInformation($"Console window {(_consoleVisible ? "shown" : "hidden")}");
                }
                else
                {
                    _logger.LogWarning("Console window handle is null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling console visibility");
            }
        }

        private void OpenApplication()
        {
            try
            {
                // Open the default browser with our URL (use correct port from launchSettings)
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5113",
                    UseShellExecute = true
                });
                _logger.LogInformation("Opening FaceVault in browser");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open browser");
            }
        }


        private void ExitApplication()
        {
            try
            {
                _logger.LogInformation("Exiting FaceVault application...");
                
                // Hide and dispose tray icon first
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
                
                // Exit the Windows Forms application loop on the tray thread
                Application.Exit();
                
                // Stop the ASP.NET Core application
                _applicationLifetime.StopApplication();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application exit");
                // Force exit if graceful shutdown fails
                Environment.Exit(0);
            }
        }

        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _trayIcon?.Dispose();
                
                // Stop the tray thread
                if (_trayThread != null && _trayThread.IsAlive)
                {
                    Application.Exit();
                    _trayThread.Join(1000);
                }
                
                _disposed = true;
            }
        }
    }
}