using CSnakes.Runtime;
using CSnakes.Runtime.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data;
using System.Diagnostics;

namespace DataTable_Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CSnakes Lab : Send DataTable to Python ===\n");

            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pythonHomeDir = Path.Join(exeDir, "Python");


            try
            {
                var requirements = Path.Combine(pythonHomeDir, "requirements.txt");
                var virtualDir = Path.Join(pythonHomeDir, ".venv_uv");
                var builder = Host.CreateApplicationBuilder();
                builder.Services
                    .WithPython()
                    .WithHome(pythonHomeDir)
                    .FromRedistributable("3.12")
                    .WithVirtualEnvironment(virtualDir)
                    .WithUvInstaller(requirements);

                using var app = builder.Build();

                // ---------- 0. create environment and install packages ----------
                var sw = Stopwatch.StartNew();
                Console.WriteLine("Creating environment and installing packages...");
                var pythonEnv = app.Services.GetRequiredService<IPythonEnvironment>();
                Console.WriteLine($"Done - {sw.ElapsedMilliseconds} ms");

                // ---------- 1. get sample data ----------
                DataTable sales = GetData();
                PrintTable(sales);

                // ---------- 2. convert for Python ----------
                var rows = ConvertDataTable(sales);

                // call Python
                var pivotRows = pythonEnv.Analytics().RevenueByRegionCategory(rows);
                var pivotTable = ConvertRowsToDataTable(pivotRows);
                PrintTable(pivotTable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during package installation: {ex}");
            }
        }

        // -------------------------------------------------------------------------
        // Returns a DataTable of sample sales (Region × Category × Revenue)
        // -------------------------------------------------------------------------
        private static DataTable GetData()
        {
            var table = new DataTable("Sales");
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("Category", typeof(string));
            table.Columns.Add("Revenue", typeof(double));

            table.Rows.Add("North", "Widgets", 1200);
            table.Rows.Add("North", "Widgets", 800);
            table.Rows.Add("North", "Gadgets", 500);
            table.Rows.Add("South", "Widgets", 600);
            table.Rows.Add("South", "Gadgets", 700);
            table.Rows.Add("South", "Gizmos", 300);

            return table;
        }

        // ---------------------------------------------------------------------
        // ConvertDataTable – DataTable ➜ List<Dictionary<string,PyObject>>
        // ---------------------------------------------------------------------
        private static List<Dictionary<string, PyObject>> ConvertDataTable(DataTable dt)
        {
            return dt.AsEnumerable()
                     .Select(r => dt.Columns
                                    .Cast<DataColumn>()
                                    .ToDictionary(c => c.ColumnName,
                                                  c => PyObject.From(r[c]))) // ← wrap each value
                     .ToList();
        }

        // -------------------------------------------------------------------------
        // IReadOnlyList<IReadOnlyDictionary<string,PyObject>>  ➜  DataTable
        // -------------------------------------------------------------------------
        private static DataTable ConvertRowsToDataTable(
            IReadOnlyList<IReadOnlyDictionary<string, PyObject>> rows,
            string tableName = "Pivot")
        {
            var table = new DataTable(tableName);
            if (rows.Count == 0) return table;

            // create columns from the first row’s keys
            foreach (var col in rows[0].Keys)
                table.Columns.Add(col, typeof(object));

            // add the data (PyObject is fine – DataTable stores boxed objects)
            foreach (var r in rows)
                table.Rows.Add(r.Values.ToArray());

            return table;
        }

        // ---------------------------------------------------------------------
        // Print helper – just for console output
        // ---------------------------------------------------------------------
        private static void PrintTable(DataTable dt)
        {
            Console.WriteLine();
            foreach (DataColumn col in dt.Columns)
                Console.Write($"{col.ColumnName,10}");
            Console.WriteLine();

            foreach (DataRow r in dt.Rows)
            {
                foreach (var item in r.ItemArray)
                    Console.Write($"{item,10}");
                Console.WriteLine();
            }
        }

    }
}
