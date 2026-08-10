using System.Collections.Generic;

namespace Resona.Models;

public sealed class MusicAnalysisResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? TrackPath { get; set; }
    public string? Model { get; set; }
    public List<int> PredictionShape { get; set; } = [];
    public int? LabelCount { get; set; }
    public double? Bpm { get; set; }
    public double? IntegratedLoudness { get; set; }
    public double? LoudnessRange { get; set; }
    public List<PredictionScore> Predictions { get; set; } = [];
    public List<ExperimentalPrediction> ExperimentalPredictions { get; set; } = [];
    public List<ExperimentalAnalysisError> ExperimentalErrors { get; set; } = [];
}

public sealed class PredictionScore
{
    public string? Label { get; set; }
    public double Score { get; set; }
}

public sealed class ExperimentalPrediction
{
    public string? Family { get; set; }
    public string? Category { get; set; }
    public string? Model { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public List<PredictionScore> Values { get; set; } = [];
}

public sealed class ExperimentalAnalysisError
{
    public string? Family { get; set; }
    public string? Model { get; set; }
    public string? Error { get; set; }
}
