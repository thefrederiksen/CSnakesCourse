// ------------------------------------------------------------
// Program.cs   (Lab07_KMeans)
// ------------------------------------------------------------
// Demonstrates calling scikit-learn K-Means inside the same
// .NET process via CSnakes — no HTTP, no PyObject juggling.
// ------------------------------------------------------------
using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace KMeans_In_Process
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CSnakes Lab : In-Process K-Means ===\n");

            // ── 1. Locate the Python home folder (sits beside the EXE) ──────────
            var exeDir = Path.GetDirectoryName(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var pythonHomeDir = Path.Join(exeDir, "Python");          // contains kmeans_sample.py
            var virtualDir = Path.Join(pythonHomeDir, ".venv_uv"); // will be created on first run
            var requirements = Path.Combine(pythonHomeDir, "requirements.txt");

            // ── 2. Build the host & configure CSnakes runtime ──────────────────
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                   .WithPython()
                       .WithHome(pythonHomeDir)
                       .FromRedistributable("3.12")    // downloads CPython the very first time
                       .WithVirtualEnvironment(virtualDir)
                       .WithUvInstaller(requirements);

            using var app = builder.Build();

            // ── 3. Warm-up: create env + install packages (idempotent) ─────────
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Creating environment and installing packages...");
            var pyEnv = app.Services.GetRequiredService<IPythonEnvironment>();
            Console.WriteLine($"Done – {sw.ElapsedMilliseconds} ms\n");

            sw.Restart();
            // Show the version of the python code and load the python module
            Console.WriteLine($"Code version: {pyEnv.KmeansSample().GetVersion()}");
            Console.WriteLine($"Module loaded in {sw.ElapsedMilliseconds} ms\n");

            // ── 4. Sample data: two easy clusters ──────────────────────────────
            (double X, double Y)[] points = GetSampleData();

            Console.WriteLine($"Generated {5*points.Length} points in 5 clusters with random noise.\n");

            // ── 5. Call the generated Python stub ──────────────────────────────
            var swKmeans = Stopwatch.StartNew();
            var (centroids, inertia) = pyEnv.KmeansSample().FitKmeans(points, nClusters: 5);
            Console.WriteLine($"KmeansSample() call took {swKmeans.ElapsedMilliseconds} ms");

            // ── 6. Display the results ─────────────────────────────────────────
            Console.WriteLine("Centroids (X, Y):");
            for (int i = 0; i < centroids.Count; i++)
            {
                var row = centroids[i];
                Console.WriteLine($"  Cluster {i + 1}: ({row[0]:0.00}, {row[1]:0.00})");
            }

            Console.WriteLine($"\nInertia: {inertia:0.000}");
        }

        private static (double X, double Y)[] GetSampleData()
        {
            // Generate 2 million points in 5 clusters
            var rnd = new Random(42);
            var points = new List<(double X, double Y)>();
            int clusters = 5;
            int pointsPerCluster = 2_000 / clusters;
            var centers = new (double X, double Y)[]
            {
                (2, 2), (8, 2), (2, 8), (8, 8), (5, 5)
            };

            Console.WriteLine("Intended cluster centers:");
            for (int i = 0; i < centers.Length; i++)
            {
                Console.WriteLine($"  Center {i + 1}: ({centers[i].X:0.00}, {centers[i].Y:0.00})");
            }
            Console.WriteLine();

            double spread = 1.0;
            for (int c = 0; c < clusters; c++)
            {
                var center = centers[c];
                for (int i = 0; i < pointsPerCluster; i++)
                {
                    double x = center.X + (rnd.NextDouble() * 2 - 1) * spread;
                    double y = center.Y + (rnd.NextDouble() * 2 - 1) * spread;
                    points.Add((x, y));
                }
            }
            return points.ToArray();
        }
    }
}
