using System;
using System.IO;

namespace Music.Services;

public static class ThumbnailService
{
    // Extracts the first embedded picture from the audio file into the cache.
    // Returns the cache path on success, null if no picture or on any error.
    public static string? EnsureCached(int trackId, string audioFilePath)
    {
        try
        {
            Directory.CreateDirectory(Values.ThumbnailDirectory);
            var cachePath = Path.Combine(Values.ThumbnailDirectory, $"{trackId}.jpg");

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
