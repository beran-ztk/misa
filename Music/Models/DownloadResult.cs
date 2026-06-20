namespace Music.Models;

public record DownloadResult(bool Success, string? Error = null, string? Warning = null);
