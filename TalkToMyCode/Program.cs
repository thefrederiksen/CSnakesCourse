using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace TalkToMyCode
{
    internal static class Program
    {
        private static string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            string selectedProjectPath = LoadLastProjectPath();
            if (string.IsNullOrEmpty(selectedProjectPath) || !Directory.Exists(selectedProjectPath))
            {
                using (var projectDialog = new ProjectDialog())
                {
                    if (projectDialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedProjectPath = projectDialog.SelectedProjectPath;
                        SaveLastProjectPath(selectedProjectPath);
                    }
                }
            }
            if (!string.IsNullOrEmpty(selectedProjectPath))
            {
                Application.Run(new MainForm(selectedProjectPath));
            }
        }

        private static string LoadLastProjectPath()
        {
            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("lastProjectPath", out var pathProp))
                        {
                            return pathProp.GetString();
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        public static void SaveLastProjectPath(string projectPath)
        {
            var obj = new { lastProjectPath = projectPath };
            string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
    }
}