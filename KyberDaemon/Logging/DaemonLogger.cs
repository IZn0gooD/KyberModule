using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using KyberDaemon.Configuration;

namespace KyberDaemon.Logging;

public sealed class DaemonLogger
{
    private readonly object _lock = new();
    private readonly LogFileSink _fileSink = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public void Configure(DaemonConfiguration configuration)
    {
        if (configuration is null)
        {
            return;
        }

        try
        {
            _fileSink.Configure(
                configuration.LogDirectory,
                configuration.LogFileName,
                configuration.LogMaxFileSizeMb,
                configuration.LogRetentionCount);

            Info("DaemonLogger", $"Rotation de logs active: {Path.Combine(configuration.LogDirectory, configuration.LogFileName)} (max {configuration.LogMaxFileSizeMb} MB, {configuration.LogRetentionCount} fichiers)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[KyberDaemon] Impossible de configurer la journalisation fichier: {ex.Message}");
        }
    }

    public void Info(string source, string message) => Write("INFO", source, message);
    public void Warning(string source, string message) => Write("WARN", source, message);
    public void Error(string source, string message, Exception? ex = null) => Write("ERROR", source, message, ex);
    public void Fatal(string source, string message, Exception? ex = null) => Write("FATAL", source, message, ex);

    private void Write(string level, string source, string message, Exception? ex = null)
    {
        lock (_lock)
        {
            var utcNow = DateTime.UtcNow;
            var processId = Environment.ProcessId;
            var threadId = Environment.CurrentManagedThreadId;
            var exceptionText = ex?.ToString();
            var formatted = exceptionText is null ? message : $"{message} | {exceptionText}";

            var entry = new LogEntry
            {
                Timestamp = utcNow,
                Level = level,
                Source = source,
                Message = message,
                Exception = exceptionText,
                ProcessId = processId,
                ThreadId = threadId
            };

            var json = JsonSerializer.Serialize(entry, _jsonOptions);

            Console.Out.WriteLine(json);
            _fileSink.Write(json);

            if (OperatingSystem.IsWindows())
            {
                WriteWindowsEventLog(level, source, formatted);
            }
            else if (OperatingSystem.IsLinux())
            {
                WriteSyslog(level, source, formatted);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void WriteWindowsEventLog(string level, string source, string message)
    {
        try
        {
            const string eventSource = "KyberDaemon";
            if (!EventLog.SourceExists(eventSource))
            {
                EventLog.CreateEventSource(eventSource, "Application");
            }

            using var eventLog = new EventLog("Application") { Source = eventSource };
            var entryType = level switch
            {
                "FATAL" => EventLogEntryType.Error,
                "ERROR" => EventLogEntryType.Error,
                "WARN" => EventLogEntryType.Warning,
                _ => EventLogEntryType.Information
            };
            eventLog.WriteEntry($"{source}: {message}", entryType);
        }
        catch
        {
            // Ignore event log errors (missing privileges, inaccessible registry, etc.)
        }
    }

    [SupportedOSPlatform("linux")]
    private static void WriteSyslog(string level, string source, string message)
    {
        try
        {
            SyslogInterop.write_syslog(level, source, message);
        }
        catch
        {
            // Best effort logging
        }
    }

    private readonly struct LogEntry
    {
        public DateTime Timestamp { get; init; }
        public string Level { get; init; }
        public string Source { get; init; }
        public string Message { get; init; }
        public string? Exception { get; init; }
        public int ProcessId { get; init; }
        public int ThreadId { get; init; }
    }

    private static class SyslogInterop
    {
        // Equivalent minimum interop for syslog via libc
        [DllImport("libc")] private static extern void openlog(string ident, int option, int facility);
        [DllImport("libc")] private static extern void syslog(int priority, string format, string message);
        [DllImport("libc")] private static extern void closelog();

        [SupportedOSPlatform("linux")]
        public static void write_syslog(string level, string source, string message)
        {
            const int LOG_PID = 0x01;
            const int LOG_CONS = 0x02;
            const int LOG_USER = 0x08 << 3;

            openlog("KyberDaemon", LOG_PID | LOG_CONS, LOG_USER);
            var priority = level switch
            {
                "FATAL" => 2, // LOG_CRIT
                "ERROR" => 3, // LOG_ERR
                "WARN" => 4,  // LOG_WARNING
                _ => 6         // LOG_INFO
            };
            syslog(priority, "%s", $"{source}: {message}");
            closelog();
        }
    }
}
