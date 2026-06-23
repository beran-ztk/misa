using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.Json;
using Music.Models;
using System.Threading.Tasks;

namespace Music.Services;

public class TrackDownloadService
{
    private const int GeneratedMixEntryLimit = 50;
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

    public async Task<YouTubeTrackMetadata?> GetMetadataAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                ["--js-runtimes", "node", "--no-playlist", "--skip-download", "--dump-single-json", url],
                cancellationToken);
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
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }

    public async Task<IReadOnlyList<YouTubePlaylistEntry>> GetPlaylistEntriesAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = new List<string> { "--js-runtimes", "node", "--flat-playlist", "--dump-json", "--no-warnings" };
            var playlistUrl = PlaylistExtractionUrl(url);
            // YouTube radio links carry start_radio=1. Limit only those generated radio queues;
            // ordinary playlists can also appear beside a watch URL and should stay unbounded.
            if (IsGeneratedMix(url))
            {
                arguments.Add("--playlist-end");
                arguments.Add(GeneratedMixEntryLimit.ToString(CultureInfo.InvariantCulture));
            }
            arguments.Add(playlistUrl);
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                arguments.ToArray(),
                cancellationToken);
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
        catch (OperationCanceledException) { throw; }
        catch { return []; }
    }

    private static bool IsGeneratedMix(string url)
    {
        return QueryValue(url, "start_radio") == "1";
    }

    private static string PlaylistExtractionUrl(string url)
    {
        var listValue = QueryValue(url, "list");
        return string.IsNullOrWhiteSpace(listValue) || IsGeneratedMix(url)
            ? url
            : $"https://www.youtube.com/playlist?list={Uri.EscapeDataString(listValue)}";
    }

    private static string? QueryValue(string url, string key)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length != 2 || !pieces[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            return Uri.UnescapeDataString(pieces[1]);
        }

        return null;
    }

    public string? FindDownloadedFile(string videoId)
    {
        return Directory.GetFiles(Values.TracksDirectory, "*.m4a")
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith($"[{videoId}]"));
    }

    public void DeleteDownloadArtifacts(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId) || !Directory.Exists(Values.TracksDirectory))
            return;

        foreach (var filePath in Directory.EnumerateFiles(Values.TracksDirectory))
        {
            var fileName = Path.GetFileName(filePath);
            if (!fileName.Contains($"[{videoId}]", StringComparison.OrdinalIgnoreCase))
                continue;

            try { File.Delete(filePath); }
            catch { }
        }
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

    private static async Task<ProcessResult> RunProcessAsync(string fileName, params string[] args) =>
        await RunProcessAsync(fileName, args, CancellationToken.None);

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string[] args, CancellationToken cancellationToken)
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
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync();
            throw;
        }

        return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
    }

    private record ProcessResult(int ExitCode, string Output, string Error);
}
