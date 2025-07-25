using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;

namespace Format_Markdown
{
    internal class Program
    {
        // 10 style presets
        // Use a class instead of record for compatibility
        class PdfStyle
        {
            public string Name { get; set; }
            public string Primary { get; set; }
            public string Secondary { get; set; }
            public string Font { get; set; }
            public double Margin { get; set; }
            public string TableHeader { get; set; }
            public PdfStyle(string name, string primary, string secondary, string font, double margin, string tableHeader)
            {
                Name = name;
                Primary = primary;
                Secondary = secondary;
                Font = font;
                Margin = margin;
                TableHeader = tableHeader;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== CSnakes Lab : Format Markdown ===\n");

            PdfStyle[] styles = new PdfStyle[]
            {
                new PdfStyle("Classic Blue",   "#2c3e50", "#3498db", "Helvetica",    0.75, "#34495e"),
                new PdfStyle("Emerald",       "#2ecc71", "#27ae60", "Helvetica",    0.75, "#2ecc71"),
                new PdfStyle("Crimson",       "#e74c3c", "#c0392b", "Times-Roman", 1.0,  "#e74c3c"),
                new PdfStyle("Orange",        "#f39c12", "#e67e22", "Helvetica",    0.5,  "#f39c12"),
                new PdfStyle("Purple",        "#8e44ad", "#9b59b6", "Times-Roman", 1.0,  "#8e44ad"),
                new PdfStyle("Teal",          "#1abc9c", "#16a085", "Helvetica",    0.75, "#1abc9c"),
                new PdfStyle("Slate Gray",    "#7f8c8d", "#95a5a6", "Helvetica",    0.75, "#7f8c8d"),
                new PdfStyle("Gold",          "#ffd700", "#bfa100", "Times-Roman", 1.0,  "#ffd700"),
                new PdfStyle("Pink",          "#fd79a8", "#e17055", "Helvetica",    0.5,  "#fd79a8"),
                new PdfStyle("Black",         "#222831", "#393e46", "Times-Roman", 1.0,  "#222831"),
            };
            Console.WriteLine("Choose a PDF style:");
            for (int i = 0; i < styles.Length; i++)
                Console.WriteLine($"{i + 1}. {styles[i].Name}");
            Console.Write($"Enter 1-{styles.Length} (default 1): ");
            var styleChoice = Console.ReadLine();
            int styleIndex = 0;
            if (!string.IsNullOrWhiteSpace(styleChoice) && int.TryParse(styleChoice, out int idx) && idx >= 1 && idx <= styles.Length)
                styleIndex = idx - 1;
            var style = styles[styleIndex];

            // ── 1. Locate the Python home folder (side-by-side with the EXE) ────
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var pythonHomeDir = Path.Join(exeDir, "Python");          // contains format_markdown.py
            var virtualDir = Path.Join(pythonHomeDir, ".venv_uv"); // created on first run
            var requirements = Path.Combine(pythonHomeDir, "requirements.txt");

            // ── 2. Build the host & configure CSnakes runtime ──────────────────
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                   .WithPython()
                       .WithHome(pythonHomeDir)
                       .FromRedistributable("3.12")       // downloads CPython on the first run
                       .WithVirtualEnvironment(virtualDir)
                       .WithUvInstaller(requirements);

            using var app = builder.Build();

            // ── 3. Warm-up: create env + install packages (idempotent) ─────────
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Creating environment and installing packages...");
            var pyEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Console.WriteLine($"Done – {sw.ElapsedMilliseconds} ms\n");

            // ── 4. Show the version of the Python code and load the module ─────
            sw.Restart();
            Console.WriteLine($"Code version: {pyEnv.FormatMarkdown().GetVersion()}");
            Console.WriteLine($"Module loaded in {sw.ElapsedMilliseconds} ms\n");

            // ── 5. Convert Markdown to PDF using Python interop ───────────────
            var inputMd = Path.Combine(pythonHomeDir, "report.md");
            var outputPdf = Path.Combine(exeDir, "report.pdf");
            if (!File.Exists(inputMd))
            {
                Console.WriteLine($"Markdown file not found: {inputMd}");
                return;
            }

            // Check if the output PDF is open/locked before generating
            bool canWritePdf = true;
            if (File.Exists(outputPdf))
            {
                try
                {
                    using (var fs = new FileStream(outputPdf, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        // If we get here, the file is not locked
                    }
                }
                catch (IOException)
                {
                    canWritePdf = false;
                    Console.WriteLine($"The PDF file '{outputPdf}' is currently open or locked. Please close it and try again.");
                    return;
                }
            }

            Console.WriteLine($"Converting {inputMd} to PDF with style {style.Name}...");
            var pdfPath = pyEnv.FormatMarkdown().MarkdownToPdf(
                inputMd, outputPdf, style.Primary, style.Secondary, style.Font, style.Margin, style.TableHeader);
            Console.WriteLine($"PDF generated at: {pdfPath}");

            // Open the PDF after generation (Windows only)
            try
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo(pdfPath)
                {
                    UseShellExecute = true
                };
                process.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open PDF automatically: {ex.Message}");
            }
        }
    }
}
