using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace FaceVault.Forms
{
    public partial class StartupForm : Form
    {
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;
        private Label titleLabel = null!;
        
        public StartupForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "FaceVault - Starting";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            
            // Title label
            titleLabel = new Label
            {
                Text = "FaceVault",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Location = new Point(12, 20),
                Size = new Size(360, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Status label
            statusLabel = new Label
            {
                Text = "Initializing...",
                Font = new Font("Segoe UI", 10),
                Location = new Point(12, 70),
                Size = new Size(360, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(40, 110),
                Size = new Size(320, 23),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };
            
            // Add controls
            this.Controls.Add(titleLabel);
            this.Controls.Add(statusLabel);
            this.Controls.Add(progressBar);
        }
        
        public void UpdateStatus(string message, int? progress = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message, progress)));
                return;
            }
            
            statusLabel.Text = message;
            
            if (progress.HasValue)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = Math.Min(100, Math.Max(0, progress.Value));
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
            }
        }
        
        public void SetProgress(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetProgress(value)));
                return;
            }
            
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = Math.Min(100, Math.Max(0, value));
        }
    }
}