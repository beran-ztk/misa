using System;

namespace Resona.Services;

public static class YouTubeUrlNormalizer
{
    public static string? ExtractVideoId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        if (uri.Host is "youtu.be" or "www.youtu.be")
            return uri.AbsolutePath.TrimStart('/').Split('?')[0];

        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq] == "v")
                return Uri.UnescapeDataString(part[(eq + 1)..]);
        }

        return null;
    }

    public static string GetCanonicalUrl(string videoId) =>
        $"https://www.youtube.com/watch?v={videoId}";

    public static YouTubeImportSource? ParseImportSource(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsYouTubeHost(uri.Host))
            return null;

        var videoId = ExtractVideoId(url);
        if (videoId is { Length: 11 })
            return new YouTubeImportSource(GetCanonicalUrl(videoId), YouTubeImportSourceKind.SingleTrack);

        if (!uri.AbsolutePath.TrimEnd('/').Equals("/playlist", StringComparison.OrdinalIgnoreCase))
            return null;

        var playlistId = QueryValue(uri, "list");
        if (string.IsNullOrWhiteSpace(playlistId)
            || playlistId.StartsWith("RD", StringComparison.OrdinalIgnoreCase))
            return null;

        return new YouTubeImportSource(
            $"https://www.youtube.com/playlist?list={Uri.EscapeDataString(playlistId)}",
            YouTubeImportSourceKind.Playlist);
    }

    private static bool IsYouTubeHost(string host) =>
        host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".youtu.be", StringComparison.OrdinalIgnoreCase)
        || host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);

    private static string? QueryValue(Uri uri, string name)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = part.IndexOf('=');
            if (equals > 0 && part[..equals].Equals(name, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(part[(equals + 1)..]);
        }

        return null;
    }
}

public enum YouTubeImportSourceKind
{
    SingleTrack,
    Playlist
}

public readonly record struct YouTubeImportSource(string Url, YouTubeImportSourceKind Kind);
