using System.IO;

namespace Music.Services;

public static class ThumbnailService
{
    public static byte[]? ReadEmbeddedArtwork(string audioFilePath)
    {
        try
        {
            if (!File.Exists(audioFilePath))
                return null;

            using var tfile = TagLib.File.Create(audioFilePath);
            var pictures = tfile.Tag.Pictures;
            if (pictures.Length == 0)
                return null;

            return pictures[0].Data.Data;
        }
        catch
        {
            return null;
        }
    }
}
