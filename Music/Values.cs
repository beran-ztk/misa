using System;
using System.IO;

namespace Music;

public static class Values
{
    public static readonly string LocalDirectory = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".private"), "music");
    public static readonly string ThumbnailDirectory = Path.Combine(LocalDirectory, "thumbnails");
    public static readonly string TracksDirectory = Path.Combine(LocalDirectory, "tracks");
    public static readonly string DbPath = Path.Combine(LocalDirectory, "music.db");
    public static readonly string ToolsDirectory = @"D:\media\tools";

    public static float Volume = 100f;
}