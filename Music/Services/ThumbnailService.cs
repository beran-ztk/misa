using System;
using System.IO;

namespace Music.Services;

public static class ThumbnailService
{
    public sealed record EmbeddedArtwork(byte[] Data, string Extension);

    public static byte[]? ReadEmbeddedArtwork(string audioFilePath)
        => ReadEmbeddedArtworkFile(audioFilePath)?.Data;

    public static EmbeddedArtwork? ReadEmbeddedArtworkFile(string audioFilePath)
    {
        try
        {
            if (!File.Exists(audioFilePath))
                return null;

            using var tfile = TagLib.File.Create(audioFilePath);
            var pictures = tfile.Tag.Pictures;
            if (pictures.Length == 0)
                return null;

            var picture = pictures[0];
            return new EmbeddedArtwork(picture.Data.Data, ExtensionFor(picture.MimeType, picture.Data.Data));
        }
        catch
        {
            return null;
        }
    }

    private static string ExtensionFor(string? mimeType, byte[] data)
    {
        if (mimeType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true)
            return ".png";

        if (mimeType?.Contains("webp", StringComparison.OrdinalIgnoreCase) == true)
            return ".webp";

        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return ".png";

        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return ".webp";

        return ".jpg";
    }
}
