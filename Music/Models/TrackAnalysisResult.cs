using System.Collections.Generic;

namespace Music.Models;

public record TrackGenrePrediction(string ModelGenre, string ModelSubgenre, double Score);
public record ExperimentalAnalysisValue(string Label, double Score);
public record ExperimentalAnalysisModel(
    string Family,
    string Category,
    string Model,
    string Type,
    string Description,
    IReadOnlyList<ExperimentalAnalysisValue> Values);

public record TrackAnalysisResult(
    string AnalyzerName,
    IReadOnlyList<TrackGenrePrediction> Predictions,
    double? Bpm,
    double? IntegratedLoudness,
    double? LoudnessRange,
    IReadOnlyList<ExperimentalAnalysisModel>? ExperimentalModels = null);

public record TrackAudioAnalysis(double? Bpm, double? IntegratedLoudness, double? LoudnessRange);
