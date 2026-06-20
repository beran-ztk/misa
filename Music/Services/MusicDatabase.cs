using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform;
using Microsoft.Data.Sqlite;
using Music.Models;

namespace Music.Services;

public class MusicDatabase
{
    private const string AssetBaseUri = "avares://Music/Assets/";
    private const string ManualAssignmentSourceKey = "manual";
    private readonly string _connectionString = $"Data Source={Values.DbPath}";

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public void Initialize()
    {
        if (File.Exists(Values.DbPath))
            return;

        var directory = Path.GetDirectoryName(Values.DbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var conn = Open();
        using var tx = conn.BeginTransaction();
        CreateSchema(conn, tx);
        SeedDefaultMetadata(conn, tx);
        tx.Commit();
    }

    private static void CreateSchema(SqliteConnection conn, SqliteTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            CREATE TABLE channels (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                name                TEXT NOT NULL,
                source_channel_id   TEXT NULL UNIQUE,
                source_url          TEXT NULL,
                inform_new_songs    INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE ratings (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT NOT NULL UNIQUE,
                sort_order  INTEGER NOT NULL UNIQUE
            );

            CREATE TABLE genres (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                key          TEXT NOT NULL UNIQUE,
                name         TEXT NOT NULL UNIQUE,
                description  TEXT NULL
            );

            CREATE TABLE genre_assignment_sources (
                id      INTEGER PRIMARY KEY AUTOINCREMENT,
                key     TEXT NOT NULL UNIQUE,
                name    TEXT NOT NULL UNIQUE
            );

            CREATE TABLE tracks (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                channel_id          INTEGER NULL REFERENCES channels(id),
                rating_id           INTEGER NULL REFERENCES ratings(id),
                canonical_url       TEXT NULL UNIQUE,
                title               TEXT NOT NULL,
                file_name           TEXT NOT NULL UNIQUE,
                duration_seconds    INTEGER NULL,
                uploaded_at         TEXT NULL,
                downloaded_at       TEXT NOT NULL,
                updated_at          TEXT NOT NULL,
                listen_count        INTEGER NOT NULL DEFAULT 0,
                skip_count          INTEGER NOT NULL DEFAULT 0,
                last_listened_at    TEXT NULL,
                needs_reevaluation  INTEGER NOT NULL DEFAULT 0,
                notes               TEXT NULL
            );

            CREATE TABLE track_genres (
                track_id                 INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                genre_id                 INTEGER NOT NULL REFERENCES genres(id),
                set_by_source_id         INTEGER NOT NULL REFERENCES genre_assignment_sources(id),
                assigned_at              TEXT NOT NULL,
                PRIMARY KEY (track_id, genre_id)
            );

            CREATE TABLE track_analysis (
                id                INTEGER PRIMARY KEY AUTOINCREMENT,
                track_id          INTEGER NOT NULL UNIQUE REFERENCES tracks(id) ON DELETE CASCADE,
                analyzed_at       TEXT NOT NULL,
                analyzer_name     TEXT NULL,
                analyzer_version  TEXT NULL,
                bpm               REAL NULL,
                loudness          REAL NULL,
                danceability      REAL NULL
            );

            CREATE TABLE model_genres (
                id      INTEGER PRIMARY KEY AUTOINCREMENT,
                name    TEXT NOT NULL UNIQUE
            );

            CREATE TABLE model_subgenres (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                model_genre_id  INTEGER NOT NULL REFERENCES model_genres(id) ON DELETE CASCADE,
                name            TEXT NOT NULL,
                UNIQUE (model_genre_id, name)
            );

            CREATE TABLE track_genre_predictions (
                id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                track_analysis_id  INTEGER NOT NULL REFERENCES track_analysis(id) ON DELETE CASCADE,
                model_subgenre_id  INTEGER NOT NULL REFERENCES model_subgenres(id),
                score              REAL NOT NULL CHECK (score >= 0 AND score <= 1),
                UNIQUE (track_analysis_id, model_subgenre_id)
            );

            CREATE TABLE genre_mappings (
                id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                genre_id           INTEGER NOT NULL REFERENCES genres(id) ON DELETE CASCADE,
                model_subgenre_id  INTEGER NOT NULL UNIQUE REFERENCES model_subgenres(id) ON DELETE CASCADE
            );

            CREATE INDEX ix_track_genres_genre_id ON track_genres(genre_id);
            CREATE INDEX ix_model_subgenres_model_genre_id ON model_subgenres(model_genre_id);
            CREATE INDEX ix_track_genre_predictions_analysis_id ON track_genre_predictions(track_analysis_id);
            CREATE INDEX ix_genre_mappings_genre_id ON genre_mappings(genre_id);";
        cmd.ExecuteNonQuery();
    }

    private static void SeedDefaultMetadata(SqliteConnection conn, SqliteTransaction tx)
    {
        foreach (var rating in ReadAsset<RatingSeedDocument>("default-ratings.json").Ratings)
        {
            ExecuteInsert(conn, tx,
                "INSERT INTO ratings (name, sort_order) VALUES ($name, $sortOrder)",
                ("$name", rating.Name), ("$sortOrder", rating.SortOrder));
        }

        var genres = ReadAsset<GenreSeedDocument>("default-genres.json").Genres;
        for (var index = 0; index < genres.Count; index++)
        {
            var genre = genres[index];
            ExecuteInsert(conn, tx,
                "INSERT INTO genres (key, name, description) VALUES ($key, $name, $description)",
                ("$key", ToKey(genre.Name)),
                ("$name", genre.Name),
                ("$description", genre.Description));
        }

        foreach (var source in new[]
                 {
                     new AssignmentSourceSeed("manual", "Manual"),
                     new AssignmentSourceSeed("model_suggestion", "Model suggestion"),
                     new AssignmentSourceSeed("import", "Import"),
                     new AssignmentSourceSeed("system", "System")
                 })
        {
            ExecuteInsert(conn, tx,
                "INSERT INTO genre_assignment_sources (key, name) VALUES ($key, $name)",
                ("$key", source.Key), ("$name", source.Name));
        }

        var modelGenres = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var modelClass in ReadAsset<ModelGenreSeedDocument>("Models/discogs-maest-30s-pw-519l-2.json").Classes)
        {
            var (genreName, subgenreName) = SplitModelClass(modelClass);
            if (!modelGenres.TryGetValue(genreName, out var modelGenreId))
            {
                modelGenreId = InsertAndGetId(conn, tx,
                    "INSERT INTO model_genres (name) VALUES ($name)",
                    ("$name", genreName));
                modelGenres.Add(genreName, modelGenreId);
            }

            ExecuteInsert(conn, tx,
                "INSERT INTO model_subgenres (model_genre_id, name) VALUES ($modelGenreId, $name)",
                ("$modelGenreId", modelGenreId), ("$name", subgenreName));
        }
    }

    public bool TrackExists(string canonicalUrl)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM tracks WHERE canonical_url = $url";
        cmd.Parameters.AddWithValue("$url", canonicalUrl);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public int InsertTrack(string canonicalUrl, string title, string fileName,
        List<int> genreIds, int? ratingId, List<int> _, int? durationSeconds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        var trackId = InsertAndGetId(conn, tx, @"
            INSERT INTO tracks (canonical_url, title, file_name, rating_id, downloaded_at, updated_at, duration_seconds)
            VALUES ($url, $title, $fileName, $ratingId, $downloadedAt, $updatedAt, $duration)",
            ("$url", canonicalUrl),
            ("$title", title),
            ("$fileName", fileName),
            ("$ratingId", ratingId),
            ("$downloadedAt", now),
            ("$updatedAt", now),
            ("$duration", durationSeconds));

        InsertTrackGenres(conn, tx, trackId, genreIds, GetAssignmentSourceId(conn, tx, ManualAssignmentSourceKey), now);
        tx.Commit();
        return (int)trackId;
    }

    public void SaveTrackAnalysis(int trackId, TrackAnalysisResult analysis)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        ExecuteInsert(conn, tx, @"
            INSERT INTO track_analysis (track_id, analyzed_at, analyzer_name)
            VALUES ($trackId, $analyzedAt, $analyzerName)
            ON CONFLICT(track_id) DO UPDATE SET
                analyzed_at = excluded.analyzed_at,
                analyzer_name = excluded.analyzer_name",
            ("$trackId", trackId),
            ("$analyzedAt", DateTime.UtcNow.ToString("O")),
            ("$analyzerName", analysis.AnalyzerName));

        var analysisId = GetTrackAnalysisId(conn, tx, trackId);
        ExecuteInsert(conn, tx,
            "DELETE FROM track_genre_predictions WHERE track_analysis_id = $analysisId",
            ("$analysisId", analysisId));

        foreach (var prediction in analysis.Predictions)
        {
            using var predictionCommand = conn.CreateCommand();
            predictionCommand.Transaction = tx;
            predictionCommand.CommandText = @"
                INSERT INTO track_genre_predictions (track_analysis_id, model_subgenre_id, score)
                SELECT $analysisId, model_subgenres.id, $score
                FROM model_subgenres
                JOIN model_genres ON model_genres.id = model_subgenres.model_genre_id
                WHERE model_genres.name = $modelGenre AND model_subgenres.name = $modelSubgenre";
            predictionCommand.Parameters.AddWithValue("$analysisId", analysisId);
            predictionCommand.Parameters.AddWithValue("$score", prediction.Score);
            predictionCommand.Parameters.AddWithValue("$modelGenre", prediction.ModelGenre);
            predictionCommand.Parameters.AddWithValue("$modelSubgenre", prediction.ModelSubgenre);
            predictionCommand.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public List<MusicTrack> GetAllTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, canonical_url, title, file_name, rating_id, downloaded_at,
                                   duration_seconds, needs_reevaluation
                            FROM tracks ORDER BY downloaded_at DESC";
        using var reader = cmd.ExecuteReader();
        var tracks = new List<MusicTrack>();
        while (reader.Read())
        {
            tracks.Add(new MusicTrack(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6),
                reader.GetInt32(7) != 0));
        }
        return tracks;
    }

