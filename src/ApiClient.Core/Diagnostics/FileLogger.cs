using System;
using System.IO;

namespace ApiClient.Core.Diagnostics;

/// <summary>
/// A tiny, dependency-free, thread-safe append-only logger. Writes timestamped lines to a
/// file; used to record exceptions and crashes so failures are diagnosable after the fact.
/// </summary>
public sealed class FileLogger
{
    private readonly object _gate = new();

    /// <summary>Creates a logger writing to <paramref name="path"/>, creating its directory if needed.</summary>
    public FileLogger(string path)
    {
        Path = path;
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    /// <summary>The log file path.</summary>
    public string Path { get; }

    /// <summary>The default per-user log file path.</summary>
    public static string DefaultPath() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ApiClient",
        "logs",
        "apiclient.log");

    /// <summary>Logs an informational message.</summary>
    public void Info(string message) => Write("INFO", message, null);

    /// <summary>Logs an error, optionally with an exception's details.</summary>
    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        if (exception is not null)
            line += Environment.NewLine + exception;

        lock (_gate)
        {
            try
            {
                File.AppendAllText(Path, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // Logging must never crash the app — swallow file errors.
            }
        }
    }
}
