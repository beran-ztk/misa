using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Music.Models;

namespace Music.Services;

public sealed class TrackAnalysisService
{
    private const string ScriptFileName = "analyze.py";
    private static readonly HashSet<string> ExcludedExperimentalModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "mtg_" + "jamen" + "do_" + "mood" + "theme"
    };
    private const string ModelDirectoryInContainer = "/models/Essentia/DiscogsMAEST";
    private const string ScriptsDirectoryInContainer = "/scripts";
    private const string TracksDirectoryInContainer = "/tracks";

    public async Task<(TrackAnalysisResult? Result, string? Error)> AnalyzeAsync(string audioFilePath)
    {
        var scriptPath = Path.Combine(Values.ScriptsDirectory, ScriptFileName);
        if (!File.Exists(scriptPath))
            return (null, $"Analysis script not found: {scriptPath}");

        var modelDirectory = Path.Combine(Values.ModelsDirectory, "Essentia", "DiscogsMAEST");
        if (!Directory.Exists(modelDirectory))
            return (null, $"Analysis model directory not found: {modelDirectory}");

        if (!File.Exists(audioFilePath))
            return (null, $"Downloaded audio file not found: {audioFilePath}");

        var containerTrackPath = $"{TracksDirectoryInContainer}/{Path.GetFileName(audioFilePath)}";
        var result = await RunDockerAsync(
            "run", "--rm",
            "-v", $"{Values.ScriptsDirectory}:{ScriptsDirectoryInContainer}:ro",
            "-v", $"{Values.ModelsDirectory}:/models:ro",
            "-v", $"{Values.TracksDirectory}:{TracksDirectoryInContainer}:ro",
            Values.AnalysisDockerImage,
            "python3", $"{ScriptsDirectoryInContainer}/{ScriptFileName}", containerTrackPath,
            "--model-directory", ModelDirectoryInContainer,
            "--top", "20",
            "--include-experimental");

        if (result.ExitCode != 0)
            return (null, ExtractError(result.Output, result.Error));

        try
        {
            var output = JsonSerializer.Deserialize<ScriptOutput>(result.Output,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (output is null)
                return (null, "Analysis did not return JSON output.");
            if (!output.Success)
                return (null, output.Error ?? "Analysis failed without an error message.");

            var predictions = (output.Predictions ?? [])
                .Select(prediction => TryCreatePrediction(prediction.Label, prediction.Score))
                .Where(prediction => prediction is not null)
                .Cast<TrackGenrePrediction>()
                .ToList();
            if (predictions.Count == 0)
                return (null, "Analysis completed but returned no valid genre predictions.");

            return (new TrackAnalysisResult(
                output.Model ?? "discogs-maest-30s-pw-519l-2",
                predictions,
                output.Bpm,
                output.IntegratedLoudness,
                output.LoudnessRange,
                ParseExperimentalModels(output)), null);
        }
        catch (JsonException exception)
        {
            return (null, $"Could not parse analysis JSON: {exception.Message}");
        }
    }

    private static TrackGenrePrediction? TryCreatePrediction(string? label, double score)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var separator = label.IndexOf("---", StringComparison.Ordinal);
        if (separator <= 0 || separator == label.Length - 3) return null;

        return new TrackGenrePrediction(
            label[..separator],
            label[(separator + 3)..],
            score);
    }

    private static List<ExperimentalAnalysisModel> ParseExperimentalModels(ScriptOutput? output)
    {
        if (output is null || !output.Success)
            return [];

        return (output.ExperimentalPredictions ?? [])
            .Where(model => !string.IsNullOrWhiteSpace(model.Model))
            .Where(model => !ExcludedExperimentalModels.Contains(model.Model!))
            .Select(model => new ExperimentalAnalysisModel(
                model.Family ?? "unknown",
                model.Category ?? "Other",
                model.Model!,
                model.Type ?? "classifier",
                model.Description ?? string.Empty,
                (model.Values ?? [])
                    .Where(value => !string.IsNullOrWhiteSpace(value.Label))
                    .Select(value => new ExperimentalAnalysisValue(value.Label!, value.Score))
                    .ToList()))
            .ToList();
    }

    private static async Task<ProcessResult> RunDockerAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
                return new ProcessResult(-1, string.Empty, "Could not start Docker.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
        }
        catch (Exception exception)
        {
            return new ProcessResult(-1, string.Empty, $"Could not start Docker: {exception.Message}");
        }
    }

    private static string ExtractError(string output, string error)
    {
        try
        {
            var scriptOutput = JsonSerializer.Deserialize<ScriptOutput>(output,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrWhiteSpace(scriptOutput?.Error)) return scriptOutput.Error;
        }
        catch (JsonException) { }

        return string.IsNullOrWhiteSpace(error) ? "Docker analysis failed." : error.Trim();
    }

    private sealed record ScriptOutput(
        bool Success,
        string? Error,
        string? Model,
        List<ScriptPrediction>? Predictions,
        double? Bpm,
        double? IntegratedLoudness,
        double? LoudnessRange,
        List<ScriptExperimentalModel>? ExperimentalPredictions);
    private sealed record ScriptPrediction(string? Label, double Score);
    private sealed record ScriptExperimentalModel(
        string? Family,
        string? Category,
        string? Model,
        string? Type,
        string? Description,
        List<ScriptExperimentalValue>? Values);
    private sealed record ScriptExperimentalValue(string? Label, double Score);
    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
