using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace FaceVault.Test
{
    class TestSystemTray
    {
        [STAThread]
        static void MainTest()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var trayIcon = new NotifyIcon();
            
            // Create a simple icon
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Blue);
            }
            trayIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
            
            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Test Item 1", null, (s, e) => MessageBox.Show("Item 1 clicked"));
            contextMenu.Items.Add("Test Item 2", null, (s, e) => MessageBox.Show("Item 2 clicked"));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            });
            
            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.Text = "Test System Tray";
            trayIcon.Visible = true;
            
            // Show a balloon tip
            trayIcon.ShowBalloonTip(3000, "Test", "System tray is working!", ToolTipIcon.Info);
            
            Console.WriteLine("System tray icon created. Right-click to see menu.");
            Console.WriteLine("Icon visible: " + trayIcon.Visible);
            Console.WriteLine("Has context menu: " + (trayIcon.ContextMenuStrip != null));
            Console.WriteLine("Menu items: " + trayIcon.ContextMenuStrip?.Items.Count);
            
            Application.Run();
        }
    }
}