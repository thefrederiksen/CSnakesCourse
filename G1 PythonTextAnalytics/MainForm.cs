using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace PythonTextAnalytics;

public partial class MainForm : Form
{
    private IPythonEnvironment? _pythonEnv;
    private IHost? _host;
    private string _selectedFilePath = string.Empty;

    public MainForm()
    {
        InitializeComponent();
        _ = InitializePythonAsync(); // Fire and forget - will run in background
    }

    private async Task InitializePythonAsync()
    {
        try
        {
            LogMessage("Starting Python environment initialization");
            UpdateStatus("Initializing Python environment...");
            
            // ── 1. Locate the Python home folder (sits beside the EXE) ──────────
            var exeDir = Path.GetDirectoryName(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var pythonHomeDir = Path.Join(exeDir, "Python");          // contains text_processor.py
            var virtualDir = Path.Join(pythonHomeDir, ".venv_uv");    // will be created on first run
            var requirements = Path.Combine(pythonHomeDir, "requirements.txt");
            
            LogMessage($"Exe directory: {exeDir}");
            LogMessage($"Python home: {pythonHomeDir}");
            LogMessage($"Virtual env: {virtualDir}");
            LogMessage($"Requirements: {requirements}");

            // ── 2. Build the host & configure CSnakes runtime ──────────────────
            LogMessage("Building host and configuring CSnakes runtime");
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                   .WithPython()
                       .WithHome(pythonHomeDir)
                       .FromRedistributable("3.12")    // downloads CPython the very first time
                       .WithVirtualEnvironment(virtualDir)
                       .WithUvInstaller(requirements);

            _host = builder.Build();
            LogMessage("Host built successfully");

            // ── 3. Warm-up: create env + install packages (idempotent) ─────────
            LogMessage("Starting Python environment warmup");
            var sw = Stopwatch.StartNew();
            _pythonEnv = await Task.Run(() => _host.Services.GetRequiredService<IPythonEnvironment>());
            LogMessage("Python environment service obtained");
            
            // Test the connection and get version
            LogMessage("Testing Python connection and getting version");
            var version = await Task.Run(() => _pythonEnv.TextProcessor().GetVersion());
            LogMessage($"Python version obtained: {version}");
            
            UpdateStatus($"Ready! ({sw.ElapsedMilliseconds}ms) - {version}");
            LogMessage($"Python initialization complete in {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            LogError("Failed to initialize Python environment", ex);
            UpdateStatus($"Error initializing Python: {ex.Message}");
            MessageBox.Show($"Failed to initialize Python environment:\n{ex.Message}", 
                          "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnSelectFile_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Title = "Select a text file to analyze",
            Filter = "All Supported|*.txt;*.pdf;*.docx;*.md|" +
                    "Text Files|*.txt|" +
                    "PDF Files|*.pdf|" +
                    "Word Documents|*.docx|" +
                    "Markdown Files|*.md;*.markdown|" +
                    "All Files|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            _selectedFilePath = openFileDialog.FileName;
            
            // Clear debug log for new file
            txtLog.Text = "Application log will appear here...";
            
            LogMessage($"File selected: {_selectedFilePath}");
            lblSelectedFile.Text = $"Selected: {Path.GetFileName(_selectedFilePath)}";
            
            // Clear previous results
            pictureBoxWordCloud.Image?.Dispose();
            pictureBoxWordCloud.Image = null;
            txtFrequencies.Clear();
            
            // Auto-generate word cloud if Python is ready
            if (_pythonEnv != null)
            {
                _ = GenerateWordCloudAsync();
            }
        }
    }

    private async Task GenerateWordCloudAsync()
    {
        if (_pythonEnv == null)
        {
            LogMessage("ERROR: Python environment not initialized");
            UpdateStatus("Error: Python not ready");
            return;
        }

        if (string.IsNullOrEmpty(_selectedFilePath))
        {
            LogMessage("ERROR: No file selected");
            UpdateStatus("Error: No file selected");
            return;
        }

        try
        {
            LogMessage($"Starting word cloud generation for: {Path.GetFileName(_selectedFilePath)}");
            UpdateStatus("Processing file...");

            // Show progress
            var sw = Stopwatch.StartNew();

            // Call Python to process the file
            LogMessage("Calling Python TextProcessor.ProcessFileComplete()");
            var result = await Task.Run(() => _pythonEnv.TextProcessor().ProcessFileComplete(_selectedFilePath));
            LogMessage($"Python call completed in {sw.ElapsedMilliseconds}ms");

            var processingTime = sw.ElapsedMilliseconds;

            // Handle the result
            LogMessage("Processing Python result dictionary");
            if (result.TryGetValue("success", out var successObj) && successObj.As<bool>())
            {
                LogMessage("Processing successful, extracting results");
                
                // Display word cloud image
                if (result.TryGetValue("word_cloud_base64", out var imageObj))
                {
                    LogMessage("Found word cloud base64 data, displaying image");
                    await DisplayWordCloudImage(imageObj.As<string>());
                }
                else
                {
                    LogMessage("WARNING: No word_cloud_base64 found in result");
                }

                // Display frequency data
                if (result.TryGetValue("frequencies_text", out var freqTextObj) && 
                    result.TryGetValue("word_count", out var wordCountObj) && 
                    result.TryGetValue("unique_words", out var uniqueWordsObj))
                {
                    LogMessage("Extracting frequency and count data");
                    
                    var frequenciesText = freqTextObj.As<string>();
                    var wordCount = wordCountObj.As<long>();
                    var uniqueWords = uniqueWordsObj.As<long>();
                    
                    LogMessage($"Word count: {wordCount}, Unique words: {uniqueWords}");
                    DisplayFrequenciesText(frequenciesText, wordCount, uniqueWords);
                    UpdateStatus($"Complete! Words: {wordCount:N0}, Unique: {uniqueWords:N0} ({processingTime}ms)");
                }
                else
                {
                    LogMessage("WARNING: Missing frequency/count data in result");
                }
            }
            else
            {
                LogMessage("Processing failed, extracting error message");
                string errorMsg = "Unknown error";
                if (result.TryGetValue("error", out var errorObj))
                {
                    errorMsg = errorObj.As<string>();
                    LogMessage($"Python error: {errorMsg}");
                }
                else
                {
                    LogMessage("No error message found in result dictionary");
                }
                
                UpdateStatus($"Error: {errorMsg}");
                MessageBox.Show($"Processing failed:\n{errorMsg}", "Processing Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            LogError("Exception during word cloud generation", ex);
            UpdateStatus($"Error: {ex.Message}");
            MessageBox.Show($"An error occurred:\n{ex.Message}", "Error", 
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Task DisplayWordCloudImage(string base64Image)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Image))
            {
                pictureBoxWordCloud.Image = null;
                return Task.CompletedTask;
            }

            // Convert base64 to image
            var imageBytes = Convert.FromBase64String(base64Image);
            using var ms = new MemoryStream(imageBytes);
            
            // Dispose previous image
            pictureBoxWordCloud.Image?.Dispose();
            
            // Set new image
            pictureBoxWordCloud.Image = Image.FromStream(ms);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error displaying word cloud:\n{ex.Message}", "Display Error", 
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return Task.CompletedTask;
        }
    }

    private void DisplayFrequenciesText(string frequenciesText, long wordCount, long uniqueWords)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== WORD ANALYSIS RESULTS ===");
            sb.AppendLine($"Total Words: {wordCount:N0}");
            sb.AppendLine($"Unique Words: {uniqueWords:N0}");
            sb.AppendLine($"File: {Path.GetFileName(_selectedFilePath)}");
            sb.AppendLine();
            sb.AppendLine("=== TOP WORD FREQUENCIES ===");
            sb.AppendLine(frequenciesText);

            txtFrequencies.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            txtFrequencies.Text = $"Error displaying frequencies: {ex.Message}";
        }
    }

    private void UpdateStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(message));
            return;
        }

        lblStatus.Text = $"Status: {message}";
        Application.DoEvents(); // Allow UI to update
    }

    private void LogMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => LogMessage(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";
        
        if (txtLog.Text == "Application log will appear here...")
        {
            txtLog.Text = logEntry;
        }
        else
        {
            txtLog.AppendText(Environment.NewLine + logEntry);
        }
        
        // Auto-scroll to bottom
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
        Application.DoEvents();
    }

    private void LogError(string message, Exception ex)
    {
        LogMessage($"ERROR: {message}");
        LogMessage($"Exception: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
        {
            LogMessage($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
        LogMessage($"Stack Trace: {ex.StackTrace}");
    }

    private void btnCopyLog_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtLog.Text) || txtLog.Text == "Application log will appear here...")
            {
                MessageBox.Show("No log data to copy.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(txtLog.Text);
            MessageBox.Show("Debug log copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

}