namespace PythonTextAnalytics;

/// <summary>
/// CSnakes Course - Windows Forms with Python Integration
/// 
/// Learning Objectives:
/// - Build desktop applications with CSnakes integration
/// - Use Python for AI/ML processing in Windows Forms apps
/// - Handle UI threading with Python operations
/// - Process text and files using Python libraries
/// - Create responsive desktop apps with Python backends
/// - Integrate modern Python AI tools with classic .NET UI
/// </summary>
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    
}