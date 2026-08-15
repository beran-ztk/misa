using System;
using System.IO;
using System.Security.Cryptography;
using Avalonia.Platform;
using Resona.Models;

namespace Resona.Services;

internal static class MprisArtworkCache
{
    private static readonly Uri FallbackArtworkUri = new("avares://Resona/Assets/headphones.png");

    public static string? GetArtworkUri(MusicTrack track, string audioFilePath)
    {
        if (!OperatingSystem.IsLinux())
            return null;

        try
        {
            var source = ThumbnailService.ReadEmbeddedArtwork(audioFilePath)
                         ?? track.Thumbnail
                         ?? MusicLibraryService.Current.GetTrackThumbnail(track.Id)
                         ?? ReadFallbackArtwork();
            if (source is not { Length: > 0 })
                return null;

            var artwork = ThumbnailService.CreateBoundedArtwork(
                              source,
                              ThumbnailService.PlayerArtworkMaxSize,
                              quality: 88)
                          ?? source;
            var hash = Convert.ToHexString(SHA256.HashData(artwork)).ToLowerInvariant()[..16];
            var directory = GetCacheDirectory();
            Directory.CreateDirectory(directory);
            var artworkPath = Path.Combine(directory, $"track-{track.Id}-{hash}.jpg");
            if (!File.Exists(artworkPath))
                File.WriteAllBytes(artworkPath, artwork);

            return new Uri(Path.GetFullPath(artworkPath)).AbsoluteUri;
        }
        catch
        {
            // Artwork is supplementary. MPRIS playback controls must remain available if caching fails.
            return null;
        }
    }

    private static byte[]? ReadFallbackArtwork()
    {
        try
        {
            using var stream = AssetLoader.Open(FallbackArtworkUri);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static string GetCacheDirectory()
    {
        var xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        var cacheRoot = !string.IsNullOrWhiteSpace(xdgCache) && Path.IsPathFullyQualified(xdgCache)
            ? xdgCache
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".cache");
        return Path.Combine(cacheRoot, "resona", "mpris-artwork");
    }
}
