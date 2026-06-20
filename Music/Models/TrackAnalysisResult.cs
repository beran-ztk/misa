using System.Collections.Generic;

namespace Music.Models;

public record TrackGenrePrediction(string ModelGenre, string ModelSubgenre, double Score);

public record TrackAnalysisResult(
    string AnalyzerName,
    IReadOnlyList<TrackGenrePrediction> Predictions,
    double? Bpm,
    double? IntegratedLoudness,
    double? LoudnessRange);
