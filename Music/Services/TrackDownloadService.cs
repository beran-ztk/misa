using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Music.Services;

public class TrackDownloadService
{
    public async Task<(bool Success, string ErrorOutput)> RunYtDlpAsync(string url)
    {
        Directory.CreateDirectory(Values.TracksDirectory);
        var outputTemplate = Path.Combine(Values.TracksDirectory, "%(title)s [%(id)s].%(ext)s");

        var result = await RunProcessAsync(
            Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
            "--js-runtimes", "node",
            "--no-playlist",
            "-x",
            "--audio-format", "m4a",
            "--embed-thumbnail",
            "--ffmpeg-location", Values.ToolsDirectory,
            "-o", outputTemplate,
            url);

        return (result.ExitCode == 0, result.Error);
    }

    public async Task<int?> GetDurationAsync(string filePath)
    {
        try
        {
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "ffprobe.exe"),
                "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                filePath);

            if (result.ExitCode == 0
                && double.TryParse(result.Output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
                return (int)seconds;
        }
        catch { }
        return null;
    }

    public async Task<string?> GetTitleAsync(string url)
    {
        try
        {
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                "--js-runtimes", "node",
                "--no-playlist",
                "--print", "title",
                "--skip-download",
                url);

            var title = result.Output
                .Split('\n')
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Length > 0);

            return result.ExitCode == 0 ? title : null;
        }
        catch
        {
            return null;
        }
    }

    public string? FindDownloadedFile(string videoId)
    {
        return Directory.GetFiles(Values.TracksDirectory, "*.m4a")
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith($"[{videoId}]"));
    }

    public string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var bracket = name.LastIndexOf('[');
        return bracket > 0 ? name[..bracket].Trim() : name;
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        if (process == null)
            return new ProcessResult(-1, "", $"Could not start {Path.GetFileName(fileName)}.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
    }

    private record ProcessResult(int ExitCode, string Output, string Error);
}
