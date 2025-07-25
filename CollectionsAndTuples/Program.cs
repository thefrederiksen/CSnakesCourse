using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CSnakes;
using System.Diagnostics;
using CSnakes.Runtime;

namespace CollectionsAndTuples
{
    public class Program
    {
        /// <summary>
        /// Entry point for the CSnakes Lab 3: Collections & Tuples demo application.
        /// Sets up the Python environment and runs all demonstration methods.
        /// </summary>
        public static void Main(string[] args)
        {
            // Set console encoding to UTF-8 for proper character display
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                // Ignore encoding errors on some systems
            }
            Console.WriteLine("=== CSnakes Lab 3: Collections & Tuples ===\n");

            // Create the host with CSnakes configuration using ApplicationBuilder pattern
            var builder = Host.CreateApplicationBuilder(args);
            var home = Path.Join(Environment.CurrentDirectory, "."); // Path to your Python modules
            builder.Services
                .WithPython()
                .WithHome(home)
                .FromRedistributable();

            var host = builder.Build();
            host.Start();

            var env = host.Services.GetRequiredService<IPythonEnvironment>();

            // Demo 1: Employee Processing
            DemoEmployeeProcessing(env);

            // Demo 2: Team Statistics
            DemoTeamStatistics(env);

            // Demo 3: Department Data Merging
            DemoDepartmentMerging(env);

            // Demo 4: Optional Data Handling
            DemoOptionalHandling(env);

            // Demo 5: Complex Nested Structures
            DemoNestedStructures(env);

            // Performance Demonstration
            DemoPerformance(env);

            host.StopAsync();
            Console.WriteLine("\n✅ Lab 3 Complete! Collections mastered.");
        }