    public Dictionary<int, List<int>> GetAllTrackGenreIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT track_id, genre_id FROM track_genres";
        return ReadTrackIdMap(cmd);
    }

    public List<int> GetTrackGenreIds(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT genre_id FROM track_genres WHERE track_id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var genreIds = new List<int>();
        while (reader.Read()) genreIds.Add(reader.GetInt32(0));
        return genreIds;
    }

    // Styles are intentionally no longer part of the schema. These compatibility
    // methods keep the current UI operational until its dedicated filter redesign.
    public Dictionary<int, List<int>> GetAllTrackStyleIds() => [];
    public List<int> GetTrackStyleIds(int trackId) => [];
    public List<Style> GetStyles() => [];

    public void UpdateTrack(int id, string title, List<int> genreIds, int? ratingId, List<int> _)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        ExecuteInsert(conn, tx,
            "UPDATE tracks SET title = $title, rating_id = $ratingId, updated_at = $updatedAt, needs_reevaluation = CASE WHEN $ratingId IS NULL THEN 1 ELSE needs_reevaluation END WHERE id = $id",
            ("$id", id), ("$title", title), ("$ratingId", ratingId), ("$updatedAt", now));

        ExecuteInsert(conn, tx, "DELETE FROM track_genres WHERE track_id = $trackId", ("$trackId", id));
        InsertTrackGenres(conn, tx, id, genreIds, GetAssignmentSourceId(conn, tx, ManualAssignmentSourceKey), now);
        tx.Commit();
    }

    public void SetTrackNeedsReview(int id, bool needsReview)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE tracks SET needs_reevaluation =
                            CASE WHEN rating_id IS NULL THEN 1 ELSE $needsReview END
                            WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$needsReview", needsReview ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTrack(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM tracks WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public List<Genre> GetGenres()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM genres ORDER BY name";
        return ReadLookupList(cmd, (id, name) => new Genre(id, name));
    }

    public List<Rating> GetRatings()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, sort_order FROM ratings ORDER BY sort_order";
        using var reader = cmd.ExecuteReader();
        var ratings = new List<Rating>();
        while (reader.Read()) ratings.Add(new Rating(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
        return ratings;
    }

    public List<ModelGenre> GetModelGenres()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM model_genres ORDER BY name";
        return ReadLookupList(cmd, (id, name) => new ModelGenre(id, name));
    }

    public List<ModelSubgenre> GetModelSubgenres(int? modelGenreId = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, model_genre_id, name
                            FROM model_subgenres
                            WHERE $modelGenreId IS NULL OR model_genre_id = $modelGenreId
                            ORDER BY name";
        cmd.Parameters.AddWithValue("$modelGenreId", modelGenreId is null ? DBNull.Value : modelGenreId.Value);
        using var reader = cmd.ExecuteReader();
        var subgenres = new List<ModelSubgenre>();
        while (reader.Read()) subgenres.Add(new ModelSubgenre(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));
        return subgenres;
    }

    public List<StoredModelGenrePrediction> GetTrackGenrePredictions(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT model_genres.id, model_genres.name, model_subgenres.id, model_subgenres.name, predictions.score
            FROM track_genre_predictions predictions
            JOIN track_analysis analysis ON analysis.id = predictions.track_analysis_id
            JOIN model_subgenres ON model_subgenres.id = predictions.model_subgenre_id
            JOIN model_genres ON model_genres.id = model_subgenres.model_genre_id
            WHERE analysis.track_id = $trackId
            ORDER BY predictions.score DESC";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var predictions = new List<StoredModelGenrePrediction>();
        while (reader.Read())
        {
            predictions.Add(new StoredModelGenrePrediction(
                reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetDouble(4)));
        }
        return predictions;
    }

    public List<GenreMapping> GetGenreMappings()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT gm.id, gm.genre_id, g.name, gm.model_subgenre_id, msg.model_genre_id, msg.name
                            FROM genre_mappings gm
                            JOIN genres g ON g.id = gm.genre_id
                            JOIN model_subgenres msg ON msg.id = gm.model_subgenre_id
                            ORDER BY msg.name";
        using var reader = cmd.ExecuteReader();
        var mappings = new List<GenreMapping>();
        while (reader.Read())
        {
            mappings.Add(new GenreMapping(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetString(5)));
        }
        return mappings;
    }

    public void SetGenreMapping(int genreId, int modelSubgenreId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO genre_mappings (genre_id, model_subgenre_id)
            VALUES ($genreId, $modelSubgenreId)
            ON CONFLICT(model_subgenre_id) DO UPDATE SET genre_id = excluded.genre_id";
        cmd.Parameters.AddWithValue("$genreId", genreId);
        cmd.Parameters.AddWithValue("$modelSubgenreId", modelSubgenreId);
        cmd.ExecuteNonQuery();
    }

    public void RemoveGenreMapping(int modelSubgenreId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM genre_mappings WHERE model_subgenre_id = $modelSubgenreId";
        cmd.Parameters.AddWithValue("$modelSubgenreId", modelSubgenreId);
        cmd.ExecuteNonQuery();
    }

    private static void InsertTrackGenres(SqliteConnection conn, SqliteTransaction tx, long trackId,
        IEnumerable<int> genreIds, long assignmentSourceId, string assignedAt)
    {
        var distinctGenreIds = genreIds.Distinct().ToList();
        if (distinctGenreIds.Count == 0) return;

        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"INSERT INTO track_genres (track_id, genre_id, set_by_source_id, assigned_at)
                            VALUES ($trackId, $genreId, $sourceId, $assignedAt)";
        cmd.Parameters.Add("$trackId", SqliteType.Integer).Value = trackId;
        var genreIdParameter = cmd.Parameters.Add("$genreId", SqliteType.Integer);
        cmd.Parameters.Add("$sourceId", SqliteType.Integer).Value = assignmentSourceId;
        cmd.Parameters.Add("$assignedAt", SqliteType.Text).Value = assignedAt;
        foreach (var genreId in distinctGenreIds)
        {
            genreIdParameter.Value = genreId;
            cmd.ExecuteNonQuery();
        }
    }

    private static long GetAssignmentSourceId(SqliteConnection conn, SqliteTransaction tx, string key)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT id FROM genre_assignment_sources WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);
        return (long)cmd.ExecuteScalar()!;
    }

    private static long GetTrackAnalysisId(SqliteConnection conn, SqliteTransaction tx, int trackId)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT id FROM track_analysis WHERE track_id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        return (long)cmd.ExecuteScalar()!;
    }

    private static List<T> ReadLookupList<T>(SqliteCommand cmd, Func<int, string, T> create)
    {
        using var reader = cmd.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read()) rows.Add(create(reader.GetInt32(0), reader.GetString(1)));
        return rows;
    }

    private static Dictionary<int, List<int>> ReadTrackIdMap(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        var values = new Dictionary<int, List<int>>();
        while (reader.Read())
        {
            var trackId = reader.GetInt32(0);
            if (!values.TryGetValue(trackId, out var ids))
                values[trackId] = ids = [];
            ids.Add(reader.GetInt32(1));
        }
        return values;
    }

    private static void ExecuteInsert(SqliteConnection conn, SqliteTransaction tx, string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);
        cmd.ExecuteNonQuery();
    }

    private static long InsertAndGetId(SqliteConnection conn, SqliteTransaction tx, string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"{sql}; SELECT last_insert_rowid();";
        AddParameters(cmd, parameters);
        return (long)cmd.ExecuteScalar()!;
    }

    private static void AddParameters(SqliteCommand cmd, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static T ReadAsset<T>(string fileName)
    {
        using var stream = AssetLoader.Open(new Uri(AssetBaseUri + fileName));
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException($"Could not read asset '{fileName}'.");
    }

    private static (string Genre, string Subgenre) SplitModelClass(string value)
    {
        var separator = value.IndexOf("---", StringComparison.Ordinal);
        if (separator <= 0 || separator == value.Length - 3)
            throw new InvalidOperationException($"Invalid model class '{value}'. Expected 'Genre---Subgenre'.");

        return (value[..separator], value[(separator + 3)..]);
    }

    private static string ToKey(string name)
    {
        var chars = name
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();
        return string.Join(string.Empty, chars).Trim('_');
    }

    private sealed record RatingSeedDocument(List<RatingSeed> Ratings);
    private sealed record RatingSeed(string Name, int SortOrder);
    private sealed record GenreSeedDocument(List<GenreSeed> Genres);
    private sealed record GenreSeed(string Name, string? Description);
    private sealed record ModelGenreSeedDocument(List<string> Classes);
    private sealed record AssignmentSourceSeed(string Key, string Name);
}
