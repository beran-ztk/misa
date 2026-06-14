using System;

namespace Misa.Music.Services;

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
}
