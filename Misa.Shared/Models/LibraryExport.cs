namespace Misa.Shared.Models;

public class LibraryExport
{
    public string ExportedAt { get; set; } = "";
    public int LibraryVersion { get; set; } = 1;
    public List<LibraryTrack> Tracks { get; set; } = [];
}

public class LibraryTrack
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string CanonicalUrl { get; set; } = "";
    public List<string> Genres { get; set; } = [];
    public string Rating { get; set; } = "";
    public List<string> Styles { get; set; } = [];
    public int? DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public bool ReEvaluationNeeded { get; set; }
}
