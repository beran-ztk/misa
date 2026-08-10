using System;
using System.IO;
using SkiaSharp;

namespace Resona.Services;

public static class ThumbnailService
{
    public const int ThumbnailSize = 128;
    public const int PlayerArtworkMaxSize = 768;

    public sealed record EmbeddedArtwork(byte[] Data, string Extension);

    public static byte[]? ReadEmbeddedArtwork(string audioFilePath)
        => ReadEmbeddedArtworkFile(audioFilePath)?.Data;

    public static byte[]? ReadEmbeddedArtworkThumbnail(string audioFilePath)
    {
        var artwork = ReadEmbeddedArtwork(audioFilePath);
        return artwork is { Length: > 0 }
            ? CreateSquareArtwork(artwork, ThumbnailSize, 82)
            : null;
    }

    public static byte[]? ReadEmbeddedPlayerArtwork(string audioFilePath)
    {
        var artwork = ReadEmbeddedArtwork(audioFilePath);
        return artwork is { Length: > 0 }
            ? CreateBoundedArtwork(artwork, PlayerArtworkMaxSize, 86)
            : null;
    }

    public static byte[]? CreateSquareArtwork(byte[] artwork, int size, int quality)
    {
        try
        {
            using var source = SKBitmap.Decode(artwork);
            if (source is null || source.Width <= 0 || source.Height <= 0)
                return null;

            using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var scale = Math.Max((float)size / source.Width, (float)size / source.Height);
            var width = source.Width * scale;
            var height = source.Height * scale;
            var left = (size - width) / 2f;
            var top = (size - height) / 2f;
            var destination = new SKRect(left, top, left + width, top + height);

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };
            canvas.DrawBitmap(source, destination, paint);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            return data?.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static byte[]? CreateBoundedArtwork(byte[] artwork, int maxSize, int quality)
    {
        try
        {
            using var source = SKBitmap.Decode(artwork);
            if (source is null || source.Width <= 0 || source.Height <= 0)
                return null;

            var scale = Math.Min(1f, (float)maxSize / Math.Max(source.Width, source.Height));
            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };
            canvas.DrawBitmap(source, new SKRect(0, 0, width, height), paint);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            return data?.ToArray();
        }
        catch
        {
            return null;
        }
    }

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
