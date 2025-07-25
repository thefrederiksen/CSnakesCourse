using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace TalkToMyCode
{
    public class OlamaInstructionsDialog : Form
    {
        public OlamaInstructionsDialog()
        {
            this.Text = "Olama (Ollama) Install Instructions";
            this.Width = 600;
            this.Height = 350;
            var richText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = true,
                Text =
                    "How to Install Olama (Ollama):\n\n" +
                    "1. Go to https://ollama.com/download and download the installer for your OS.\n" +
                    "2. Run the installer and follow the on-screen instructions.\n" +
                    "3. After installation, open a terminal and run: ollama --version\n" +
                    "   (You should see the installed version if successful.)\n" +
                    "4. To download a model, run: ollama pull llama2\n" +
                    "5. To start the Ollama server, run: ollama serve\n\n"
            };
            richText.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            this.Controls.Add(richText);
            var btnClose = new Button { Text = "Close", Dock = DockStyle.Bottom };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }
    }
} 