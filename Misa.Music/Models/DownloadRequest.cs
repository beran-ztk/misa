using System.Collections.Generic;

namespace Misa.Music.Models;

public class DownloadRequest
{
    public required string RawUrl { get; init; }
    public string? CustomTitle { get; init; }
    public required int GenreId { get; init; }
    public required int RatingId { get; init; }
    public List<int> StyleIds { get; init; } = [];
}
