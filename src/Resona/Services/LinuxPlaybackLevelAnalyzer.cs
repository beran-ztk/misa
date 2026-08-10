#if !WINDOWS
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Resona.Services;

// libVLC's decoded-audio callbacks replace its normal audio output. To keep libVLC
// in charge of the device, FFmpeg builds a lightweight level timeline in parallel.
internal static class LinuxPlaybackLevelAnalyzer
{
    private const int SampleRate = 44_100;
    private const int FramesPerSecond = 30;
    private const int WindowSamples = SampleRate / FramesPerSecond;
    private static readonly PlaybackAudioLevel[] Empty = [];
    private static readonly ConcurrentDictionary<string, Task<PlaybackAudioLevel[]>> Cache =
        new(StringComparer.Ordinal);

    public static Task<PlaybackAudioLevel[]> Start(string filePath)
    {
        var key = Path.GetFullPath(filePath);
        return Cache.GetOrAdd(key, AnalyzeAsync);
    }

    public static PlaybackAudioLevel At(
        Task<PlaybackAudioLevel[]> analysis,
        TimeSpan position,
        double amplitude)
    {
        if (!analysis.IsCompletedSuccessfully)
            return new PlaybackAudioLevel(0, 0, 0);

        var levels = analysis.Result;
        if (levels.Length == 0)
            return new PlaybackAudioLevel(0, 0, 0);

        var index = Math.Clamp(
            (int)(Math.Max(0, position.TotalSeconds) * FramesPerSecond),
            0,
            levels.Length - 1);
        var level = levels[index];
        var spectrum = new float[AudioSpectrumAnalyzer.BandCount];
        if (level.Spectrum is not null)
        {
            var count = Math.Min(spectrum.Length, level.Spectrum.Count);
            for (var band = 0; band < count; band++)
                spectrum[band] = (float)Math.Clamp(level.Spectrum[band] * amplitude, 0, 1);
        }
        return new PlaybackAudioLevel(
            Math.Clamp(level.Energy * amplitude, 0, 1),
            Math.Clamp(level.Bass * amplitude, 0, 1),
            Math.Clamp(level.Treble * amplitude, 0, 1),
            spectrum);
    }

    private static async Task<PlaybackAudioLevel[]> AnalyzeAsync(string filePath)
    {
        if (!File.Exists(filePath)
            || !ExternalToolLocator.TryResolve("ffmpeg", out var ffmpegPath))
        {
            return Empty;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("error");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(filePath);
            startInfo.ArgumentList.Add("-vn");
            startInfo.ArgumentList.Add("-ac");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-ar");
            startInfo.ArgumentList.Add(SampleRate.ToString());
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("f32le");
            startInfo.ArgumentList.Add("-");

            using var process = Process.Start(startInfo);
            if (process is null)
                return Empty;

            var stderrTask = process.StandardError.ReadToEndAsync();
            var levels = await ReadLevelsAsync(process.StandardOutput.BaseStream);
            await process.WaitForExitAsync();
            await stderrTask;
            return process.ExitCode == 0 ? levels : Empty;
        }
        catch
        {
            // Playback must remain available even when FFmpeg is missing or rejects a file.
            return Empty;
        }
    }

    private static async Task<PlaybackAudioLevel[]> ReadLevelsAsync(Stream stream)
    {
        var levels = new List<PlaybackAudioLevel>();
        var buffer = new byte[64 * 1024 + 3];
        var carriedBytes = 0;
        var bassAlpha = LowPassAlpha(180);
        var trebleLowPassAlpha = LowPassAlpha(2400);
        double bassSample = 0;
        double trebleLowPassSample = 0;
        double energySum = 0;
        double bassSum = 0;
        double trebleSum = 0;
        var windowSamples = 0;
        var spectrumAnalyzer = new AudioSpectrumAnalyzer(SampleRate);

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(carriedBytes, buffer.Length - carriedBytes));
            if (read == 0)
                break;

            var availableBytes = carriedBytes + read;
            var completeBytes = availableBytes - availableBytes % sizeof(float);
            for (var offset = 0; offset < completeBytes; offset += sizeof(float))
            {
                var mono = BitConverter.ToSingle(buffer, offset);
                if (!float.IsFinite(mono))
                    mono = 0;

                spectrumAnalyzer.AddSample(mono);

                bassSample += bassAlpha * (mono - bassSample);
                trebleLowPassSample += trebleLowPassAlpha * (mono - trebleLowPassSample);
                var trebleSample = mono - trebleLowPassSample;
                energySum += mono * mono;
                bassSum += bassSample * bassSample;
                trebleSum += trebleSample * trebleSample;
                windowSamples++;

                if (windowSamples < WindowSamples)
                    continue;

                levels.Add(CreateLevel(
                    energySum, bassSum, trebleSum, windowSamples, spectrumAnalyzer.LatestSpectrum));
                energySum = 0;
                bassSum = 0;
                trebleSum = 0;
                windowSamples = 0;
            }

            carriedBytes = availableBytes - completeBytes;
            if (carriedBytes > 0)
                Buffer.BlockCopy(buffer, completeBytes, buffer, 0, carriedBytes);
        }

        if (windowSamples > 0)
            levels.Add(CreateLevel(
                energySum, bassSum, trebleSum, windowSamples, spectrumAnalyzer.LatestSpectrum));
        return levels.ToArray();
    }

    private static PlaybackAudioLevel CreateLevel(
        double energySum,
        double bassSum,
        double trebleSum,
        int samples,
        IReadOnlyList<float> spectrum) =>
        new(
            Normalize(Math.Sqrt(energySum / samples), 3.5),
            Normalize(Math.Sqrt(bassSum / samples), 7.0),
            Normalize(Math.Sqrt(trebleSum / samples), 5.0),
            spectrum);

    private static double Normalize(double value, double gain) =>
        Math.Clamp(value * gain, 0, 1);

    private static double LowPassAlpha(double cutoffHz)
    {
        var dt = 1.0 / SampleRate;
        var rc = 1.0 / (2.0 * Math.PI * cutoffHz);
        return dt / (rc + dt);
    }
}
#endif
