using System;
using System.Collections.Generic;
using System.IO;
using Music.Models;

namespace Music;

public static class Values
{
    public static readonly string SolutionDirectory = FindSolutionDirectory();
    public static readonly string LocalDirectory = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".private"), "music");
    public static readonly string TracksDirectory = Path.Combine(LocalDirectory, "tracks");
    public static readonly string DbPath = Path.Combine(LocalDirectory, "music.db");
    public static readonly string FilterPresetsPath = Path.Combine(LocalDirectory, "filter-presets.json");
    public static readonly string ToolsDirectory = Path.Combine(SolutionDirectory, "Tools");

    public static float Volume = 1f;
    public const int CrossfadeDurationSeconds = 10;
    public const int ManualFadeDurationSeconds = 2;
    
    public static List<Genre> Genres = [];
    public static List<Style> Styles = [];
    public static List<Rating> Ratings = [];

    private static string FindSolutionDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Music.sln")))
                return directory.FullName;
        }

        return AppContext.BaseDirectory;
    }
}
