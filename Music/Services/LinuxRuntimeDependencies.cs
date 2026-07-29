using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Music.Services;

public sealed record LinuxRuntimeDependency(
    string Name,
    bool IsAvailable,
    string Detail);

public static class LinuxRuntimeDependencies
{
    public static IReadOnlyList<LinuxRuntimeDependency> Inspect()
    {
        if (!OperatingSystem.IsLinux())
            return [];

        return
        [
            InspectLibVlc(),
            InspectTool("FFmpeg", "ffmpeg"),
            InspectTool("FFprobe", "ffprobe"),
            InspectTool("yt-dlp", "yt-dlp"),
            InspectTool("Node.js", "node")
        ];
    }

    private static LinuxRuntimeDependency InspectLibVlc()
    {
        foreach (var libraryName in new[] { "libvlc.so.5", "libvlc.so", "libvlc" })
        {
            if (!NativeLibrary.TryLoad(libraryName, out var handle))
                continue;

            NativeLibrary.Free(handle);
            return new LinuxRuntimeDependency("libVLC", true, libraryName);
        }

        return new LinuxRuntimeDependency(
            "libVLC",
            false,
            "not found · install the vlc package");
    }

    private static LinuxRuntimeDependency InspectTool(string displayName, string toolName)
    {
        var found = ExternalToolLocator.TryResolve(toolName, out var path);
        return new LinuxRuntimeDependency(
            displayName,
            found,
            found ? path : $"not found · install {toolName}");
    }
}
