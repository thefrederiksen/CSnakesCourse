using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FaceVault.Services;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public static class Logger
{
    private static readonly object _lock = new();
    private static StreamWriter? _logWriter;
    private static string _logFilePath = string.Empty;
    private static LogLevel _minimumLevel = LogLevel.Info;

    public static event EventHandler<LogEventArgs>? LogMessage;

    public static void Initialize(string logDirectory, LogLevel minimumLevel = LogLevel.Info)
    {
        _minimumLevel = minimumLevel;
        
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(logDirectory, $"FaceVault_{timestamp}.log");
        
        try
        {
            _logWriter = new StreamWriter(_logFilePath, append: true)
            {
                AutoFlush = true
            };
            
            Log(LogLevel.Info, $"FaceVault Logger initialized. Log file: {_logFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
        }
    }

    public static void Log(LogLevel level, string message, 
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (level < _minimumLevel)
            return;

        var className = Path.GetFileNameWithoutExtension(filePath);
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            ClassName = className,
            MethodName = memberName,
            LineNumber = lineNumber
        };

        WriteLog(logEntry);
    }

    public static void LogException(Exception ex, string? additionalMessage = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
        var message = string.IsNullOrEmpty(additionalMessage) 
            ? $"Exception: {ex.GetType().Name} - {ex.Message}" 
            : $"{additionalMessage} | Exception: {ex.GetType().Name} - {ex.Message}";

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Error,
            Message = message,
            ClassName = className,
            MethodName = memberName,
            LineNumber = lineNumber,
            StackTrace = ex.StackTrace
        };

        WriteLog(logEntry);
    }

    private static void WriteLog(LogEntry entry)
    {
        lock (_lock)
        {
            var logLine = FormatLogEntry(entry);
            
            // Write to file
            _logWriter?.WriteLine(logLine);
            
            // Write to Debug output
            System.Diagnostics.Debug.WriteLine(logLine);
            
            // Raise event for UI updates
            LogMessage?.Invoke(null, new LogEventArgs(entry));
        }
    }

    private static string FormatLogEntry(LogEntry entry)
    {
        var levelStr = entry.Level.ToString().PadRight(8);
        var location = $"{entry.ClassName}.{entry.MethodName}:{entry.LineNumber}";
        
        var logLine = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{levelStr}] {location,-50} | {entry.Message}";
        
        if (!string.IsNullOrEmpty(entry.StackTrace))
        {
            logLine += Environment.NewLine + "StackTrace: " + entry.StackTrace;
        }
        
        return logLine;
    }

    public static void Close()
    {
        lock (_lock)
        {
            _logWriter?.Close();
            _logWriter?.Dispose();
            _logWriter = null;
        }
    }

    // Convenience methods
    public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => Log(LogLevel.Debug, message, memberName, filePath, lineNumber);

    public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => Log(LogLevel.Info, message, memberName, filePath, lineNumber);

    public static void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => Log(LogLevel.Warning, message, memberName, filePath, lineNumber);

    public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => Log(LogLevel.Error, message, memberName, filePath, lineNumber);

    public static void Critical(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        => Log(LogLevel.Critical, message, memberName, filePath, lineNumber);
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string? StackTrace { get; set; }
}

public class LogEventArgs : EventArgs
{
    public LogEntry Entry { get; }
    
    public LogEventArgs(LogEntry entry)
    {
        Entry = entry;
    }
}