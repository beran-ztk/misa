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
    public async Task<(bool Success, string ErrorOutput)> RunYtDlpAsync(string url)
    {
        Directory.CreateDirectory(Values.TracksDirectory);
        var outputTemplate = Path.Combine(Values.TracksDirectory, "%(title)s [%(id)s].%(ext)s");

        var result = await RunProcessAsync(
            Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
            "--js-runtimes", "node",
            "--no-playlist",
            "-f", "bestaudio[ext=m4a]/bestaudio/best[height<=360]/18",
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

    public async Task<(IReadOnlyList<YouTubePlaylistEntry> Entries, string? Error)> GetPlaylistEntriesAsync(
        string url, CancellationToken cancellationToken = default)
    {
        try
        {
            string? lastError = null;
            foreach (var extractionUrl in PlaylistExtractionUrls(url))
            {
                var result = await RunProcessAsync(
                    Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                    [
                        "--js-runtimes", "node",
                        "--ignore-errors",
                        "--flat-playlist",
                        "--dump-json",
                        "--no-warnings",
                        extractionUrl
                    ],
                    cancellationToken);

                var entries = ParsePlaylistEntries(url, result.Output);
                if (entries.Count > 0) return (entries, null);

                lastError = CleanYtDlpError(result.Error);
            }

            return ([], lastError);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { return ([], exception.Message); }
    }

    public async Task<(YouTubeChannelSnapshot? Snapshot, string? Error)> GetChannelSnapshotAsync(
        string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await RunProcessAsync(
                Path.Combine(Values.ToolsDirectory, "yt-dlp.exe"),
                [
                    "--js-runtimes", "node",
                    "--ignore-errors",
                    "--flat-playlist",
                    "--dump-single-json",
                    "--no-warnings",
                    NormalizeChannelVideosUrl(url)
                ],
                cancellationToken);

            if (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.Output))
                return (null, CleanYtDlpError(result.Error));

            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            var name = StringValue(root, "channel")
                       ?? StringValue(root, "uploader")
                       ?? StringValue(root, "title")
                       ?? "YouTube channel";
            var channelUrl = StringValue(root, "channel_url") ?? StringValue(root, "uploader_url") ?? url;
            var sourceUrl = StringValue(root, "webpage_url") ?? url;
            var videos = new List<YouTubeChannelVideoEntry>();
            if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entries.EnumerateArray())
                    AddChannelVideoEntry(entry, videos);
            }

            return (new YouTubeChannelSnapshot(
                sourceUrl,
                StringValue(root, "channel_id") ?? StringValue(root, "uploader_id"),
                name,
                channelUrl,
                videos), null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { return (null, exception.Message); }
    }

    private static List<YouTubePlaylistEntry> ParsePlaylistEntries(string sourceUrl, string output)
    {
        var entries = new List<YouTubePlaylistEntry>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            JsonDocument document;
            try { document = JsonDocument.Parse(line); }
            catch (JsonException) { continue; }
            using (document)
            {
                var root = document.RootElement;
                AddPlaylistEntry(sourceUrl, root, entries);
                if (root.TryGetProperty("entries", out var childEntries) && childEntries.ValueKind == JsonValueKind.Array)
                {
                    foreach (var child in childEntries.EnumerateArray())
                        AddPlaylistEntry(sourceUrl, child, entries);
                }
            }
        }
        return entries;
    }

    private static void AddPlaylistEntry(string sourceUrl, JsonElement root, List<YouTubePlaylistEntry> entries)
    {
        if (IsUnavailablePlaylistEntry(root)) return;

        var id = StringValue(root, "id");
        if (string.IsNullOrWhiteSpace(id) || id.Length != 11)
            id = YouTubeUrlNormalizer.ExtractVideoId(StringValue(root, "url") ?? string.Empty);
        if (string.IsNullOrWhiteSpace(id)) return;

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(id);
        if (entries.Any(entry => entry.CanonicalUrl.Equals(canonicalUrl, StringComparison.OrdinalIgnoreCase)))
            return;

        var title = StringValue(root, "title") ?? id;
        var duration = DurationSeconds(root);
        entries.Add(new YouTubePlaylistEntry(sourceUrl, canonicalUrl, title, duration));
    }

    private static void AddChannelVideoEntry(JsonElement root, List<YouTubeChannelVideoEntry> entries)
    {
        if (IsUnavailablePlaylistEntry(root)) return;

        var id = StringValue(root, "id");
        if (string.IsNullOrWhiteSpace(id) || id.Length != 11)
            id = YouTubeUrlNormalizer.ExtractVideoId(StringValue(root, "url") ?? string.Empty);
        if (string.IsNullOrWhiteSpace(id)) return;

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(id);
        if (entries.Any(entry => entry.CanonicalUrl.Equals(canonicalUrl, StringComparison.OrdinalIgnoreCase)))
            return;

        entries.Add(new YouTubeChannelVideoEntry(
            id,
            canonicalUrl,
            StringValue(root, "title") ?? id,
            DurationSeconds(root),
            NormalizeUploadDate(StringValue(root, "upload_date"))));
    }

    private static bool IsGeneratedMix(string url)
    {
        return QueryValue(url, "start_radio") == "1";
    }

    private static bool IsUnavailablePlaylistEntry(JsonElement root)
    {
        var title = StringValue(root, "title");
        var availability = StringValue(root, "availability");
        var liveStatus = StringValue(root, "live_status");

        return title is "[Deleted video]" or "[Private video]"
               || availability is "private" or "premium_only" or "subscriber_only"
               || liveStatus == "is_upcoming";
    }

    private static IEnumerable<string> PlaylistExtractionUrls(string url)
    {
        var listValue = QueryValue(url, "list");
        if (string.IsNullOrWhiteSpace(listValue) || IsGeneratedMix(url))
        {
            yield return url;
            yield break;
        }

        var playlistUrl = $"https://www.youtube.com/playlist?list={Uri.EscapeDataString(listValue)}";
        yield return playlistUrl;
        if (!url.Equals(playlistUrl, StringComparison.OrdinalIgnoreCase))
            yield return url;
    }

    private static string? CleanYtDlpError(string error)
    {
        var line = error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            ?? error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();

        if (string.IsNullOrWhiteSpace(line)) return null;
        return line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)
            ? line["ERROR:".Length..].Trim()
            : line.Trim();
    }

    private static string NormalizeChannelVideosUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith("/videos", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.Contains("/playlist", StringComparison.OrdinalIgnoreCase))
            return url;

        return uri.AbsolutePath.TrimEnd('/').Length > 0
            ? $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath.TrimEnd('/')}/videos"
            : url;
    }

    private static string? NormalizeUploadDate(string? value)
    {
        if (value?.Length == 8 && DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return date.ToString("yyyy-MM-dd");
        return value;
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
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out var number) ? number : null;
        static long? Integer(JsonElement element, string key) => element.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var number) ? number : null;
    }

    private static int? DurationSeconds(JsonElement root) => root.TryGetProperty("duration", out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetDouble(out var seconds) && seconds > 0
            ? (int)Math.Round(seconds)
            : null;

    private static string? StringValue(JsonElement root, string key) => root.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
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
