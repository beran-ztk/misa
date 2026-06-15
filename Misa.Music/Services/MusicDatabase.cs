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
        }

        Migrate(conn);
        SeedDefaultMetadata(conn);
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
            CREATE TABLE Languages (
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
            );
            CREATE TABLE MusicLanguages (
                MusicId    INTEGER NOT NULL,
                LanguageId INTEGER NOT NULL,
                PRIMARY KEY (MusicId, LanguageId)
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

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO MusicGenres (MusicId, GenreId) SELECT Id, GenreId FROM Music";
                cmd.ExecuteNonQuery();
            }

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
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Music ADD COLUMN ReEvaluationNeeded INTEGER NOT NULL DEFAULT 0";
            cmd.ExecuteNonQuery();
        }

        // Ensure junction tables exist for databases that skipped earlier migration paths
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

        // Languages support (new — all existing songs start with no language)
        if (!TableExists(conn, "Languages"))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE Languages (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE)";
            cmd.ExecuteNonQuery();
        }
        if (!TableExists(conn, "MusicLanguages"))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE MusicLanguages (
                MusicId    INTEGER NOT NULL,
                LanguageId INTEGER NOT NULL,
                PRIMARY KEY (MusicId, LanguageId)
            )";
            cmd.ExecuteNonQuery();
        }
    }
    private static void SeedDefaultMetadata(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Ratings (Name, SortOrder) VALUES
                ('Skip', 1), ('Okay', 2), ('Good', 3), ('Great', 4), ('Favorite', 5);

            INSERT OR IGNORE INTO Genres (Name) VALUES
                ('Anime'), ('Pop'), ('Nightcore'), ('Techno'), ('Trance'),
                ('Hardstyle'), ('Frenchcore'), ('EDM'), ('House'), ('Synthwave'),
                ('Retrowave'), ('Darksynth'), ('Chillwave'), ('Phonk'), ('Trap'),
                ('Ambient'), ('Epic'), ('Classical'), ('Orchestral'), ('Piano');

            INSERT OR IGNORE INTO Styles (Name) VALUES
                ('Fast'), ('Slow'), ('Energetic'), ('Calm'), ('Hard'),
                ('Soft'), ('Dark'), ('Light'), ('Melodic'), ('Emotional'),
                ('Melancholic'), ('Romantic'), ('Epic'), ('Dramatic'), ('Powerful'),
                ('Ambient'), ('Wave'), ('Chill'), ('Vocal'), ('Instrumental'),
                ('Anime Opening'), ('Anime Ending'), ('Party'), ('Bossfight'),
                ('Speed Up'), ('Slowed'), ('Reverb');

            INSERT OR IGNORE INTO Languages (Name) VALUES
                ('English'), ('Japanese'), ('Turkish'), ('German'),
                ('Korean'), ('French'), ('Unknown');";
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
                            List<int> genreIds, int ratingId, List<int> styleIds,
                            List<int> languageIds, int? durationSeconds)
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

        InsertJunctionRows(conn, tx, "MusicGenres", "GenreId", musicId, genreIds);
        InsertJunctionRows(conn, tx, "MusicStyles", "StyleId", musicId, styleIds);
        InsertJunctionRows(conn, tx, "MusicLanguages", "LanguageId", musicId, languageIds);

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

    public Dictionary<int, List<int>> GetAllMusicStyleIds() => GetAllJunctionIds("MusicStyles", "StyleId");
    public List<int> GetMusicStyleIds(int musicId) => GetJunctionIds("MusicStyles", "StyleId", musicId);

    public Dictionary<int, List<int>> GetAllMusicGenreIds() => GetAllJunctionIds("MusicGenres", "GenreId");
    public List<int> GetMusicGenreIds(int musicId) => GetJunctionIds("MusicGenres", "GenreId", musicId);

    public Dictionary<int, List<int>> GetAllMusicLanguageIds() => GetAllJunctionIds("MusicLanguages", "LanguageId");
    public List<int> GetMusicLanguageIds(int musicId) => GetJunctionIds("MusicLanguages", "LanguageId", musicId);

    public void UpdateTrack(int id, string title, List<int> genreIds, int ratingId,
                            List<int> styleIds, List<int> languageIds, string? notes, bool reEvaluationNeeded)
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

        ReplaceJunctionRows(conn, tx, "MusicGenres", "GenreId", id, genreIds);
        ReplaceJunctionRows(conn, tx, "MusicStyles", "StyleId", id, styleIds);
        ReplaceJunctionRows(conn, tx, "MusicLanguages", "LanguageId", id, languageIds);

        tx.Commit();
    }

    public void DeleteTrack(int id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        foreach (var table in new[] { "MusicGenres", "MusicStyles", "MusicLanguages" })
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"DELETE FROM {table} WHERE MusicId = $id";
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

    // --- Genres ---

    public List<Genre> GetGenres() => GetLookupList("Genres", (id, name) => new Genre(id, name));

    public void InsertGenre(string name) => InsertLookup("Genres", name);
    public bool IsGenreInUse(int id) => IsInUse("MusicGenres", "GenreId", id);
    public void UpdateGenre(int id, string name) => UpdateLookup("Genres", id, name);
    public void DeleteGenre(int id) => DeleteLookup("Genres", id);

    // --- Styles ---

    public List<Style> GetStyles() => GetLookupList("Styles", (id, name) => new Style(id, name));

    public void InsertStyle(string name) => InsertLookup("Styles", name);
    public bool IsStyleInUse(int id) => IsInUse("MusicStyles", "StyleId", id);
    public void UpdateStyle(int id, string name) => UpdateLookup("Styles", id, name);
    public void DeleteStyle(int id) => DeleteLookup("Styles", id);

    // --- Languages ---

    public List<Language> GetLanguages() => GetLookupList("Languages", (id, name) => new Language(id, name));

    public void InsertLanguage(string name) => InsertLookup("Languages", name);
    public bool IsLanguageInUse(int id) => IsInUse("MusicLanguages", "LanguageId", id);
    public void UpdateLanguage(int id, string name) => UpdateLookup("Languages", id, name);
    public void DeleteLanguage(int id) => DeleteLookup("Languages", id);

    // --- Ratings ---

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

    public bool IsRatingInUse(int id) => IsInUse("Music", "RatingId", id);
    public void UpdateRating(int id, string name) => UpdateLookup("Ratings", id, name);
    public void DeleteRating(int id) => DeleteLookup("Ratings", id);

    // --- Private helpers ---

    private List<T> GetLookupList<T>(string table, Func<int, string, T> ctor)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT Id, Name FROM {table} ORDER BY Name";
        using var r = cmd.ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(ctor(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    private void InsertLookup(string table, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"INSERT OR IGNORE INTO {table} (Name) VALUES ($name)";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    private bool IsInUse(string table, string column, int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table} WHERE {column} = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    private void UpdateLookup(string table, int id, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE {table} SET Name = $name WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }

    private void DeleteLookup(string table, int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM {table} WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private Dictionary<int, List<int>> GetAllJunctionIds(string table, string idColumn)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT MusicId, {idColumn} FROM {table}";
        using var r = cmd.ExecuteReader();
        var dict = new Dictionary<int, List<int>>();
        while (r.Read())
        {
            var musicId = r.GetInt32(0);
            var tagId = r.GetInt32(1);
            if (!dict.TryGetValue(musicId, out var ids))
            {
                ids = [];
                dict[musicId] = ids;
            }
            ids.Add(tagId);
        }
        return dict;
    }

    private List<int> GetJunctionIds(string table, string idColumn, int musicId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {idColumn} FROM {table} WHERE MusicId = $id";
        cmd.Parameters.AddWithValue("$id", musicId);
        using var r = cmd.ExecuteReader();
        var ids = new List<int>();
        while (r.Read()) ids.Add(r.GetInt32(0));
        return ids;
    }

    private static void InsertJunctionRows(SqliteConnection conn, SqliteTransaction tx,
        string table, string idColumn, long musicId, List<int> ids)
    {
        if (ids.Count == 0) return;
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"INSERT INTO {table} (MusicId, {idColumn}) VALUES ($mid, $tid)";
        cmd.Parameters.Add("$mid", SqliteType.Integer).Value = musicId;
        var tidParam = cmd.Parameters.Add("$tid", SqliteType.Integer);
        foreach (var id in ids)
        {
            tidParam.Value = id;
            cmd.ExecuteNonQuery();
        }
    }

    private static void ReplaceJunctionRows(SqliteConnection conn, SqliteTransaction tx,
        string table, string idColumn, int musicId, List<int> ids)
    {
        using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = $"DELETE FROM {table} WHERE MusicId = $id";
            del.Parameters.AddWithValue("$id", musicId);
            del.ExecuteNonQuery();
        }
        InsertJunctionRows(conn, tx, table, idColumn, musicId, ids);
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
