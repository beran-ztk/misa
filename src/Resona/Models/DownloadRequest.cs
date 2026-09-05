using System.Collections.Generic;

namespace Resona.Models;

public class DownloadRequest
{
    public required string RawUrl { get; init; }
    public List<int> GenreIds { get; init; } = [];
    public int? RatingId { get; init; }
    public List<int> StyleIds { get; init; } = [];
    public bool IsOriginal { get; init; } = true;
    public int? ParentTrackId { get; init; }
    public TrackEditTypes EditTypes { get; init; }
    public string? VersionName { get; init; }
}
