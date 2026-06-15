using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Misa.Music.Models;

namespace Misa.Music.Services;

public class MusicDatabase
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public MusicDatabase(string dbPath)
    {
        _dbPath = dbPath;
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var conn = Open();

        if (!TableExists(conn, "Music"))
        {
            CreateSchema(conn);
            SeedRatings(conn);
        }

        Migrate(conn);
    }

    private static bool TableExists(SqliteConnection conn, string table)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$t";
        cmd.Parameters.AddWithValue("$t", table);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    private static bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static void CreateSchema(SqliteConnection conn)
    {
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
                Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                CanonicalUrl       TEXT    NOT NULL UNIQUE,
                Title              TEXT    NOT NULL,
                FileName           TEXT    NOT NULL UNIQUE,
                RatingId           INTEGER NOT NULL,
                DownloadedAt       TEXT    NOT NULL,
                DurationSeconds    INTEGER NULL,
                Notes              TEXT    NULL,
                ReEvaluationNeeded INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE MusicStyles (
                MusicId INTEGER NOT NULL,
                StyleId INTEGER NOT NULL,
                PRIMARY KEY (MusicId, StyleId)
            );
            CREATE TABLE MusicGenres (
                MusicId INTEGER NOT NULL,
                GenreId INTEGER NOT NULL,
                PRIMARY KEY (MusicId, GenreId)
            );";
        cmd.ExecuteNonQuery();
    }

    private static void Migrate(SqliteConnection conn)
    {
        // Legacy: Notes column (pre-multi-genre era)
        if (!ColumnExists(conn, "Music", "Notes"))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Music ADD COLUMN Notes TEXT NULL";
            cmd.ExecuteNonQuery();
        }

        // Multi-genre migration: GenreId column on Music → MusicGenres junction table.
        // Also rebuilds Music table to drop GenreId and add ReEvaluationNeeded.
        if (ColumnExists(conn, "Music", "GenreId"))
        {
            using var tx = conn.BeginTransaction();

            // Create MusicGenres junction table (may not exist yet)
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS MusicGenres (
                    MusicId INTEGER NOT NULL,
                    GenreId INTEGER NOT NULL,
                    PRIMARY KEY (MusicId, GenreId)
                )";
                cmd.ExecuteNonQuery();
            }

            // Copy existing single-genre assignments into the junction table
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO MusicGenres (MusicId, GenreId) SELECT Id, GenreId FROM Music";
                cmd.ExecuteNonQuery();
            }

            // Rebuild Music without GenreId, adding ReEvaluationNeeded
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"CREATE TABLE Music_New (
                    Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                    CanonicalUrl       TEXT    NOT NULL UNIQUE,
                    Title              TEXT    NOT NULL,
                    FileName           TEXT    NOT NULL UNIQUE,
                    RatingId           INTEGER NOT NULL,
                    DownloadedAt       TEXT    NOT NULL,
                    DurationSeconds    INTEGER NULL,
                    Notes              TEXT    NULL,
                    ReEvaluationNeeded INTEGER NOT NULL DEFAULT 0
                )";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Music_New (Id, CanonicalUrl, Title, FileName, RatingId, DownloadedAt, DurationSeconds, Notes)
                    SELECT Id, CanonicalUrl, Title, FileName, RatingId, DownloadedAt, DurationSeconds, Notes FROM Music";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "DROP TABLE Music";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "ALTER TABLE Music_New RENAME TO Music";
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        else if (!ColumnExists(conn, "Music", "ReEvaluationNeeded"))
        {
            // Already on multi-genre schema but column missing (shouldn't normally happen)
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Music ADD COLUMN ReEvaluationNeeded INTEGER NOT NULL DEFAULT 0";
            cmd.ExecuteNonQuery();
        }

        // Ensure MusicGenres exists for databases that skipped the GenreId migration path
        if (!TableExists(conn, "MusicGenres"))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE MusicGenres (
                MusicId INTEGER NOT NULL,
                GenreId INTEGER NOT NULL,
                PRIMARY KEY (MusicId, GenreId)
            )";
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedRatings(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Ratings (Name, SortOrder) VALUES
            ('Skip', 1), ('Okay', 2), ('Good', 3), ('Great', 4), ('Timeless', 5)";
        cmd.ExecuteNonQuery();
    }

    public bool TrackExists(string canonicalUrl)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Music WHERE CanonicalUrl = $url";
        cmd.Parameters.AddWithValue("$url", canonicalUrl);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public void InsertTrack(string canonicalUrl, string title, string fileName,
                            List<int> genreIds, int ratingId, List<int> styleIds, int? durationSeconds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = tx;
        insertCmd.CommandText = @"
            INSERT INTO Music (CanonicalUrl, Title, FileName, RatingId, DownloadedAt, DurationSeconds)
            VALUES ($url, $title, $fileName, $ratingId, $downloadedAt, $duration)";
        insertCmd.Parameters.AddWithValue("$url", canonicalUrl);
        insertCmd.Parameters.AddWithValue("$title", title);
        insertCmd.Parameters.AddWithValue("$fileName", fileName);
        insertCmd.Parameters.AddWithValue("$ratingId", ratingId);
        insertCmd.Parameters.AddWithValue("$downloadedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        insertCmd.Parameters.AddWithValue("$duration", durationSeconds.HasValue ? (object)durationSeconds.Value : DBNull.Value);
        insertCmd.ExecuteNonQuery();

        using var idCmd = conn.CreateCommand();
        idCmd.Transaction = tx;
        idCmd.CommandText = "SELECT last_insert_rowid()";
        var musicId = (long)idCmd.ExecuteScalar()!;

        if (genreIds.Count > 0)
        {
            using var genreCmd = conn.CreateCommand();
            genreCmd.Transaction = tx;
            genreCmd.CommandText = "INSERT INTO MusicGenres (MusicId, GenreId) VALUES ($mid, $gid)";
            genreCmd.Parameters.Add("$mid", SqliteType.Integer).Value = musicId;
            var gidParam = genreCmd.Parameters.Add("$gid", SqliteType.Integer);
            foreach (var gid in genreIds)
            {
                gidParam.Value = gid;
                genreCmd.ExecuteNonQuery();
            }
        }

        if (styleIds.Count > 0)
        {
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

    public List<MusicTrack> GetAllTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, CanonicalUrl, Title, FileName, RatingId, DownloadedAt, DurationSeconds, Notes, ReEvaluationNeeded FROM Music ORDER BY DownloadedAt DESC";
        using var r = cmd.ExecuteReader();
        var list = new List<MusicTrack>();
        while (r.Read())
            list.Add(new MusicTrack(
                r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
                r.GetInt32(4), r.GetString(5),
                r.IsDBNull(6) ? null : r.GetInt32(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                r.GetInt32(8) != 0));
        return list;
    }

    public Dictionary<int, List<int>> GetAllMusicStyleIds()
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

    public List<int> GetMusicStyleIds(int musicId)
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

    public Dictionary<int, List<int>> GetAllMusicGenreIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MusicId, GenreId FROM MusicGenres";
        using var r = cmd.ExecuteReader();
        var dict = new Dictionary<int, List<int>>();
        while (r.Read())
        {
            var musicId = r.GetInt32(0);
            var genreId = r.GetInt32(1);
            if (!dict.TryGetValue(musicId, out var ids))
            {
                ids = [];
                dict[musicId] = ids;
            }
            ids.Add(genreId);
        }
        return dict;
    }

    public List<int> GetMusicGenreIds(int musicId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT GenreId FROM MusicGenres WHERE MusicId = $id";
        cmd.Parameters.AddWithValue("$id", musicId);
        using var r = cmd.ExecuteReader();
        var ids = new List<int>();
        while (r.Read()) ids.Add(r.GetInt32(0));
        return ids;
    }

    public void UpdateTrack(int id, string title, List<int> genreIds, int ratingId, List<int> styleIds, string? notes, bool reEvaluationNeeded)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Music SET Title = $title, RatingId = $ratingId, Notes = $notes, ReEvaluationNeeded = $reEval WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$ratingId", ratingId);
            cmd.Parameters.AddWithValue("$notes", notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$reEval", reEvaluationNeeded ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM MusicGenres WHERE MusicId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        if (genreIds.Count > 0)
        {
            using var genreCmd = conn.CreateCommand();
            genreCmd.Transaction = tx;
            genreCmd.CommandText = "INSERT INTO MusicGenres (MusicId, GenreId) VALUES ($mid, $gid)";
            genreCmd.Parameters.Add("$mid", SqliteType.Integer).Value = id;
            var gidParam = genreCmd.Parameters.Add("$gid", SqliteType.Integer);
            foreach (var gid in genreIds)
            {
                gidParam.Value = gid;
                genreCmd.ExecuteNonQuery();
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM MusicStyles WHERE MusicId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
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

    public void DeleteTrack(int id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM MusicGenres WHERE MusicId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM MusicStyles WHERE MusicId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Music WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public List<Genre> GetGenres()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Genres ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var list = new List<Genre>();
        while (r.Read()) list.Add(new Genre(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    public void InsertGenre(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Genres (Name) VALUES ($name)";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public bool IsGenreInUse(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM MusicGenres WHERE GenreId = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public void UpdateGenre(int id, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Genres SET Name = $name WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public void DeleteGenre(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Genres WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<Style> GetStyles()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Styles ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var list = new List<Style>();
        while (r.Read()) list.Add(new Style(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    public void InsertStyle(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Styles (Name) VALUES ($name)";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public bool IsStyleInUse(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM MusicStyles WHERE StyleId = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public void UpdateStyle(int id, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Styles SET Name = $name WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public void DeleteStyle(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Styles WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<Rating> GetRatings()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, SortOrder FROM Ratings ORDER BY SortOrder";
        using var r = cmd.ExecuteReader();
        var list = new List<Rating>();
        while (r.Read()) list.Add(new Rating(r.GetInt32(0), r.GetString(1), r.GetInt32(2)));
        return list;
    }

    public void InsertRating(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Ratings (Name, SortOrder)
            VALUES ($name, (SELECT COALESCE(MAX(SortOrder), 0) + 1 FROM Ratings))";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public bool IsRatingInUse(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Music WHERE RatingId = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public void UpdateRating(int id, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Ratings SET Name = $name WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    public void DeleteRating(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Ratings WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
