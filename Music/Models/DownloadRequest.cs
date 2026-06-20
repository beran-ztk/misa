using System.Collections.Generic;

namespace Music.Models;

public class DownloadRequest
{
    public required string RawUrl { get; init; }
    public List<int> GenreIds { get; init; } = [];
    public int? RatingId { get; init; }
    public List<int> StyleIds { get; init; } = [];
}