        /// <summary>
        /// Demonstrates employee categorization by age using a Python function.
        /// Shows marshaling of a list of tuples from C# to Python and back.
        /// </summary>
        /// <param name="env">The Python environment to use for interop.</param>
        static void DemoEmployeeProcessing(IPythonEnvironment env)
        {
            Console.WriteLine("Demo 1: Employee Categorization");
            Console.WriteLine("--------------------------------");

            // Create sample employee data (name, age)
            var employees = new List<(string, long)>
            {
                ("Alice Johnson", 28),
                ("Bob Smith", 42),
                ("Charlie Brown", 23),
                ("Diana Prince", 35),
                ("Eve Adams", 45)
            };

            Console.WriteLine("Input employees:");
            foreach (var (name, age) in employees)
            {
                Console.WriteLine($"  {name}, {age}");
            }

            // Call Python function - automatic marshaling!
            IReadOnlyDictionary<string, string> categories = env.CollectionsDemo().ProcessEmployees(employees);

            Console.WriteLine("\nEmployee categories:");
            foreach (var kvp in categories)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates calculation of team performance statistics using nested collections.
        /// Passes a dictionary of team scores to Python and receives a dictionary of statistics.
        /// </summary>
        /// <param name="env">The Python environment to use for interop.</param>
        static void DemoTeamStatistics(IPythonEnvironment env)
        {
            Console.WriteLine("Demo 2: Team Performance Statistics");
            Console.WriteLine("-----------------------------------");

            // Complex nested collections
            var teamScores = new Dictionary<string, IReadOnlyList<double>>
            {
                ["Engineering"] = new List<double> { 85.5, 92.0, 78.5, 88.0, 90.5 },
                ["Marketing"] = new List<double> { 75.0, 82.5, 79.0, 85.0 },
                ["Sales"] = new List<double> { 95.0, 88.5, 92.0, 89.5, 91.0, 87.5 },
                ["EmptyTeam"] = new List<double>() // Test edge case
            };

            Console.WriteLine("Team scores:");
            foreach (var team in teamScores)
            {
                Console.WriteLine($"  {team.Key}: [{string.Join(", ", team.Value)}]");
            }

            // Call Python - returns Dict[str, Tuple[float, int]]
            var stats = env.CollectionsDemo().CalculateTeamStats(teamScores);

            Console.WriteLine("\nTeam statistics (average, count):");
            foreach (var kvp in stats)
            {
                Console.WriteLine($"  {kvp.Key}: avg={kvp.Value.Item1:F2}, count={kvp.Value.Item2}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates merging of department employee and salary data using Python.
        /// Combines a list of employees and a dictionary of salaries into enriched data.
        /// </summary>
        /// <param name="env">The Python environment to use for interop.</param>
        static void DemoDepartmentMerging(IPythonEnvironment env)
        {
            Console.WriteLine("Demo 3: Department Data Merging");
            Console.WriteLine("-------------------------------");

            var employees = new List<(string, long)>
            {
                ("John Doe", 30L),
                ("Jane Smith", 28L),
                ("Mike Johnson", 35L)
            };

            var salaries = new Dictionary<string, double>
            {
                ["John Doe"] = 75000.0,
                ["Jane Smith"] = 68000.0,
                ["Mike Johnson"] = 82000.0
            };

            Console.WriteLine("Merging employee data with salaries...");

            // Returns List[Tuple[str, int, float]] -> IReadOnlyList<(string, int, double)>
            var enrichedData = env.CollectionsDemo().MergeDepartmentData(employees, salaries);

            Console.WriteLine("Enriched employee data:");
            foreach (var (name, age, salary) in enrichedData)
            {
                Console.WriteLine($"  {name}: {age} years old, ${salary:N0}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates handling of optional/nullable data in collections using Python.
        /// Passes a list of tuples with nullable values and receives processed results.
        /// </summary>
        /// <param name="env">The Python environment to use for interop.</param>
        static void DemoOptionalHandling(IPythonEnvironment env)
        {
            Console.WriteLine("Demo 4: Optional/Nullable Data Handling");
            Console.WriteLine("---------------------------------------");

            // Demonstrate nullable handling
            var mixedData = new List<(string, long?)>
            {
                ("Complete Record", 30L),
                ("Missing Age", null),
                ("Another Complete", 25L),
                ("Also Missing", null),
            };

            Console.WriteLine("Input data with some null ages:");
            foreach (var (name, age) in mixedData)
            {
                Console.WriteLine($"  {name}: {(age?.ToString() ?? "null")}");
            }

            var processed = env.CollectionsDemo().HandleOptionalData(mixedData);

            Console.WriteLine("\nProcessed results:");
            foreach (var kvp in processed)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates processing of complex nested structures using Python.
        /// Passes a dictionary of departments with employee details and receives a summary.
        /// </summary>
        /// <param name="env">The Python environment to use for interop.</param>
        static void DemoNestedStructures(IPythonEnvironment env)
        {
            Console.WriteLine("Demo 5: Complex Nested Structures");
            Console.WriteLine("---------------------------------");

            // Complex nested structure: departments with employee details
            //
            // Dictionary key: Department name (string)
            // Value: List of tuples, each representing an employee:
            //   (string name, long age, double salary)
            var departments = new Dictionary<string, IReadOnlyList<(string, long, double)>>
            {
                ["Engineering"] = new List<(string, long, double)>
                {
                    ("Alice Dev", 30L, 85000),
                    ("Bob Code", 28L, 75000),
                    ("Carol Arch", 35L, 95000)
                },
                ["Marketing"] = new List<(string, long, double)>
                {
                    ("Dan Brand", 32L, 65000),
                    ("Eve Social", 26L, 55000)
                },
                ["Sales"] = new List<(string, long, double)>
                {
                    ("Frank Deal", 40L, 70000),
                    ("Grace Close", 29L, 62000),
                    ("Henry Pitch", 33L, 68000)
                }
            };

            Console.WriteLine("Department data:");
            foreach (var dept in departments)
            {
                Console.WriteLine($"  {dept.Key}: {dept.Value.Count} employees");
            }

            IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> summary = env.CollectionsDemo().ProcessNestedStructures(departments);

            Console.WriteLine("\nDepartment summary:");
            foreach (var dept in summary)
            {
                Console.WriteLine($"  {dept.Key}:");
                foreach (var stat in dept.Value)
                {
                    Console.WriteLine($"    {stat.Key}: {stat.Value:F2}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates performance of in-process Python interop with a large dataset.
        /// Measures and prints timing statistics for repeated calls to a Python function.
        /// </summary>
        /// <param name="env">The Python environment to use for interop.</param>
        static void DemoPerformance(IPythonEnvironment env)
        {
            Console.WriteLine("Demo 6: Performance Comparison");
            Console.WriteLine("------------------------------");

            var largeDataset = Enumerable.Range(1, 1000)
                .Select(i => ($"Employee_{i}", 20L + (i % 30)))
                .ToList();
            IReadOnlyList<(string, long)> largeDatasetReadOnly = largeDataset;

            Console.WriteLine($"Processing {largeDataset.Count} employee records...");

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                var result = env.CollectionsDemo().ProcessEmployees(largeDatasetReadOnly);
            }
            stopwatch.Stop();

            Console.WriteLine($"✅ 100 iterations with {largeDataset.Count} records each:");
            Console.WriteLine($"   Total time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   Average per call: {stopwatch.ElapsedMilliseconds / 100.0:F2}ms");
            Console.WriteLine($"   Records per second: {(largeDataset.Count * 100) / (stopwatch.ElapsedMilliseconds / 1000.0):N0}");
            Console.WriteLine("\n💡 No HTTP serialization overhead - this is in-process speed!");
        }
    }
}