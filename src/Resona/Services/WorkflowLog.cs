using System;
using System.IO;

namespace Resona.Services;

/// <summary>A small, dependency-free diagnostic log for background workflow transitions.</summary>
public static class WorkflowLog
{
    private const long MaximumBytes = 2 * 1024 * 1024;
    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(Values.LocalDirectory, "workflow.log");
    private static readonly string PreviousLogPath = Path.Combine(Values.LocalDirectory, "workflow.previous.log");

    public static void Info(string area, string message) => Write("INFO", area, message);

    public static void Error(string area, string message, Exception? exception = null) =>
        Write("ERROR", area, exception is null ? message : $"{message} · {exception.GetType().Name}: {exception.Message}");

    private static void Write(string level, string area, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Values.LocalDirectory);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length >= MaximumBytes)
                    File.Move(LogPath, PreviousLogPath, overwrite: true);

                File.AppendAllText(
                    LogPath,
                    $"{DateTime.UtcNow:O}\t{level}\t{area}\t{message.ReplaceLineEndings(" ")}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never change application behavior.
        }
    }
}
