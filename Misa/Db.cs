using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Misa.Models;

namespace Misa;

static class Db
{
    public const string DbPath = @"D:\media\music\music.db";
    private static readonly string ConnectionString = $"Data Source={DbPath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(@"D:\media\music");
        
        if (File.Exists(DbPath)) return;

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Genres (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT    NOT NULL UNIQUE
            );
            CREATE TABLE Styles (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT    NOT NULL UNIQUE
            );
            CREATE TABLE Ratings (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT    NOT NULL UNIQUE,
                SortOrder INTEGER NOT NULL
            );
            CREATE TABLE Music (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                CanonicalUrl    TEXT    NOT NULL UNIQUE,
                Title           TEXT    NOT NULL,
                FileName        TEXT    NOT NULL UNIQUE,
                GenreId         INTEGER NOT NULL,
                RatingId        INTEGER NOT NULL,
                DownloadedAt    TEXT    NOT NULL,
                DurationSeconds INTEGER NULL
            );
            CREATE TABLE MusicStyles (
                MusicId INTEGER NOT NULL,
                StyleId INTEGER NOT NULL,
                PRIMARY KEY (MusicId, StyleId)
            );";
        cmd.ExecuteNonQuery();

        SeedRatings(conn);
    }

    private static void SeedRatings(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Ratings (Name, SortOrder) VALUES
            ('Skip', 1), ('Okay', 2), ('Good', 3), ('Great', 4), ('Timeless', 5)";
        cmd.ExecuteNonQuery();
    }

    public static bool TrackExists(string canonicalUrl)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Music WHERE CanonicalUrl = $url";
        cmd.Parameters.AddWithValue("$url", canonicalUrl);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public static void InsertTrack(string canonicalUrl, string title, string fileName,
                                   int genreId, int ratingId, List<int> styleIds, int? durationSeconds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = tx;
        insertCmd.CommandText = @"
            INSERT INTO Music (CanonicalUrl, Title, FileName, GenreId, RatingId, DownloadedAt, DurationSeconds)
            VALUES ($url, $title, $fileName, $genreId, $ratingId, $downloadedAt, $duration)";
        insertCmd.Parameters.AddWithValue("$url", canonicalUrl);
        insertCmd.Parameters.AddWithValue("$title", title);
        insertCmd.Parameters.AddWithValue("$fileName", fileName);
        insertCmd.Parameters.AddWithValue("$genreId", genreId);
        insertCmd.Parameters.AddWithValue("$ratingId", ratingId);
        insertCmd.Parameters.AddWithValue("$downloadedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        insertCmd.Parameters.AddWithValue("$duration", durationSeconds.HasValue ? (object)durationSeconds.Value : DBNull.Value);
        insertCmd.ExecuteNonQuery();

        if (styleIds.Count > 0)
        {
            using var idCmd = conn.CreateCommand();
            idCmd.Transaction = tx;
            idCmd.CommandText = "SELECT last_insert_rowid()";
            var musicId = (long)idCmd.ExecuteScalar()!;

            using var styleCmd = conn.CreateCommand();
            styleCmd.Transaction = tx;
            styleCmd.CommandText = "INSERT INTO MusicStyles (MusicId, StyleId) VALUES ($mid, $sid)";
            styleCmd.Parameters.Add("$mid", SqliteType.Integer).Value = musicId;
            var sidParam = styleCmd.Parameters.Add("$sid", SqliteType.Integer);
            foreach (var sid in styleIds)
            {
                sidParam.Value = sid;
                styleCmd.ExecuteNonQuery();
            }
        }

        tx.Commit();
    }

    public static List<MusicTrack> GetAllTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CanonicalUrl, Title, FileName, GenreId, RatingId, DownloadedAt, DurationSeconds FROM Music ORDER BY DownloadedAt DESC";
        using var r = cmd.ExecuteReader();
        var list = new List<MusicTrack>();
        while (r.Read())
            list.Add(new MusicTrack(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
                                    r.GetInt32(4), r.GetInt32(5), r.GetString(6),
                                    r.IsDBNull(7) ? null : r.GetInt32(7)));
        return list;
    }

    public static Dictionary<int, List<int>> GetAllMusicStyleIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MusicId, StyleId FROM MusicStyles";
        using var r = cmd.ExecuteReader();
        var dict = new Dictionary<int, List<int>>();
        while (r.Read())
        {
            var musicId = r.GetInt32(0);
            var styleId = r.GetInt32(1);
            if (!dict.TryGetValue(musicId, out var ids))
            {
                ids = [];
                dict[musicId] = ids;
            }
            ids.Add(styleId);
        }
        return dict;
    }

    public static List<Genre> GetGenres()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Genres ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var list = new List<Genre>();
        while (r.Read()) list.Add(new Genre(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    public static void InsertGenre(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Genres (Name) VALUES ($name)";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public static List<Style> GetStyles()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Styles ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var list = new List<Style>();
        while (r.Read()) list.Add(new Style(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    public static void InsertStyle(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Styles (Name) VALUES ($name)";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public static List<Rating> GetRatings()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, SortOrder FROM Ratings ORDER BY SortOrder";
        using var r = cmd.ExecuteReader();
        var list = new List<Rating>();
        while (r.Read()) list.Add(new Rating(r.GetInt32(0), r.GetString(1), r.GetInt32(2)));
        return list;
    }

    public static List<int> GetMusicStyleIds(int musicId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT StyleId FROM MusicStyles WHERE MusicId = $id";
        cmd.Parameters.AddWithValue("$id", musicId);
        using var r = cmd.ExecuteReader();
        var ids = new List<int>();
        while (r.Read()) ids.Add(r.GetInt32(0));
        return ids;
    }

    public static void UpdateTrack(int id, string title, int genreId, int ratingId, List<int> styleIds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using var updateCmd = conn.CreateCommand();
        updateCmd.Transaction = tx;
        updateCmd.CommandText = "UPDATE Music SET Title = $title, GenreId = $genreId, RatingId = $ratingId WHERE Id = $id";
        updateCmd.Parameters.AddWithValue("$id", id);
        updateCmd.Parameters.AddWithValue("$title", title);
        updateCmd.Parameters.AddWithValue("$genreId", genreId);
        updateCmd.Parameters.AddWithValue("$ratingId", ratingId);
        updateCmd.ExecuteNonQuery();

        using var delStylesCmd = conn.CreateCommand();
        delStylesCmd.Transaction = tx;
        delStylesCmd.CommandText = "DELETE FROM MusicStyles WHERE MusicId = $id";
        delStylesCmd.Parameters.AddWithValue("$id", id);
        delStylesCmd.ExecuteNonQuery();

        if (styleIds.Count > 0)
        {
            using var styleCmd = conn.CreateCommand();
            styleCmd.Transaction = tx;
            styleCmd.CommandText = "INSERT INTO MusicStyles (MusicId, StyleId) VALUES ($mid, $sid)";
            styleCmd.Parameters.Add("$mid", SqliteType.Integer).Value = id;
            var sidParam = styleCmd.Parameters.Add("$sid", SqliteType.Integer);
            foreach (var sid in styleIds)
            {
                sidParam.Value = sid;
                styleCmd.ExecuteNonQuery();
            }
        }

        tx.Commit();
    }

    public static void DeleteTrack(int id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using var delStylesCmd = conn.CreateCommand();
        delStylesCmd.Transaction = tx;
        delStylesCmd.CommandText = "DELETE FROM MusicStyles WHERE MusicId = $id";
        delStylesCmd.Parameters.AddWithValue("$id", id);
        delStylesCmd.ExecuteNonQuery();

        using var delCmd = conn.CreateCommand();
        delCmd.Transaction = tx;
        delCmd.CommandText = "DELETE FROM Music WHERE Id = $id";
        delCmd.Parameters.AddWithValue("$id", id);
        delCmd.ExecuteNonQuery();

        tx.Commit();
    }

    private static SqliteConnection Open()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
