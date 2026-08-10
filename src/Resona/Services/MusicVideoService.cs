using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public interface IMusicVideoService
{
    Task CreateAsync(
        MusicVideoOptions options,
        IProgress<MusicVideoProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class MusicVideoService : IMusicVideoService
{
    public static MusicVideoService Current { get; } = new();

    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public MusicVideoService(string? toolsDirectory = null)
    {
        _ffmpegPath = ExternalToolLocator.Resolve("ffmpeg", toolsDirectory);
        _ffprobePath = ExternalToolLocator.Resolve("ffprobe", toolsDirectory);
    }

    public async Task CreateAsync(
        MusicVideoOptions options,
        IProgress<MusicVideoProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        EnsureToolExists(_ffmpegPath);
        EnsureToolExists(_ffprobePath);

        progress?.Report(new MusicVideoProgress(0, "Audiodauer wird gelesen …"));
        var duration = await ProbeDurationAsync(options.AudioPath, cancellationToken);
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath))
            ?? throw new InvalidOperationException("Der Zielordner ist ungültig.");
        Directory.CreateDirectory(outputDirectory);

        var partialPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileNameWithoutExtension(options.OutputPath)}.{Guid.NewGuid():N}.partial.mp4");
        var textFilePrefix = Path.Combine(outputDirectory, $".music-video-{Guid.NewGuid():N}");
        var titleTextPath = string.IsNullOrWhiteSpace(options.Title) ? null : textFilePrefix + ".title.txt";
        var subtitleTextPath = string.IsNullOrWhiteSpace(options.Subtitle) ? null : textFilePrefix + ".subtitle.txt";

        try
        {
            if (titleTextPath is not null)
                await File.WriteAllTextAsync(titleTextPath, options.Title, new UTF8Encoding(false), cancellationToken);
            if (subtitleTextPath is not null)
                await File.WriteAllTextAsync(subtitleTextPath, options.Subtitle, new UTF8Encoding(false), cancellationToken);

            var arguments = BuildArguments(options, duration, partialPath, titleTextPath, subtitleTextPath);
            progress?.Report(new MusicVideoProgress(0.01, "Video wird erstellt …"));
            await RunFfmpegAsync(arguments, duration, progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, Path.GetFullPath(options.OutputPath), true);
            progress?.Report(new MusicVideoProgress(1, "Video wurde erstellt."));
        }
        finally
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);
            if (titleTextPath is not null && File.Exists(titleTextPath))
                File.Delete(titleTextPath);
            if (subtitleTextPath is not null && File.Exists(subtitleTextPath))
                File.Delete(subtitleTextPath);
        }
    }

    internal static IReadOnlyList<string> BuildArguments(
        MusicVideoOptions options,
        TimeSpan duration,
        string outputPath,
        string? titleTextPath,
        string? subtitleTextPath)
    {
        return
        [
            "-hide_banner",
            "-y",
            "-loop", "1",
            "-i", options.ImagePath,
            "-i", options.AudioPath,
            "-filter_complex", BuildFilter(options, duration, titleTextPath, subtitleTextPath),
            "-map", "[v]",
            "-map", "1:a:0",
            "-c:v", "libx264",
            "-preset", "medium",
            "-tune", "stillimage",
            "-r", "30",
            "-pix_fmt", "yuv420p",
            "-c:a", "aac",
            "-b:a", "192k",
            "-movflags", "+faststart",
            "-shortest",
            "-progress", "pipe:1",
            "-nostats",
            outputPath
        ];
    }

    internal static string BuildFilter(
        MusicVideoOptions options,
        TimeSpan duration,
        string? titleTextPath,
        string? subtitleTextPath)
    {
        var width = options.Width;
        var height = options.Height;
        var minimumScale = options.ImageMode == MusicVideoImageMode.Crop ? 1 : 0.25;
        var scale = Number(Math.Clamp(options.ImageScale, minimumScale, 3));
        var offsetX = Number(Math.Clamp(options.ImagePositionX, -1, 1));
        var offsetY = Number(Math.Clamp(options.ImagePositionY, -1, 1));
        var backgroundBlur = Math.Clamp(options.BackgroundBlur, 0, 60);
        var backgroundDim = Math.Clamp(options.BackgroundDim, 0, 0.7);
        var graph = new StringBuilder();

        switch (options.ImageMode)
        {
            case MusicVideoImageMode.Crop:
                graph.Append($"[0:v]scale={width}*{scale}:{height}*{scale}:force_original_aspect_ratio=increase,");
                graph.Append($"crop={width}:{height}:");
                graph.Append($"x='max(0,min(iw-ow,(iw-ow)/2-{offsetX}*(iw-ow)/2))':");
                graph.Append($"y='max(0,min(ih-oh,(ih-oh)/2-{offsetY}*(ih-oh)/2))'[scene];");
                break;

            case MusicVideoImageMode.BlurredBackground:
                graph.Append("[0:v]split=2[bgsource][fgsource];");
                graph.Append($"[bgsource]scale={width}:{height}:force_original_aspect_ratio=increase,");
                graph.Append($"crop={width}:{height}:x='(iw-ow)/2':y='(ih-oh)/2'");
                if (backgroundBlur > 0)
                {
                    var blurRadius = Number(backgroundBlur);
                    var chromaRadius = Number(backgroundBlur / 2);
                    graph.Append($",boxblur=luma_radius={blurRadius}:luma_power=2:");
                    graph.Append($"chroma_radius={chromaRadius}:chroma_power=2");
                }
                if (backgroundDim > 0)
                    graph.Append($",drawbox=x=0:y=0:w=iw:h=ih:color=black@{Number(backgroundDim)}:t=fill");
                graph.Append("[background];");
                graph.Append($"[fgsource]scale={width}*{scale}:{height}*{scale}:force_original_aspect_ratio=decrease[foreground];");
                graph.Append($"[background][foreground]overlay=");
                graph.Append($"x='(W-w)/2+{offsetX}*W/2':y='(H-h)/2+{offsetY}*H/2'[scene];");
                break;

            default:
                graph.Append($"color=c=black:s={width}x{height}:r=30[background];");
                graph.Append($"[0:v]scale={width}*{scale}:{height}*{scale}:force_original_aspect_ratio=decrease[foreground];");
                graph.Append($"[background][foreground]overlay=");
                graph.Append($"x='(W-w)/2+{offsetX}*W/2':y='(H-h)/2+{offsetY}*H/2'[scene];");
                break;
        }

        var current = "scene";
        if (!string.IsNullOrWhiteSpace(options.Title))
        {
            if (titleTextPath is null)
                throw new ArgumentException("Für den Titel fehlt die temporäre Textdatei.", nameof(titleTextPath));
            var titleY = TextY(options.TextPositionY, options.Subtitle.Length > 0 ? -0.04 : 0);
            graph.Append($"[{current}]drawtext=textfile='{EscapeFilterPath(titleTextPath)}':");
            graph.Append($"fontcolor=white:fontsize={Math.Max(24, height * 6 / 100)}:");
            graph.Append("borderw=2:bordercolor=black@0.75:");
            graph.Append($"x='(w-text_w)*{Number(Math.Clamp(options.TextPositionX, 0, 1))}':");
            graph.Append($"y='(h-text_h)*{Number(titleY)}'[title];");
            current = "title";
        }

        if (!string.IsNullOrWhiteSpace(options.Subtitle))
        {
            if (subtitleTextPath is null)
                throw new ArgumentException("Für den Untertitel fehlt die temporäre Textdatei.", nameof(subtitleTextPath));
            var subtitleY = TextY(options.TextPositionY, 0.055);
            graph.Append($"[{current}]drawtext=textfile='{EscapeFilterPath(subtitleTextPath)}':");
            graph.Append($"fontcolor=white@0.88:fontsize={Math.Max(18, height * 35 / 1000)}:");
            graph.Append("borderw=2:bordercolor=black@0.7:");
            graph.Append($"x='(w-text_w)*{Number(Math.Clamp(options.TextPositionX, 0, 1))}':");
            graph.Append($"y='(h-text_h)*{Number(subtitleY)}'[subtitle];");
            current = "subtitle";
        }

        var frames = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds * 30));
        var amount = 0.02 + Math.Clamp(options.AnimationStrength, 0, 1) * 0.23;
        var amountText = Number(amount);
        switch (options.Animation)
        {
            case MusicVideoAnimation.ZoomIn:
                graph.Append($"[{current}]zoompan=z='1+{amountText}*on/{frames}':");
                graph.Append($"x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=1:s={width}x{height}:fps=30[v]");
                break;
            case MusicVideoAnimation.ZoomOut:
                graph.Append($"[{current}]zoompan=z='1+{amountText}*(1-on/{frames})':");
                graph.Append($"x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=1:s={width}x{height}:fps=30[v]");
                break;
            case MusicVideoAnimation.Pan:
                AppendPan(graph, current, options.AnimationDirection, width, height, frames, amountText);
                break;
            default:
                graph.Append($"[{current}]fps=30,format=yuv420p[v]");
                break;
        }

        return graph.ToString();
    }

    private static void AppendPan(
        StringBuilder graph,
        string input,
        MusicVideoAnimationDirection direction,
        int width,
        int height,
        int frames,
        string amount)
    {
        var progress = $"on/{frames}";
        var reverse = $"(1-on/{frames})";
        var x = direction switch
        {
            MusicVideoAnimationDirection.Left => $"(iw-iw/zoom)*{reverse}",
            MusicVideoAnimationDirection.Right => $"(iw-iw/zoom)*{progress}",
            _ => "iw/2-iw/zoom/2"
        };
        var y = direction switch
        {
            MusicVideoAnimationDirection.Up => $"(ih-ih/zoom)*{reverse}",
            MusicVideoAnimationDirection.Down => $"(ih-ih/zoom)*{progress}",
            _ => "ih/2-ih/zoom/2"
        };
        graph.Append($"[{input}]zoompan=z='1+{amount}':x='{x}':y='{y}':");
        graph.Append($"d=1:s={width}x{height}:fps=30[v]");
    }

    private async Task<TimeSpan> ProbeDurationAsync(string audioPath, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(
            _ffprobePath,
            ["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", audioPath],
            cancellationToken);
        if (result.ExitCode != 0 ||
            !double.TryParse(result.Output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
            !double.IsFinite(seconds) ||
            seconds <= 0)
        {
            throw new InvalidOperationException(
                $"Die Audiodauer konnte nicht gelesen werden. {LastErrorLine(result.Error)}".Trim());
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private async Task RunFfmpegAsync(
        IReadOnlyList<string> arguments,
        TimeSpan duration,
        IProgress<MusicVideoProgress>? progress,
        CancellationToken cancellationToken)
    {
        var psi = CreateProcessStartInfo(_ffmpegPath, arguments);
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("FFmpeg konnte nicht gestartet werden.");
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(true);
            }
            catch (InvalidOperationException) { }
        });

        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("out_time_us=", StringComparison.Ordinal) ||
                !long.TryParse(line.AsSpan("out_time_us=".Length), CultureInfo.InvariantCulture, out var microseconds))
                continue;

            var fraction = Math.Clamp(microseconds / 1_000_000d / duration.TotalSeconds, 0, 0.99);
            progress?.Report(new MusicVideoProgress(fraction, $"Video wird erstellt … {fraction:P0}"));
        }

        await process.WaitForExitAsync(cancellationToken);
        var error = await errorTask;
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg-Export fehlgeschlagen. {LastErrorLine(error)}".Trim());
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var psi = CreateProcessStartInfo(fileName, arguments);
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"{Path.GetFileName(fileName)} konnte nicht gestartet werden.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(true);
            await process.WaitForExitAsync();
            throw;
        }

        return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);
        return psi;
    }

    private static void Validate(MusicVideoOptions options)
    {
        if (!File.Exists(options.AudioPath))
            throw new FileNotFoundException("Die Audiodatei wurde nicht gefunden.", options.AudioPath);
        if (!File.Exists(options.ImagePath))
            throw new FileNotFoundException("Die Bilddatei wurde nicht gefunden.", options.ImagePath);
        if (string.IsNullOrWhiteSpace(options.OutputPath))
            throw new ArgumentException("Bitte einen Zielpfad auswählen.", nameof(options));
        if (options.Width < 320 || options.Height < 240 || options.Width % 2 != 0 || options.Height % 2 != 0)
            throw new ArgumentException("Breite und Höhe müssen gerade Zahlen und mindestens 320 × 240 sein.", nameof(options));
        if (!string.Equals(Path.GetExtension(options.OutputPath), ".mp4", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Der Zieldateiname muss die Endung .mp4 haben.", nameof(options));
    }

    private static void EnsureToolExists(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Das benötigte Werkzeug wurde nicht gefunden: {path}", path);
    }

    private static double TextY(double value, double offset) => Math.Clamp(value + offset, 0, 0.96);
    private static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string EscapeFilterPath(string path) => Path.GetFullPath(path)
        .Replace("\\", "/", StringComparison.Ordinal)
        .Replace(":", "\\:", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal);

    private static string LastErrorLine(string error)
    {
        var lines = error.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return string.Empty;
        var start = Math.Max(0, lines.Length - 6);
        return string.Join(" ", lines[start..]).Trim();
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
