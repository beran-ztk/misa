using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Resona.Models;

namespace Resona.Cloud.Server;

public sealed partial class CloudDownloadWorker(
    CloudDownloadRepository jobs,
    CloudMediaRepository media,
    CloudDeviceLibraryRepository libraries,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<CloudDownloadWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("EnableServerDownloads", true))
            return;
        await RetryRepositoryAsync(
            token => jobs.RecoverInterruptedAsync(token),
            "recover interrupted downloads",
            stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ClaimedCloudDownload? job;
            try
            {
                job = await jobs.ClaimAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not claim a server download; retrying.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }
            if (job is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            try
            {
                await ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Server download {JobId} failed.", job.JobId);
                await jobs.SetStateAsync(
                    job.JobId, "Failed", 100, null, null, FriendlyError(exception), stoppingToken);
            }
        }
    }

    private async Task RetryRepositoryAsync(
        Func<CancellationToken, Task> action,
        string operation,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await action(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not {Operation}; retrying.", operation);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ProcessAsync(ClaimedCloudDownload job, CancellationToken cancellationToken)
    {
        var trackKey = TrackKey(job.Request.Url)
                       ?? throw new InvalidDataException("Only individual YouTube video links are supported.");
        var workDirectory = Path.Combine(Path.GetTempPath(), "resona-download-" + job.JobId.ToString("N"));
        Directory.CreateDirectory(workDirectory);
        try
        {
            var metadataJson = await RunAsync(
                "yt-dlp",
                ["--no-playlist", "--skip-download", "--dump-single-json", job.Request.Url],
                workDirectory,
                cancellationToken);
            using var metadata = JsonDocument.Parse(metadataJson);
            var root = metadata.RootElement;
            var title = String(root, "title") ?? trackKey;
            await jobs.SetStateAsync(job.JobId, "Downloading", 20, trackKey, title, null, cancellationToken);

            await RunAsync(
                "yt-dlp",
                ["--no-playlist", "-f", "bestaudio[ext=m4a]/bestaudio",
                 "-x", "--audio-format", "m4a", "--embed-thumbnail",
                 "-o", Path.Combine(workDirectory, "%(id)s.%(ext)s"), job.Request.Url],
                workDirectory,
                cancellationToken);
            var path = Directory.EnumerateFiles(workDirectory, trackKey + ".*")
                .FirstOrDefault(file => Path.GetExtension(file).Equals(".m4a", StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException("yt-dlp completed without creating an M4A file.");

            await jobs.SetStateAsync(job.JobId, "Analyzing", 65, trackKey, title, null, cancellationToken);
            var analysis = await AnalyzeAsync(path, cancellationToken);
            var fileName = trackKey + ".m4a";
            await using (var stream = File.OpenRead(path))
                await media.StoreAsync(
                    job.UserId, trackKey, fileName, "audio/mp4", stream, stream.Length, null, cancellationToken);

            var thumbnail = await DownloadThumbnailAsync(String(root, "thumbnail"), cancellationToken);
            var genres = AnalysisGenres(analysis);
            var emotional = EmotionalCharacter(analysis);
            var track = new CloudDeviceTrack(
                TrackKey: trackKey,
                FileName: fileName,
                Title: title,
                OriginalTitle: title,
                Artist: String(root, "artist") ?? String(root, "channel") ?? String(root, "uploader"),
                Remix: job.Request.VersionName,
                DurationSeconds: Integer(root, "duration"),
                Rating: job.Request.Rating,
                RatingBand: null,
                Genres: job.Request.Genres?.Count > 0 ? job.Request.Genres : genres,
                Styles: job.Request.Styles ?? [],
                Tags: [],
                LanguageCode: null,
                NeedsReview: job.Request.Rating is null,
                LibraryState: job.Request.Rating is null ? "PendingRating" : "Active",
                Thumbnail: thumbnail,
                PlayCount: 0,
                ListenedSeconds: 0,
                SkipCount: 0,
                LastListenedAt: null,
                Analysis: new CloudPublicTrackAnalysis(
                    Number(analysis.RootElement, "bpm"),
                    Number(analysis.RootElement, "integratedLoudness"),
                    Number(analysis.RootElement, "loudnessRange")),
                EmotionalCharacter: emotional,
                UpdatedAt: DateTimeOffset.UtcNow.ToString("O"),
                IsOriginal: job.Request.IsOriginal,
                ParentTrackKey: job.Request.ParentTrackKey,
                EditTypes: job.Request.EditTypes,
                CanonicalUrl: $"https://www.youtube.com/watch?v={trackKey}");
            if (await libraries.AddDownloadedTrackAsync(job.UserId, track, cancellationToken) is null)
                throw new InvalidOperationException("The server library must be synchronized once before adding downloads.");

            await jobs.SetStateAsync(job.JobId, "Completed", 100, trackKey, title, null, cancellationToken);
        }
        finally
        {
            try { Directory.Delete(workDirectory, recursive: true); }
            catch { }
        }
    }

    private async Task<JsonDocument> AnalyzeAsync(string path, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("analyzer");
        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(path);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mp4");
        content.Add(fileContent, "file", Path.GetFileName(path));
        using var request = new HttpRequestMessage(HttpMethod.Post, "analyze") { Content = content };
        if (configuration["AnalyzerApiKey"] is { Length: > 0 } apiKey)
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var result = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(result, cancellationToken: cancellationToken);
    }

    private async Task<byte[]?> DownloadThumbnailAsync(string? url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;
        try
        {
            var bytes = await httpClientFactory.CreateClient().GetByteArrayAsync(uri, cancellationToken);
            return bytes.Length <= 5_000_000 ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> AnalysisGenres(JsonDocument analysis)
    {
        if (!analysis.RootElement.TryGetProperty("predictions", out var predictions))
            return [];
        return predictions.EnumerateArray()
            .Where(value => Number(value, "score") is > 0.25)
            .Select(value => String(value, "label"))
            .Where(value => !string.IsNullOrWhiteSpace(value) && value.Contains("---", StringComparison.Ordinal))
            .Select(value => value!.Replace("---", " → ", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, double> EmotionalCharacter(JsonDocument analysis)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (!analysis.RootElement.TryGetProperty("experimentalPredictions", out var models))
            return result;
        foreach (var model in models.EnumerateArray())
        {
            if (!string.Equals(String(model, "model"), "moods mirex", StringComparison.OrdinalIgnoreCase)
                || !model.TryGetProperty("values", out var values))
                continue;
            foreach (var value in values.EnumerateArray())
            {
                var label = String(value, "label");
                var score = Number(value, "score");
                if (!string.IsNullOrWhiteSpace(label) && score is double number)
                    result[label] = number;
            }
        }
        return result;
    }

    private static async Task<string> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)
                            ?? throw new InvalidOperationException($"Could not start {executable}.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await output;
        var stderr = await error;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{executable} failed: {stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim()}");
        return stdout;
    }

    public static string? TrackKey(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (!uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
                && !uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
                && !uri.Host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase)))
            return null;
        var candidate = uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
            ? uri.AbsolutePath.Trim('/').Split('/')[0]
            : QueryValue(uri.Query, "v");
        return candidate is not null && VideoIdPattern().IsMatch(candidate) ? candidate : null;
    }

    private static string? QueryValue(string query, string key) => query.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2))
        .Where(parts => Uri.UnescapeDataString(parts[0]).Equals(key, StringComparison.OrdinalIgnoreCase))
        .Select(parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty)
        .FirstOrDefault();

    private static string? String(JsonElement element, string key) =>
        element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? Number(JsonElement element, string key) =>
        element.TryGetProperty(key, out var value) && value.TryGetDouble(out var number) ? number : null;

    private static int? Integer(JsonElement element, string key) =>
        Number(element, key) is double number ? (int)Math.Round(number) : null;

    private static string FriendlyError(Exception exception)
    {
        var message = exception.Message.Trim();
        return message.Length <= 1000 ? message : message[..1000];
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex VideoIdPattern();
}
