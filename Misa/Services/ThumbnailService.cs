using System;
using System.IO;

namespace Misa.Services;

public static class ThumbnailService
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Misa", "thumbcache");

    // Returns the cache path if a thumbnail exists there, null otherwise.
    public static string? GetCachedPath(int trackId)
    {
        var path = Path.Combine(CacheDir, $"{trackId}.jpg");
        return File.Exists(path) ? path : null;
    }

    // Extracts the first embedded picture from the audio file into the cache.
    // Returns the cache path on success, null if no picture or on any error.
    public static string? EnsureCached(int trackId, string audioFilePath)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var cachePath = Path.Combine(CacheDir, $"{trackId}.jpg");

            if (File.Exists(cachePath))
                return cachePath;

            if (!File.Exists(audioFilePath))
                return null;

            using var tfile = TagLib.File.Create(audioFilePath);
            var pictures = tfile.Tag.Pictures;
            if (pictures.Length == 0)
                return null;

            File.WriteAllBytes(cachePath, pictures[0].Data.Data);
            return cachePath;
        }
        catch
        {
            return null;
        }
    }
}
