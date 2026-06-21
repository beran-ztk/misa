using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Music.Models;
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

    public async Task<YouTubeTrackMetadata?> GetMetadataAsync(string url)
    {
        try
        {
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                "--js-runtimes", "node",
                "--no-playlist",
                "--skip-download",
                "--dump-single-json",
                url);
            if (result.ExitCode != 0) return null;

            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            string? Value(string key) => root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() : null;
            var uploadDate = Value("upload_date");
            if (uploadDate?.Length == 8 && DateTime.TryParseExact(uploadDate, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                uploadDate = date.ToString("yyyy-MM-dd");
            return new YouTubeTrackMetadata(
                Value("title"), Value("channel_id"), Value("channel"), Value("channel_url"), uploadDate,
                EstimatedAudioSize(root), DurationSeconds(root));
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<YouTubePlaylistEntry>> GetPlaylistEntriesAsync(string url)
    {
        try
        {
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                "--js-runtimes", "node",
                "--flat-playlist",
                "--dump-json",
                "--no-warnings",
                url);
            if (result.ExitCode != 0) return [];

            var entries = new List<YouTubePlaylistEntry>();
            foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (!root.TryGetProperty("id", out var idValue) || idValue.ValueKind != JsonValueKind.String) continue;
                var id = idValue.GetString();
                if (string.IsNullOrWhiteSpace(id)) continue;
                var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(id);
                var title = root.TryGetProperty("title", out var titleValue) && titleValue.ValueKind == JsonValueKind.String
                    ? titleValue.GetString() ?? id
                    : id;
                var duration = DurationSeconds(root);
                entries.Add(new YouTubePlaylistEntry(url, canonicalUrl, title, duration));
            }
            return entries;
        }
        catch
        {
            return [];
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

    private static long? EstimatedAudioSize(JsonElement root)
    {
        if (!root.TryGetProperty("formats", out var formats) || formats.ValueKind != JsonValueKind.Array)
            return null;

        var candidates = formats.EnumerateArray()
            .Where(format => Value(format, "acodec") is not "none"
                             && Value(format, "vcodec") == "none")
            .Select(format => new
            {
                Bitrate = Number(format, "abr") ?? 0,
                Size = Integer(format, "filesize") ?? Integer(format, "filesize_approx")
            })
            .Where(format => format.Size is > 0)
            .OrderByDescending(format => format.Bitrate)
            .FirstOrDefault();
        return candidates?.Size;

        static string? Value(JsonElement element, string key) => element.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        static double? Number(JsonElement element, string key) => element.TryGetProperty(key, out var value)
            && value.TryGetDouble(out var number) ? number : null;
        static long? Integer(JsonElement element, string key) => element.TryGetProperty(key, out var value)
            && value.TryGetInt64(out var number) ? number : null;
    }

    private static int? DurationSeconds(JsonElement root) => root.TryGetProperty("duration", out var value)
        && value.TryGetDouble(out var seconds) && seconds > 0
            ? (int)Math.Round(seconds)
            : null;

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
