using System;
using System.Collections.Generic;
using System.IO;
using Music.Models;

namespace Music;

public static class Values
{
    public static readonly string LocalDirectory = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".private"), "music");
    public static readonly string ThumbnailDirectory = Path.Combine(LocalDirectory, "thumbnails");
    public static readonly string TracksDirectory = Path.Combine(LocalDirectory, "tracks");
    public static readonly string DbPath = Path.Combine(LocalDirectory, "music.db");
    public static readonly string ToolsDirectory = @"D:\media\tools";

    public static float Volume = 100f;
    public const int CrossfadeDurationSeconds = 10;
    public const int ManualFadeDurationSeconds = 2;
    
    public static List<Genre> Genres = [];
    public static List<Style> Styles = [];
    public static List<Rating> Ratings = [];
}
