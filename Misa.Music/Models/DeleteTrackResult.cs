namespace Misa.Music.Models;

public record DeleteTrackResult(bool FileDeleted, string? FileError = null);
