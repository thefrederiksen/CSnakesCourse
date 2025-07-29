using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FaceVault.Forms;

namespace FaceVault
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Check for single instance
            const string appGuid = "FaceVault-{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}";
            using (Mutex mutex = new Mutex(true, appGuid, out bool createdNew))
            {
                if (!createdNew)
                {
                    // Application is already running - open browser and exit
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "http://localhost:5113",
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // If browser fails to open, at least show a message
                        MessageBox.Show("FaceVault is already running. Please check your system tray.", 
                            "FaceVault", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }
                
                // First instance - continue with normal startup
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Start the Blazor server with a progress window
                var starter = new BlazorServerStarter();
                starter.Start(args);
                
                // Keep mutex alive for the lifetime of the application
                GC.KeepAlive(mutex);
            }
        }
    }
}