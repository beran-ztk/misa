namespace Music.Models;

public record ImportResult(bool Success, MusicTrack? Track = null, string? Error = null);
