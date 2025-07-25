using System;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TalkToMyCode
{
    public partial class MainForm : Form
    {
        private string projectPath;
        private string projectConfigPath;
        private string codeBaseDirectory;
        private CancellationTokenSource shadowScanCts;

        public MainForm(string projectPath)
        {
            InitializeComponent();
            this.projectPath = projectPath;
            this.projectConfigPath = Path.Combine(projectPath, "project.json");
            this.Text = $"Talk To My Code - {Path.GetFileName(projectPath)}";
            this.Load += MainForm_Load;
        }

        public MainForm() : this(null) { }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load code base directory from project.json
            if (File.Exists(projectConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(projectConfigPath);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("codeBaseDirectory", out var codeBaseDirProp))
                        {
                            codeBaseDirectory = codeBaseDirProp.GetString();
                            textBoxCodeBaseDirectory.Text = codeBaseDirectory;
                        }
                        if (root.TryGetProperty("fileCount", out var fileCountProp))
                        {
                            textBoxFileCount.Text = fileCountProp.GetInt32().ToString();
                        }
                        else
                        {
                            textBoxFileCount.Text = "";
                        }
                        if (root.TryGetProperty("lineCount", out var lineCountProp))
                        {
                            textBoxLineCount.Text = lineCountProp.GetInt32().ToString();
                        }
                        else
                        {
                            textBoxLineCount.Text = "";
                        }
                    }
                }
                catch { /* Ignore errors for now */ }
            }
        }

        private void buttonSelectCodeBaseDirectory_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select Code Base Directory";
                folderDialog.ShowNewFolderButton = false;

                if (Directory.Exists(Application.StartupPath))
                {
                    folderDialog.InitialDirectory = Application.StartupPath;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    if (Directory.Exists(selectedPath))
                    {
                        textBoxCodeBaseDirectory.Text = selectedPath;
                        codeBaseDirectory = selectedPath;
                        SaveCodeBaseDirectoryToProject();
                        MessageBox.Show($"Code base directory selected: {selectedPath}",
                                      "Directory Selected",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("The selected directory does not exist.",
                                      "Invalid Directory",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void SaveCodeBaseDirectoryToProject()
        {
            // Read existing project.json or create new
            string name = Path.GetFileName(projectPath);
            string json = "{}";
            if (File.Exists(projectConfigPath))
            {
                json = File.ReadAllText(projectConfigPath);
            }
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                string projectName = name;
                if (root.TryGetProperty("name", out var nameProp))
                {
                    projectName = nameProp.GetString();
                }
                var newObj = new { name = projectName, codeBaseDirectory = codeBaseDirectory };
                string newJson = JsonSerializer.Serialize(newObj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(projectConfigPath, newJson);
            }
        }

        private void buttonCountLines_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codeBaseDirectory) || !Directory.Exists(codeBaseDirectory))
            {
                MessageBox.Show("Please select a valid code base directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var csFiles = Directory.GetFiles(codeBaseDirectory, "*.cs", SearchOption.AllDirectories);
            int totalFiles = 0;
            int totalLines = 0;
            textBoxOutput.Clear();

            foreach (var file in csFiles)
            {
                int lineCount = 0;
                try
                {
                    lineCount = File.ReadAllLines(file).Length;
                }
                catch { }
                textBoxOutput.AppendText($"{file}: {lineCount} lines\r\n");
                totalFiles++;
                totalLines += lineCount;
            }

            textBoxOutput.AppendText($"\r\nTotal files: {totalFiles}\r\nTotal lines: {totalLines}\r\n");
            textBoxFileCount.Text = totalFiles.ToString();
            textBoxLineCount.Text = totalLines.ToString();
            SaveFileAndLineCountsToProject(totalFiles, totalLines);
            CreateShadowDirectory(csFiles);
        }

        private void CreateShadowDirectory(string[] csFiles)
        {
            string shadowDir = Path.Combine(projectPath, "shadow");
            if (!Directory.Exists(shadowDir))
                Directory.CreateDirectory(shadowDir);

            foreach (var file in csFiles)
            {
                string relPath = Path.GetRelativePath(codeBaseDirectory, file);
                string destPath = Path.Combine(shadowDir, relPath);
                string? destDir = Path.GetDirectoryName(destPath);
                ArgumentNullException.ThrowIfNullOrEmpty(destDir);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                File.Copy(file, destPath, true);

                // Call stub LLM summary method and write summary file
                string summary = SummarizeFileWithLLM(destPath);
                string summaryPath = destPath + ".summary.txt";
                File.WriteAllText(summaryPath, summary);
            }
        }

        // Stub method to simulate LLM summary
        private string SummarizeFileWithLLM(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            return $"Summary for {fileName}: This is a mock summary generated by a local LLM stub.";
        }

        private void SaveFileAndLineCountsToProject(int fileCount, int lineCount)
        {
            // Read existing project.json or create new
            string name = Path.GetFileName(projectPath);
            string json = "{}";
            if (File.Exists(projectConfigPath))
            {
                json = File.ReadAllText(projectConfigPath);
            }
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                string projectName = name;
                string codeBaseDir = codeBaseDirectory;
                if (root.TryGetProperty("name", out var nameProp))
                {
                    projectName = nameProp.GetString();
                }
                if (root.TryGetProperty("codeBaseDirectory", out var codeBaseDirProp))
                {
                    codeBaseDir = codeBaseDirProp.GetString();
                }
                var newObj = new { name = projectName, codeBaseDirectory = codeBaseDir, fileCount = fileCount, lineCount = lineCount };
                string newJson = JsonSerializer.Serialize(newObj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(projectConfigPath, newJson);
            }
        }

        private async void buttonStartShadowScan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codeBaseDirectory) || !Directory.Exists(codeBaseDirectory))
            {
                MessageBox.Show("Please select a valid code base directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            buttonStartShadowScan.Enabled = false;
            buttonStopScan.Enabled = true;
            textBoxOutput.Clear();
            shadowScanCts = new CancellationTokenSource();
            var csFiles = Directory.GetFiles(codeBaseDirectory, "*.cs", SearchOption.AllDirectories);
            await Task.Run(() => ShadowScanFiles(csFiles, shadowScanCts.Token));
            buttonStartShadowScan.Enabled = true;
            buttonStopScan.Enabled = false;
        }

        private void buttonStopScan_Click(object sender, EventArgs e)
        {
            shadowScanCts?.Cancel();
        }

        private void ShadowScanFiles(string[] csFiles, CancellationToken token)
        {
            string shadowDir = Path.Combine(projectPath, "shadow");
            if (!Directory.Exists(shadowDir))
                Directory.CreateDirectory(shadowDir);

            foreach (var file in csFiles)
            {
                if (token.IsCancellationRequested)
                    break;
                string relPath = Path.GetRelativePath(codeBaseDirectory, file);
                string summaryPath = Path.Combine(shadowDir, Path.ChangeExtension(relPath, ".summary"));
                string? summaryDir = Path.GetDirectoryName(summaryPath);
                ArgumentNullException.ThrowIfNullOrEmpty(summaryDir);
                if (!Directory.Exists(summaryDir))
                    Directory.CreateDirectory(summaryDir);

                bool skip = false;
                if (File.Exists(summaryPath))
                {
                    var summaryTime = File.GetLastWriteTime(summaryPath);
                    var fileTime = File.GetLastWriteTime(file);
                    if (summaryTime > fileTime)
                        skip = true;
                }

                string statusMsg = skip
                    ? $"Skipping (up-to-date): {relPath}\r\n"
                    : $"Summarizing: {relPath}\r\n";
                AppendOutput(statusMsg);

                if (skip)
                    continue;
                string summary = SummarizeFileWithLLM(file);
                File.WriteAllText(summaryPath, summary);
            }
        }

        private void AppendOutput(string text)
        {
            if (textBoxOutput.InvokeRequired)
            {
                textBoxOutput.Invoke(new Action<string>(AppendOutput), text);
            }
            else
            {
                textBoxOutput.AppendText(text);
            }
        }

        private void menuItemOpenProject_Click(object sender, EventArgs e)
        {
            using (var projectDialog = new ProjectDialog())
            {
                if (projectDialog.ShowDialog() == DialogResult.OK)
                {
                    string newProjectPath = projectDialog.SelectedProjectPath;
                    Program.SaveLastProjectPath(newProjectPath);
                    // Restart the app with the new project
                    Process.Start(Application.ExecutablePath);
                    Application.Exit();
                }
            }
        }

        private void buttonCheckOldSummaries_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codeBaseDirectory) || !Directory.Exists(codeBaseDirectory))
            {
                MessageBox.Show("Please select a valid code base directory first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            textBoxOutput.Clear();
            string shadowDir = Path.Combine(projectPath, "shadow");
            var csFiles = Directory.GetFiles(codeBaseDirectory, "*.cs", SearchOption.AllDirectories);
            int updatedCount = 0;
            foreach (var file in csFiles)
            {
                string relPath = Path.GetRelativePath(codeBaseDirectory, file);
                string summaryPath = Path.Combine(shadowDir, Path.ChangeExtension(relPath, ".summary"));
                bool needsUpdate = false;
                if (!File.Exists(summaryPath))
                {
                    needsUpdate = true;
                }
                else
                {
                    var summaryTime = File.GetLastWriteTime(summaryPath);
                    var fileTime = File.GetLastWriteTime(file);
                    if (summaryTime <= fileTime)
                        needsUpdate = true;
                }
                if (needsUpdate)
                {
                    string summary = SummarizeFileWithLLM(file);
                    string? summaryDir = Path.GetDirectoryName(summaryPath);
                    ArgumentNullException.ThrowIfNullOrEmpty(summaryDir);
                    if (!Directory.Exists(summaryDir))
                        Directory.CreateDirectory(summaryDir);
                    File.WriteAllText(summaryPath, summary);
                    textBoxOutput.AppendText($"Updated summary: {relPath}\r\n");
                    updatedCount++;
                }
            }
            textBoxOutput.AppendText($"\r\nTotal summaries updated: {updatedCount}\r\n");
        }

        private void buttonOlamaInstructions_Click(object sender, EventArgs e)
        {
            using (var dlg = new OlamaInstructionsDialog())
            {
                dlg.ShowDialog(this);
            }
        }
    }
}
