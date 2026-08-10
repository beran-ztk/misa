using System;
using System.Collections.Generic;
using System.IO;

namespace Resona.Services;

public static class ExternalToolLocator
{
    public static string Resolve(string toolName, string? toolsDirectory = null)
    {
        if (TryResolve(toolName, out var path, toolsDirectory))
            return path;

        var executableName = ExecutableName(toolName);
        return toolsDirectory is null
            ? executableName
            : Path.Combine(toolsDirectory, executableName);
    }

    public static bool TryResolve(
        string toolName,
        out string path,
        string? toolsDirectory = null)
    {
        var executableName = ExecutableName(toolName);
        var localPath = Path.Combine(toolsDirectory ?? Values.ToolsDirectory, executableName);
        if (File.Exists(localPath))
        {
            path = Path.GetFullPath(localPath);
            return true;
        }

        if (toolsDirectory is null)
        {
            foreach (var directory in PathDirectories())
            {
                var candidate = Path.Combine(directory, executableName);
                if (!File.Exists(candidate))
                    continue;

                path = Path.GetFullPath(candidate);
                return true;
            }
        }

        path = executableName;
        return false;
    }

    private static string ExecutableName(string toolName)
    {
        var name = Path.GetFileNameWithoutExtension(toolName.Trim());
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name is required.", nameof(toolName));
        return OperatingSystem.IsWindows() ? name + ".exe" : name;
    }

    private static IEnumerable<string> PathDirectories()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var directory = entry.Trim().Trim('"');
            if (directory.Length > 0 && Path.IsPathFullyQualified(directory))
                yield return directory;
        }
    }
}
