using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Music.Models;

namespace Music.Services;

public class TrackDownloadService
{
    private readonly string _toolsDir;
    private readonly string _musicDir;
    private readonly MusicSettings _settings;

    public TrackDownloadService(string toolsDir, string musicDir, MusicSettings settings)
    {
        _toolsDir = toolsDir;
        _musicDir = musicDir;
        _settings = settings;
    }

    public async Task<(bool Success, string ErrorOutput)> RunYtDlpAsync(string url)
    {
        var outputTemplate = Path.Combine(_musicDir, "%(title)s [%(id)s].%(ext)s");

        var args = new List<string>
        {
            "--js-runtimes node",
            "--no-playlist",
            "-x",
            "--audio-format m4a",
            "--embed-thumbnail",
            $"--ffmpeg-location \"{_toolsDir}\"",
            $"-o \"{outputTemplate}\"",
            $"\"{url}\""
        };

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(_toolsDir, "yt-dlp.exe"),
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stderr = await stderrTask;
        await stdoutTask;

        return (process.ExitCode == 0, stderr);
    }

    public async Task<int?> GetDurationAsync(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(_toolsDir, "ffprobe.exe"),
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
                return (int)seconds;
        }
        catch { }
        return null;
    }

    public string? FindDownloadedFile(string videoId)
    {
        return Directory.GetFiles(_musicDir, "*.m4a")
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith($"[{videoId}]"));
    }

    public string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var bracket = name.LastIndexOf('[');
        return bracket > 0 ? name[..bracket].Trim() : name;
    }
}
