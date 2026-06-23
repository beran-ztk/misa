using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;
using Microsoft.Data.Sqlite;
using Music.Models;

namespace Music.Services;

public class MusicDatabase
{
    private const string AssetBaseUri = "avares://Music/Assets/";
    private const string RemovedMoodThemeModelName = "mtg_" + "jamen" + "do_" + "mood" + "theme";
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
        {
            using var existingConnection = Open();
            ApplyMigrations(existingConnection);
            return;
        }

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

            CREATE TABLE tag_categories (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                key         TEXT NOT NULL UNIQUE,
                name        TEXT NOT NULL UNIQUE,
                color       TEXT NULL,
                sort_order  INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE tags (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                category_id  INTEGER NOT NULL REFERENCES tag_categories(id) ON DELETE CASCADE,
                name         TEXT NOT NULL,
                description  TEXT NULL,
                UNIQUE (category_id, name)
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
                listened_seconds    INTEGER NOT NULL DEFAULT 0,
                skip_count          INTEGER NOT NULL DEFAULT 0,
                last_listened_at    TEXT NULL,
                file_size_bytes     INTEGER NULL,
                download_duration_ms INTEGER NULL,
                needs_reevaluation  INTEGER NOT NULL DEFAULT 0,
                notes               TEXT NULL
            );

            CREATE TABLE track_genres (
                track_id                 INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                genre_id                 INTEGER NOT NULL REFERENCES model_subgenres(id),
                assigned_at              TEXT NOT NULL,
                is_enabled               INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (track_id, genre_id)
            );

            CREATE TABLE track_tags (
                track_id     INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                tag_id       INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                source       TEXT NOT NULL DEFAULT 'manual',
                strength     REAL NULL CHECK (strength IS NULL OR (strength >= 0 AND strength <= 1)),
                confidence   REAL NULL CHECK (confidence IS NULL OR (confidence >= 0 AND confidence <= 1)),
                assigned_at  TEXT NOT NULL,
                updated_at   TEXT NOT NULL,
                PRIMARY KEY (track_id, tag_id)
            );

            CREATE TABLE track_analysis (
                id                INTEGER PRIMARY KEY AUTOINCREMENT,
                track_id          INTEGER NOT NULL UNIQUE REFERENCES tracks(id) ON DELETE CASCADE,
                analyzed_at       TEXT NOT NULL,
                analyzer_name     TEXT NULL,
                analyzer_version  TEXT NULL,
                bpm               REAL NULL,
                integrated_loudness REAL NULL,
                loudness_range    REAL NULL,
                danceability      REAL NULL,
                analysis_duration_ms INTEGER NULL
            );

            CREATE TABLE model_genres (
                id      INTEGER PRIMARY KEY AUTOINCREMENT,
                name    TEXT NOT NULL UNIQUE
            );

            CREATE TABLE model_subgenres (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                model_genre_id  INTEGER NOT NULL REFERENCES model_genres(id) ON DELETE CASCADE,
                name            TEXT NOT NULL,
                description     TEXT NULL,
                classification_hint TEXT NULL,
                bpm_min         INTEGER NULL,
                bpm_max         INTEGER NULL,
                UNIQUE (model_genre_id, name)
            );

            CREATE TABLE model_subgenre_distinctions (
                id                              INTEGER PRIMARY KEY AUTOINCREMENT,
                model_subgenre_id               INTEGER NOT NULL REFERENCES model_subgenres(id) ON DELETE CASCADE,
                distinguish_from_model_subgenre_id INTEGER NOT NULL REFERENCES model_subgenres(id) ON DELETE CASCADE,
                difference                      TEXT NOT NULL,
                UNIQUE (model_subgenre_id, distinguish_from_model_subgenre_id)
            );

            CREATE TABLE track_genre_predictions (
                id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                track_analysis_id  INTEGER NOT NULL REFERENCES track_analysis(id) ON DELETE CASCADE,
                model_subgenre_id  INTEGER NOT NULL REFERENCES model_subgenres(id),
                score              REAL NOT NULL CHECK (score >= 0 AND score <= 1),
                UNIQUE (track_analysis_id, model_subgenre_id)
            );

            CREATE TABLE track_analysis_signals (
                id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                track_analysis_id  INTEGER NOT NULL REFERENCES track_analysis(id) ON DELETE CASCADE,
                model_family       TEXT NOT NULL,
                category           TEXT NOT NULL,
                model_name         TEXT NOT NULL,
                model_type         TEXT NOT NULL,
                description        TEXT NOT NULL,
                signal_key         TEXT NOT NULL,
                score              REAL NOT NULL,
                UNIQUE (track_analysis_id, model_name, signal_key)
            );

            CREATE TABLE track_derived_attributes (
                id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                track_analysis_id  INTEGER NOT NULL REFERENCES track_analysis(id) ON DELETE CASCADE,
                attribute_key      TEXT NOT NULL,
                system_value       TEXT NOT NULL,
                system_score       REAL NOT NULL,
                manual_value       TEXT NULL,
                UNIQUE (track_analysis_id, attribute_key)
            );

            CREATE TABLE import_batches (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                source_url  TEXT NOT NULL,
                created_at  TEXT NOT NULL
            );

            CREATE TABLE import_queue_items (
                id                    INTEGER PRIMARY KEY AUTOINCREMENT,
                batch_id              INTEGER NOT NULL REFERENCES import_batches(id) ON DELETE CASCADE,
                source_url            TEXT NOT NULL,
                canonical_url         TEXT NOT NULL UNIQUE,
                title                 TEXT NOT NULL,
                duration_seconds      INTEGER NULL,
                estimated_size_bytes  INTEGER NULL,
                status                TEXT NOT NULL,
                detail                TEXT NULL,
                track_id              INTEGER NULL REFERENCES tracks(id) ON DELETE SET NULL,
                created_at            TEXT NOT NULL,
                updated_at            TEXT NOT NULL
            );

            CREATE INDEX ix_track_genres_genre_id ON track_genres(genre_id);
            CREATE INDEX ix_tags_category_id ON tags(category_id);
            CREATE INDEX ix_track_tags_tag_id ON track_tags(tag_id);
            CREATE INDEX ix_model_subgenres_model_genre_id ON model_subgenres(model_genre_id);
            CREATE INDEX ix_model_subgenre_distinctions_source ON model_subgenre_distinctions(model_subgenre_id);
            CREATE INDEX ix_track_genre_predictions_analysis_id ON track_genre_predictions(track_analysis_id);
            CREATE INDEX ix_track_analysis_signals_analysis_id ON track_analysis_signals(track_analysis_id);
            CREATE INDEX ix_track_derived_attributes_analysis_id ON track_derived_attributes(track_analysis_id);
            ";
        cmd.ExecuteNonQuery();
    }

    private static void ApplyMigrations(SqliteConnection conn)
    {
        EnsureColumn(conn, "tracks", "listened_seconds", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "tracks", "file_size_bytes", "INTEGER NULL");
        EnsureColumn(conn, "tracks", "download_duration_ms", "INTEGER NULL");
        EnsureColumn(conn, "track_analysis", "analysis_duration_ms", "INTEGER NULL");
        EnsureColumn(conn, "model_subgenres", "description", "TEXT NULL");
        EnsureColumn(conn, "model_subgenres", "classification_hint", "TEXT NULL");
        EnsureColumn(conn, "model_subgenres", "bpm_min", "INTEGER NULL");
        EnsureColumn(conn, "model_subgenres", "bpm_max", "INTEGER NULL");
        CreateImportQueueSchema(conn);
        CreateModelMetadataSchema(conn);
        CreateTagSchema(conn);
        SimplifyTagSchemaIfNeeded(conn);
        RemoveExcludedExperimentalModelData(conn);
        RemoveCompletedImportQueueItems(conn);
        DropTagRuleSchema(conn);
        SeedDefaultLookups(conn);
        if (ModelMetadataNeedsImport(conn))
        {
            using var tx = conn.BeginTransaction();
            SynchronizeModelMetadata(conn, tx);
            tx.Commit();
        }
    }

    private static void RemoveExcludedExperimentalModelData(SqliteConnection conn)
    {
        if (!TableExists(conn, "track_analysis_signals"))
            return;

        ExecuteNonQuery(conn,
            "DELETE FROM track_analysis_signals WHERE model_name = $modelName",
            ("$modelName", RemovedMoodThemeModelName));

        RebuildDerivedAttributes(conn);
    }

    private static void DropTagRuleSchema(SqliteConnection conn)
    {
        ExecuteNonQuery(conn, "DROP INDEX IF EXISTS ix_tag_rules_tag_id");
        ExecuteNonQuery(conn, "DROP INDEX IF EXISTS ix_tag_rule_groups_tag_id");
        ExecuteNonQuery(conn, "DROP INDEX IF EXISTS ix_tag_rule_conditions_group_id");
        ExecuteNonQuery(conn, "DROP INDEX IF EXISTS ix_track_tag_suggestions_track_id");
        ExecuteNonQuery(conn, "DROP TABLE IF EXISTS track_tag_suggestions");
        ExecuteNonQuery(conn, "DROP TABLE IF EXISTS tag_rule_conditions");
        ExecuteNonQuery(conn, "DROP TABLE IF EXISTS tag_rule_groups");
        ExecuteNonQuery(conn, "DROP TABLE IF EXISTS tag_rules");
    }

    private static void CreateTagSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS tag_categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL UNIQUE,
                color TEXT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                category_id INTEGER NOT NULL REFERENCES tag_categories(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                description TEXT NULL,
                UNIQUE (category_id, name)
            );
            CREATE TABLE IF NOT EXISTS track_tags (
                track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                source TEXT NOT NULL DEFAULT 'manual',
                strength REAL NULL CHECK (strength IS NULL OR (strength >= 0 AND strength <= 1)),
                confidence REAL NULL CHECK (confidence IS NULL OR (confidence >= 0 AND confidence <= 1)),
                assigned_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                PRIMARY KEY (track_id, tag_id)
            );
            CREATE INDEX IF NOT EXISTS ix_tags_category_id ON tags(category_id);
            CREATE INDEX IF NOT EXISTS ix_track_tags_tag_id ON track_tags(tag_id);
            ";
        cmd.ExecuteNonQuery();
    }

    private static void SimplifyTagSchemaIfNeeded(SqliteConnection conn)
    {
        if (!ColumnExists(conn, "tags", "key"))
            return;

        using (var foreignKeyCommand = conn.CreateCommand())
        {
            foreignKeyCommand.CommandText = "PRAGMA foreign_keys = OFF;";
            foreignKeyCommand.ExecuteNonQuery();
        }

        try
        {
            using var tx = conn.BeginTransaction();
            ExecuteInsert(conn, tx, "DROP INDEX IF EXISTS ix_tags_category_id");
            ExecuteInsert(conn, tx, "DROP INDEX IF EXISTS ix_track_tags_tag_id");
            ExecuteInsert(conn, tx, "DROP INDEX IF EXISTS ix_tag_rules_tag_id");
            ExecuteInsert(conn, tx, "DROP INDEX IF EXISTS ix_track_tag_suggestions_track_id");

            ExecuteInsert(conn, tx, "DROP TABLE IF EXISTS track_tag_suggestions");
            ExecuteInsert(conn, tx, "DROP TABLE IF EXISTS tag_rules");
            ExecuteInsert(conn, tx, "ALTER TABLE track_tags RENAME TO track_tags_legacy");
            ExecuteInsert(conn, tx, "ALTER TABLE tags RENAME TO tags_legacy");

            ExecuteInsert(conn, tx, @"
                CREATE TABLE tags (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category_id INTEGER NOT NULL REFERENCES tag_categories(id) ON DELETE CASCADE,
                    name TEXT NOT NULL,
                    description TEXT NULL,
                    UNIQUE (category_id, name)
                )");
            ExecuteInsert(conn, tx, @"
                INSERT INTO tags (id, category_id, name, description)
                SELECT id, category_id, name, description FROM tags_legacy");

            ExecuteInsert(conn, tx, @"
                CREATE TABLE track_tags (
                    track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                    tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                    source TEXT NOT NULL DEFAULT 'manual',
                    strength REAL NULL CHECK (strength IS NULL OR (strength >= 0 AND strength <= 1)),
                    confidence REAL NULL CHECK (confidence IS NULL OR (confidence >= 0 AND confidence <= 1)),
                    assigned_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    PRIMARY KEY (track_id, tag_id)
                )");
            ExecuteInsert(conn, tx, @"
                INSERT INTO track_tags (track_id, tag_id, source, strength, confidence, assigned_at, updated_at)
                SELECT track_id, tag_id, source, strength, confidence, assigned_at, updated_at
                FROM track_tags_legacy");

            ExecuteInsert(conn, tx, "DROP TABLE track_tags_legacy");
            ExecuteInsert(conn, tx, "DROP TABLE tags_legacy");
            ExecuteInsert(conn, tx, "CREATE INDEX ix_tags_category_id ON tags(category_id)");
            ExecuteInsert(conn, tx, "CREATE INDEX ix_track_tags_tag_id ON track_tags(tag_id)");
            tx.Commit();
        }
        finally
        {
            using var foreignKeyCommand = conn.CreateCommand();
            foreignKeyCommand.CommandText = "PRAGMA foreign_keys = ON;";
            foreignKeyCommand.ExecuteNonQuery();
        }
    }

    private static void CreateModelMetadataSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS model_subgenre_distinctions (
                id                              INTEGER PRIMARY KEY AUTOINCREMENT,
                model_subgenre_id               INTEGER NOT NULL REFERENCES model_subgenres(id) ON DELETE CASCADE,
                distinguish_from_model_subgenre_id INTEGER NOT NULL REFERENCES model_subgenres(id) ON DELETE CASCADE,
                difference                      TEXT NOT NULL,
                UNIQUE (model_subgenre_id, distinguish_from_model_subgenre_id)
            );
            CREATE INDEX IF NOT EXISTS ix_model_subgenre_distinctions_source
                ON model_subgenre_distinctions(model_subgenre_id);";
        cmd.ExecuteNonQuery();
    }

    private static bool ModelMetadataNeedsImport(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(*) FROM model_subgenres
                            WHERE description IS NOT NULL OR classification_hint IS NOT NULL
                               OR bpm_min IS NOT NULL OR bpm_max IS NOT NULL";
        return Convert.ToInt32(cmd.ExecuteScalar()) == 0;
    }

    private static void CreateImportQueueSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS import_batches (
                id INTEGER PRIMARY KEY AUTOINCREMENT, source_url TEXT NOT NULL, created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS import_queue_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                batch_id INTEGER NOT NULL REFERENCES import_batches(id) ON DELETE CASCADE,
                source_url TEXT NOT NULL,
                canonical_url TEXT NOT NULL UNIQUE,
                title TEXT NOT NULL,
                duration_seconds INTEGER NULL,
                estimated_size_bytes INTEGER NULL,
                status TEXT NOT NULL,
                detail TEXT NULL,
                track_id INTEGER NULL REFERENCES tracks(id) ON DELETE SET NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_import_queue_status ON import_queue_items(status, created_at);";
        cmd.ExecuteNonQuery();
    }

    private static void RemoveCompletedImportQueueItems(SqliteConnection conn)
    {
        if (!TableExists(conn, "import_queue_items"))
            return;
        ExecuteNonQuery(conn, "DELETE FROM import_queue_items WHERE status = $status",
            ("$status", ImportQueueStatus.ReadyForReview.ToString()));
    }

    private static void EnsureColumn(SqliteConnection conn, string table, string column, string definition)
    {
        if (ColumnExists(conn, table, column)) return;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        cmd.ExecuteNonQuery();
    }

    private static bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static bool TableExists(SqliteConnection conn, string table)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $table";
        cmd.Parameters.AddWithValue("$table", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static long TableRowCount(SqliteConnection conn, string table)
    {
        if (!TableExists(conn, table))
            return 0;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
        return (long)cmd.ExecuteScalar()!;
    }

    public void RecordTrackPlaybackStarted(int trackId)
    {
        using var conn = Open();
        ExecuteNonQuery(conn,
            "UPDATE tracks SET listen_count = listen_count + 1, last_listened_at = $now WHERE id = $trackId",
            ("$now", DateTime.UtcNow.ToString("O")), ("$trackId", trackId));
    }

    public void AddTrackListenedSeconds(int trackId, int seconds)
    {
        if (seconds <= 0) return;
        using var conn = Open();
        ExecuteNonQuery(conn,
            "UPDATE tracks SET listened_seconds = listened_seconds + $seconds WHERE id = $trackId",
            ("$seconds", seconds), ("$trackId", trackId));
    }

    public void RecordTrackSkip(int trackId)
    {
        using var conn = Open();
        ExecuteNonQuery(conn,
            "UPDATE tracks SET skip_count = skip_count + 1 WHERE id = $trackId",
            ("$trackId", trackId));
    }

    public TrackUsageStats GetTrackUsageStats(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT listen_count, listened_seconds, skip_count, last_listened_at
                            FROM tracks WHERE id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        return reader.Read()
            ? new TrackUsageStats(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3))
            : new TrackUsageStats(0, 0, 0, null);
    }

    public TimeSpan? EstimateAnalysisDuration(int? trackDurationSeconds, long? fileSizeBytes)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.duration_seconds, tracks.file_size_bytes, analysis.analysis_duration_ms
                            FROM track_analysis analysis JOIN tracks ON tracks.id = analysis.track_id
                            WHERE analysis.analysis_duration_ms IS NOT NULL
                            ORDER BY analysis.analyzed_at DESC LIMIT 30";
        using var reader = cmd.ExecuteReader();
        var samples = new List<(int? Duration, long? Size, int Milliseconds)>();
        while (reader.Read())
            samples.Add((reader.IsDBNull(0) ? null : reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt64(1), reader.GetInt32(2)));
        if (samples.Count == 0) return null;

        var estimates = new List<double> { samples.Average(sample => (double)sample.Milliseconds) };
        if (trackDurationSeconds is > 0)
        {
            var durationRates = samples.Where(sample => sample.Duration is > 0)
                .Select(sample => sample.Milliseconds / (double)sample.Duration!.Value).ToList();
            if (durationRates.Count > 0) estimates.Add(durationRates.Average() * trackDurationSeconds.Value);
        }
        if (fileSizeBytes is > 0)
        {
            var sizeRates = samples.Where(sample => sample.Size is > 0)
                .Select(sample => sample.Milliseconds / (sample.Size!.Value / 1_000_000d)).ToList();
            if (sizeRates.Count > 0) estimates.Add(sizeRates.Average() * (fileSizeBytes.Value / 1_000_000d));
        }
        return TimeSpan.FromMilliseconds(Math.Clamp(estimates.Average(), 3_000, 15 * 60_000));
    }

    public TimeSpan? EstimateDownloadDuration(int? trackDurationSeconds, long? fileSizeBytes)
    {
        if (trackDurationSeconds is not > 0 && fileSizeBytes is not > 0) return null;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT duration_seconds, file_size_bytes, download_duration_ms FROM tracks
                            WHERE download_duration_ms IS NOT NULL AND download_duration_ms > 0
                            ORDER BY downloaded_at DESC LIMIT 30";
        using var reader = cmd.ExecuteReader();
        var samples = new List<(int? Duration, long? Size, int Milliseconds)>();
        while (reader.Read())
            samples.Add((reader.IsDBNull(0) ? null : reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt64(1), reader.GetInt32(2)));
        if (samples.Count == 0) return null;

        var estimates = new List<(double Value, double Weight)> { (samples.Average(sample => (double)sample.Milliseconds), .15) };
        if (trackDurationSeconds is > 0)
        {
            var rates = samples.Where(sample => sample.Duration is > 0)
                .Select(sample => sample.Milliseconds / (double)sample.Duration!.Value).ToList();
            if (rates.Count > 0) estimates.Add((rates.Average() * trackDurationSeconds.Value, .7));
        }
        if (fileSizeBytes is > 0)
        {
            var rates = samples.Where(sample => sample.Size is > 0)
                .Select(sample => sample.Milliseconds / (sample.Size!.Value / 1_000_000d)).ToList();
            if (rates.Count > 0) estimates.Add((rates.Average() * (fileSizeBytes.Value / 1_000_000d), .15));
        }
        var estimate = estimates.Sum(item => item.Value * item.Weight) / estimates.Sum(item => item.Weight);
        return TimeSpan.FromMilliseconds(Math.Clamp(estimate, 1_000, 60 * 60_000));
    }

    public int CreateImportBatch(string sourceUrl, IReadOnlyList<ImportPreviewItem> items)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        var batchId = InsertAndGetId(conn, tx,
            "INSERT INTO import_batches (source_url, created_at) VALUES ($sourceUrl, $createdAt)",
            ("$sourceUrl", sourceUrl), ("$createdAt", now));

        foreach (var item in items.Where(item => item.Status == ImportQueueStatus.Queued))
        {
            ExecuteInsert(conn, tx, @"
                INSERT INTO import_queue_items
                    (batch_id, source_url, canonical_url, title, duration_seconds, estimated_size_bytes, status, detail, created_at, updated_at)
                VALUES ($batchId, $sourceUrl, $canonicalUrl, $title, $duration, $size, $status, $detail, $createdAt, $updatedAt)
                ON CONFLICT(canonical_url) DO UPDATE SET
                    batch_id = excluded.batch_id,
                    source_url = excluded.source_url,
                    title = excluded.title,
                    duration_seconds = excluded.duration_seconds,
                    estimated_size_bytes = excluded.estimated_size_bytes,
                    status = excluded.status,
                    detail = excluded.detail,
                    track_id = NULL,
                    updated_at = excluded.updated_at",
                ("$batchId", batchId), ("$sourceUrl", item.SourceUrl), ("$canonicalUrl", item.CanonicalUrl),
                ("$title", item.Title), ("$duration", item.DurationSeconds), ("$size", item.EstimatedSizeBytes),
                ("$status", ImportQueueStatus.Queued.ToString()), ("$detail", item.Detail),
                ("$createdAt", now), ("$updatedAt", now));
        }
        tx.Commit();
        return (int)batchId;
    }

    public void RequeueInterruptedImports()
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"UPDATE import_queue_items SET status = $queued,
                                detail = 'Interrupted — cleaned up and queued again',
                                track_id = NULL,
                                updated_at = $now
                                WHERE status IN ($downloading, $analyzing)",
            ("$queued", ImportQueueStatus.Queued.ToString()),
            ("$downloading", ImportQueueStatus.Downloading.ToString()),
            ("$analyzing", ImportQueueStatus.Analyzing.ToString()),
            ("$now", DateTime.UtcNow.ToString("O")));
    }

    public List<ImportQueueItem> GetInterruptedImportQueueItems()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, batch_id, source_url, canonical_url, title, duration_seconds, estimated_size_bytes,
                                   status, detail, track_id
                            FROM import_queue_items
                            WHERE status IN ($downloading, $analyzing)
                            ORDER BY created_at, id";
        cmd.Parameters.AddWithValue("$downloading", ImportQueueStatus.Downloading.ToString());
        cmd.Parameters.AddWithValue("$analyzing", ImportQueueStatus.Analyzing.ToString());
        using var reader = cmd.ExecuteReader();
        var items = new List<ImportQueueItem>();
        while (reader.Read()) items.Add(ReadImportQueueItem(reader));
        return items;
    }

    public ImportQueueItem? GetNextQueuedImport()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, batch_id, source_url, canonical_url, title, duration_seconds, estimated_size_bytes,
                                   status, detail, track_id
                            FROM import_queue_items WHERE status = $status ORDER BY created_at, id LIMIT 1";
        cmd.Parameters.AddWithValue("$status", ImportQueueStatus.Queued.ToString());
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadImportQueueItem(reader) : null;
    }

    public void UpdateImportQueueItem(int id, ImportQueueStatus status, string? detail = null, int? trackId = null)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"UPDATE import_queue_items SET status = $status, detail = $detail,
                                track_id = COALESCE($trackId, track_id), updated_at = $updatedAt WHERE id = $id",
            ("$status", status.ToString()), ("$detail", detail), ("$trackId", trackId),
            ("$updatedAt", DateTime.UtcNow.ToString("O")), ("$id", id));
    }

    public ImportQueueSummary GetImportQueueSummary()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT status, COUNT(*) FROM import_queue_items
                            WHERE status IN ($queued, $downloading, $analyzing) GROUP BY status";
        cmd.Parameters.AddWithValue("$queued", ImportQueueStatus.Queued.ToString());
        cmd.Parameters.AddWithValue("$downloading", ImportQueueStatus.Downloading.ToString());
        cmd.Parameters.AddWithValue("$analyzing", ImportQueueStatus.Analyzing.ToString());
        var counts = new Dictionary<string, int>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) counts[reader.GetString(0)] = reader.GetInt32(1);
        return new ImportQueueSummary(
            counts.GetValueOrDefault(ImportQueueStatus.Queued.ToString()),
            counts.GetValueOrDefault(ImportQueueStatus.Downloading.ToString()),
            counts.GetValueOrDefault(ImportQueueStatus.Analyzing.ToString()),
            0);
    }

    public HashSet<string> GetActiveImportCanonicalUrls()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT canonical_url FROM import_queue_items
                            WHERE status IN ($queued, $downloading, $analyzing)";
        cmd.Parameters.AddWithValue("$queued", ImportQueueStatus.Queued.ToString());
        cmd.Parameters.AddWithValue("$downloading", ImportQueueStatus.Downloading.ToString());
        cmd.Parameters.AddWithValue("$analyzing", ImportQueueStatus.Analyzing.ToString());
        using var reader = cmd.ExecuteReader();
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) urls.Add(reader.GetString(0));
        return urls;
    }

    public List<ImportQueueSource> GetImportQueueSources()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, batch_id, source_url, canonical_url, title, duration_seconds, estimated_size_bytes,
                                   status, detail, track_id
                            FROM import_queue_items
                            WHERE status IN ($queued, $downloading, $analyzing, $failed)
                            ORDER BY CASE status
                                         WHEN $downloading THEN 0
                                         WHEN $analyzing THEN 0
                                         WHEN $queued THEN 1
                                         ELSE 2
                                     END,
                                     batch_id DESC, created_at, id";
        cmd.Parameters.AddWithValue("$queued", ImportQueueStatus.Queued.ToString());
        cmd.Parameters.AddWithValue("$downloading", ImportQueueStatus.Downloading.ToString());
        cmd.Parameters.AddWithValue("$analyzing", ImportQueueStatus.Analyzing.ToString());
        cmd.Parameters.AddWithValue("$failed", ImportQueueStatus.Failed.ToString());
        using var reader = cmd.ExecuteReader();
        var items = new List<ImportQueueItem>();
        while (reader.Read()) items.Add(ReadImportQueueItem(reader));
        return items.GroupBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ImportQueueSource(group.Key, group.ToList())).ToList();
    }

    public bool RemoveQueuedImport(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM import_queue_items WHERE id = $id AND status IN ($queued, $failed)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$queued", ImportQueueStatus.Queued.ToString());
        cmd.Parameters.AddWithValue("$failed", ImportQueueStatus.Failed.ToString());
        return cmd.ExecuteNonQuery() > 0;
    }

    public void DeleteImportQueueItem(int id)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, "DELETE FROM import_queue_items WHERE id = $id", ("$id", id));
    }

    private static ImportQueueItem ReadImportQueueItem(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetInt32(5), reader.IsDBNull(6) ? null : reader.GetInt64(6),
        Enum.Parse<ImportQueueStatus>(reader.GetString(7)), reader.IsDBNull(8) ? null : reader.GetString(8),
        reader.IsDBNull(9) ? null : reader.GetInt32(9));

    private static void SeedDefaultMetadata(SqliteConnection conn, SqliteTransaction tx)
    {
        foreach (var rating in ReadAsset<RatingSeedDocument>("default-ratings.json").Ratings)
        {
            ExecuteInsert(conn, tx,
                "INSERT INTO ratings (name, sort_order) VALUES ($name, $sortOrder)",
                ("$name", rating.Name), ("$sortOrder", rating.SortOrder));
        }

        SeedDefaultLookups(conn, tx);

        SynchronizeModelMetadata(conn, tx);
    }

    private static void SeedDefaultLookups(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();
        SeedDefaultLookups(conn, tx);
        tx.Commit();
    }

    private static void SeedDefaultLookups(SqliteConnection conn, SqliteTransaction tx)
    {
        var lookups = ReadAsset<LookupSeedDocument>("default-lookups.json");

        foreach (var category in lookups.TagCategories)
        {
            var key = string.IsNullOrWhiteSpace(category.Key) ? SlugKey(category.Name) : category.Key;
            ExecuteInsert(conn, tx, @"
                INSERT INTO tag_categories (key, name, color, sort_order)
                VALUES ($key, $name, $color, $sortOrder)
                ON CONFLICT(key) DO UPDATE SET
                    name = excluded.name,
                    color = excluded.color,
                    sort_order = excluded.sort_order",
                ("$key", key), ("$name", category.Name), ("$color", category.Color), ("$sortOrder", category.SortOrder));

            var categoryId = SelectId(conn, tx, "SELECT id FROM tag_categories WHERE key = $key", ("$key", key));
            foreach (var tag in category.Tags)
            {
                ExecuteInsert(conn, tx, @"
                    INSERT INTO tags (category_id, name, description)
                    VALUES ($categoryId, $name, $description)
                    ON CONFLICT(category_id, name) DO UPDATE SET description = excluded.description",
                    ("$categoryId", categoryId),
                    ("$name", tag.Name),
                    ("$description", string.IsNullOrWhiteSpace(tag.Description) ? DBNull.Value : tag.Description));
            }
        }

    }

    /// <summary>
    /// Imports the human-authored MAEST vocabulary unchanged for a newly created or not-yet-migrated database.
    /// </summary>
    private static void SynchronizeModelMetadata(SqliteConnection conn, SqliteTransaction tx)
    {
        var metadata = ReadAsset<List<ModelSubgenreMetadataSeed>>("Models/maest-genre-metadata.json");
        var genreIds = new Dictionary<string, long>(StringComparer.Ordinal);
        var subgenreIds = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var item in metadata)
        {
            if (!genreIds.TryGetValue(item.Genre, out var genreId))
            {
                ExecuteInsert(conn, tx, "INSERT OR IGNORE INTO model_genres (name) VALUES ($name)", ("$name", item.Genre));
                genreId = SelectId(conn, tx, "SELECT id FROM model_genres WHERE name = $name", ("$name", item.Genre));
                genreIds.Add(item.Genre, genreId);
            }

            ExecuteInsert(conn, tx, @"INSERT INTO model_subgenres
                    (model_genre_id, name, description, classification_hint, bpm_min, bpm_max)
                VALUES ($genreId, $name, $description, $hint, $bpmMin, $bpmMax)
                ON CONFLICT(model_genre_id, name) DO UPDATE SET
                    description = excluded.description,
                    classification_hint = excluded.classification_hint,
                    bpm_min = excluded.bpm_min,
                    bpm_max = excluded.bpm_max",
                ("$genreId", genreId), ("$name", item.Subgenre), ("$description", item.Description),
                ("$hint", item.ClassificationHint), ("$bpmMin", item.BpmMin), ("$bpmMax", item.BpmMax));

            subgenreIds[item.Label] = SelectId(conn, tx,
                "SELECT id FROM model_subgenres WHERE model_genre_id = $genreId AND name = $name",
                ("$genreId", genreId), ("$name", item.Subgenre));
        }

        ExecuteInsert(conn, tx, "DELETE FROM model_subgenre_distinctions");
        foreach (var item in metadata)
        {
            if (!subgenreIds.TryGetValue(item.Label, out var sourceId)) continue;
            foreach (var distinction in item.DistinguishFrom)
            {
                if (!subgenreIds.TryGetValue(distinction.Label, out var targetId)) continue;
                ExecuteInsert(conn, tx, @"INSERT OR REPLACE INTO model_subgenre_distinctions
                        (model_subgenre_id, distinguish_from_model_subgenre_id, difference)
                    VALUES ($sourceId, $targetId, $difference)",
                    ("$sourceId", sourceId), ("$targetId", targetId), ("$difference", distinction.Difference));
            }
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
        List<int> genreIds, int? ratingId, List<int> _, int? durationSeconds, long? fileSizeBytes,
        int? downloadDurationMilliseconds, YouTubeTrackMetadata? metadata = null)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        long? channelId = null;
        if (!string.IsNullOrWhiteSpace(metadata?.ChannelId))
        {
            ExecuteInsert(conn, tx, @"INSERT INTO channels (name, source_channel_id, source_url)
                VALUES ($name, $channelId, $channelUrl)
                ON CONFLICT(source_channel_id) DO UPDATE SET name = excluded.name, source_url = excluded.source_url",
                ("$name", metadata.ChannelName ?? "Unknown channel"), ("$channelId", metadata.ChannelId), ("$channelUrl", metadata.ChannelUrl));
            channelId = InsertAndGetId(conn, tx, "SELECT id FROM channels WHERE source_channel_id = $channelId",
                ("$channelId", metadata.ChannelId));
        }

        var trackId = InsertAndGetId(conn, tx, @"
            INSERT INTO tracks (canonical_url, title, file_name, channel_id, rating_id, uploaded_at, downloaded_at, updated_at, duration_seconds, file_size_bytes, download_duration_ms)
            VALUES ($url, $title, $fileName, $channelId, $ratingId, $uploadedAt, $downloadedAt, $updatedAt, $duration, $fileSizeBytes, $downloadDurationMs)",
            ("$url", canonicalUrl),
            ("$title", title),
            ("$fileName", fileName),
            ("$channelId", channelId),
            ("$ratingId", ratingId),
            ("$uploadedAt", metadata?.UploadedAt),
            ("$downloadedAt", now),
            ("$updatedAt", now),
            ("$duration", durationSeconds),
            ("$fileSizeBytes", fileSizeBytes),
            ("$downloadDurationMs", downloadDurationMilliseconds));

        tx.Commit();
        return (int)trackId;
    }

    public void SaveTrackAnalysis(int trackId, TrackAnalysisResult analysis, int? analysisDurationMilliseconds = null)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        ExecuteInsert(conn, tx, @"
            INSERT INTO track_analysis (track_id, analyzed_at, analyzer_name, bpm, integrated_loudness, loudness_range, analysis_duration_ms)
            VALUES ($trackId, $analyzedAt, $analyzerName, $bpm, $loudness, $loudnessRange, $analysisDurationMs)
            ON CONFLICT(track_id) DO UPDATE SET
                analyzed_at = excluded.analyzed_at,
                analyzer_name = excluded.analyzer_name,
                bpm = excluded.bpm,
                integrated_loudness = excluded.integrated_loudness,
                loudness_range = excluded.loudness_range,
                analysis_duration_ms = excluded.analysis_duration_ms",
            ("$trackId", trackId),
            ("$analyzedAt", DateTime.UtcNow.ToString("O")),
            ("$analyzerName", analysis.AnalyzerName),
            ("$bpm", analysis.Bpm),
            ("$loudness", analysis.IntegratedLoudness),
            ("$loudnessRange", analysis.LoudnessRange),
            ("$analysisDurationMs", analysisDurationMilliseconds));

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

        ExecuteInsert(conn, tx,
            "DELETE FROM track_analysis_signals WHERE track_analysis_id = $analysisId",
            ("$analysisId", analysisId));
        foreach (var model in analysis.ExperimentalModels ?? [])
        foreach (var value in model.Values)
        {
            ExecuteInsert(conn, tx, @"
                INSERT INTO track_analysis_signals
                    (track_analysis_id, model_family, category, model_name, model_type, description, signal_key, score)
                VALUES ($analysisId, $family, $category, $model, $type, $description, $key, $score)",
                ("$analysisId", analysisId), ("$family", model.Family), ("$category", model.Category),
                ("$model", model.Model), ("$type", model.Type), ("$description", model.Description),
                ("$key", value.Label), ("$score", value.Score));
        }

        SaveDerivedAttributes(conn, tx, analysisId, DeriveAttributes(analysis.ExperimentalModels ?? []));

        RefreshModelGenres(conn, tx, trackId);

        tx.Commit();
    }

    public List<MusicTrack> GetAllTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.id, tracks.canonical_url, tracks.title, tracks.file_name, tracks.rating_id, tracks.downloaded_at,
                                   tracks.duration_seconds, tracks.needs_reevaluation, channels.name, channels.source_url, tracks.uploaded_at
                            FROM tracks LEFT JOIN channels ON channels.id = tracks.channel_id
                            ORDER BY tracks.downloaded_at DESC";
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
                reader.GetInt32(7) != 0,
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)));
        }
        return tracks;
    }

    public List<MusicTrack> GetUnanalyzedTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.id, tracks.canonical_url, tracks.title, tracks.file_name, tracks.rating_id, tracks.downloaded_at,
                                   tracks.duration_seconds, tracks.needs_reevaluation, channels.name, channels.source_url, tracks.uploaded_at
                            FROM tracks
                            LEFT JOIN channels ON channels.id = tracks.channel_id
                            LEFT JOIN track_analysis analysis ON analysis.track_id = tracks.id
                            WHERE analysis.id IS NULL
                            ORDER BY tracks.downloaded_at, tracks.id";
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
                reader.GetInt32(7) != 0,
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)));
        }
        return tracks;
    }

    public Dictionary<int, List<int>> GetAllTrackGenreIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT track_id, genre_id FROM track_genres WHERE is_enabled = 1";
        return ReadTrackIdMap(cmd);
    }

    public List<int> GetTrackGenreIds(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT genre_id FROM track_genres WHERE track_id = $trackId AND is_enabled = 1";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var genreIds = new List<int>();
        while (reader.Read()) genreIds.Add(reader.GetInt32(0));
        return genreIds;
    }

    public List<TrackModelGenre> GetTrackModelGenres(int trackId)
    {
        using var conn = Open(); using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT track_genres.genre_id, msg.name, track_genres.is_enabled, mg.name, msg.name, predictions.score
                            FROM track_genres
                            JOIN model_subgenres msg ON msg.id = track_genres.genre_id
                            JOIN model_genres mg ON mg.id = msg.model_genre_id
                            LEFT JOIN track_analysis analysis ON analysis.track_id = track_genres.track_id
                            LEFT JOIN track_genre_predictions predictions ON predictions.track_analysis_id = analysis.id AND predictions.model_subgenre_id = msg.id
                            WHERE track_genres.track_id = $trackId
                            ORDER BY predictions.score IS NULL, predictions.score DESC, msg.name";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var groups = new Dictionary<int, (string Name, bool Enabled, List<ModelGenreReason> Reasons)>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            if (!groups.TryGetValue(id, out var group)) groups[id] = group = (reader.GetString(1), reader.GetInt32(2) != 0, []);
            if (!reader.IsDBNull(5))
                group.Reasons.Add(new ModelGenreReason(reader.GetString(3), reader.GetString(4), reader.GetDouble(5)));
        }
        return groups.Select(x => new TrackModelGenre(x.Key, x.Value.Name, x.Value.Enabled, x.Value.Reasons)).ToList();
    }

    public void SetTrackModelGenreEnabled(int trackId, int genreId, bool isEnabled)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        if (isEnabled)
        {
            cmd.CommandText = @"
                INSERT INTO track_genres (track_id, genre_id, assigned_at, is_enabled)
                VALUES ($trackId, $genreId, $assignedAt, 1)
                ON CONFLICT(track_id, genre_id) DO UPDATE SET is_enabled = 1, assigned_at = excluded.assigned_at";
            cmd.Parameters.AddWithValue("$assignedAt", DateTime.UtcNow.ToString("O"));
        }
        else
        {
            cmd.CommandText = @"UPDATE track_genres SET is_enabled = 0 WHERE track_id = $trackId AND genre_id = $genreId";
        }
        cmd.Parameters.AddWithValue("$trackId", trackId);
        cmd.Parameters.AddWithValue("$genreId", genreId);
        cmd.ExecuteNonQuery();
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
            "UPDATE tracks SET title = $title, rating_id = $ratingId, updated_at = $updatedAt, needs_reevaluation = 0 WHERE id = $id",
            ("$id", id), ("$title", title), ("$ratingId", ratingId), ("$updatedAt", now));

        tx.Commit();
    }

    public void SetTrackNeedsReview(int id, bool needsReview)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tracks SET needs_reevaluation = $needsReview WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$needsReview", needsReview ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTrack(int id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        // Older import_queue_items tables were created without ON DELETE SET NULL.
        // Detach queued/history rows explicitly so deleting a library track does not
        // fail with a foreign-key constraint.
        if (TableExists(conn, "import_queue_items"))
        {
            ExecuteInsert(conn, tx,
                "UPDATE import_queue_items SET track_id = NULL WHERE track_id = $id",
                ("$id", id));
        }

        if (TableExists(conn, "track_tags"))
            ExecuteInsert(conn, tx, "DELETE FROM track_tags WHERE track_id = $id", ("$id", id));
        if (TableExists(conn, "track_genres"))
            ExecuteInsert(conn, tx, "DELETE FROM track_genres WHERE track_id = $id", ("$id", id));
        if (TableExists(conn, "track_analysis"))
        {
            if (TableExists(conn, "track_genre_predictions"))
                ExecuteInsert(conn, tx, @"DELETE FROM track_genre_predictions
                    WHERE track_analysis_id IN (SELECT id FROM track_analysis WHERE track_id = $id)", ("$id", id));
            if (TableExists(conn, "track_analysis_signals"))
                ExecuteInsert(conn, tx, @"DELETE FROM track_analysis_signals
                    WHERE track_analysis_id IN (SELECT id FROM track_analysis WHERE track_id = $id)", ("$id", id));
            if (TableExists(conn, "track_derived_attributes"))
                ExecuteInsert(conn, tx, @"DELETE FROM track_derived_attributes
                    WHERE track_analysis_id IN (SELECT id FROM track_analysis WHERE track_id = $id)", ("$id", id));
            ExecuteInsert(conn, tx, "DELETE FROM track_analysis WHERE track_id = $id", ("$id", id));
        }

        ExecuteInsert(conn, tx, "DELETE FROM tracks WHERE id = $id", ("$id", id));
        tx.Commit();
    }

    public List<Genre> GetGenres()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT model_subgenres.id, model_genres.name || ' → ' || model_subgenres.name
                            FROM model_subgenres
                            JOIN model_genres ON model_genres.id = model_subgenres.model_genre_id
                            ORDER BY model_genres.name, model_subgenres.name";
        return ReadLookupList(cmd, (id, name) => new Genre(id, name));
    }

    public List<TagCategory> GetTagCategories()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, color, sort_order FROM tag_categories ORDER BY sort_order, name";
        using var reader = cmd.ExecuteReader();
        var categories = new List<TagCategory>();
        while (reader.Read())
            categories.Add(new TagCategory(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetInt32(3)));
        return categories;
    }

    public void AddTagCategory(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO tag_categories (key, name, sort_order)
                            VALUES ($key, $name, COALESCE((SELECT MAX(sort_order) + 10 FROM tag_categories), 10))";
        cmd.Parameters.AddWithValue("$key", SlugKey(name));
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.ExecuteNonQuery();
    }

    public void RenameTagCategory(int id, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tag_categories SET key = $key, name = $name WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$key", SlugKey(name));
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.ExecuteNonQuery();
    }

    public void SetTagCategoryColor(int id, string? color)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tag_categories SET color = $color WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$color", string.IsNullOrWhiteSpace(color) ? DBNull.Value : color.Trim());
        cmd.ExecuteNonQuery();
    }

    public string? DeleteTagCategoryIfUnused(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM tags WHERE category_id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        var tags = (long)cmd.ExecuteScalar()!;
        if (tags > 0)
            return $"Cannot delete: category still contains {tags} tag(s).";

        cmd.CommandText = "DELETE FROM tag_categories WHERE id = $id";
        cmd.ExecuteNonQuery();
        return null;
    }

    public List<Tag> GetTags()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT tags.id, tags.category_id, categories.name, categories.color, tags.name, tags.description
            FROM tags
            JOIN tag_categories categories ON categories.id = tags.category_id
            ORDER BY categories.sort_order, categories.name, tags.name";
        return ReadTags(cmd);
    }

    public void AddTag(int categoryId, string name, string? description)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO tags (category_id, name, description)
                            VALUES ($categoryId, $name, $description)";
        cmd.Parameters.AddWithValue("$categoryId", categoryId);
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.Parameters.AddWithValue("$description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
        cmd.ExecuteNonQuery();
    }

    public void RenameTag(int id, string name, string? description)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tags SET name = $name, description = $description WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.Parameters.AddWithValue("$description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
        cmd.ExecuteNonQuery();
    }

    public string? DeleteTagIfUnused(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM track_tags WHERE tag_id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        var tracks = reader.GetInt64(0);
        if (tracks > 0)
            return $"Cannot delete: used by {tracks} track(s).";
        reader.Close();

        cmd.CommandText = "DELETE FROM tags WHERE id = $id";
        cmd.ExecuteNonQuery();
        return null;
    }

    public Dictionary<int, List<int>> GetAllTrackTagIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT track_id, tag_id FROM track_tags";
        return ReadTrackIdMap(cmd);
    }

    public List<int> GetTrackTagIds(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tag_id FROM track_tags WHERE track_id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var ids = new List<int>();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        return ids;
    }

    public List<TrackTag> GetTrackTags(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT tags.id, tags.category_id, categories.name, categories.color, tags.name, tags.description,
                   track_tags.source, track_tags.strength, track_tags.confidence
            FROM track_tags
            JOIN tags ON tags.id = track_tags.tag_id
            JOIN tag_categories categories ON categories.id = tags.category_id
            WHERE track_tags.track_id = $trackId
            ORDER BY categories.sort_order, categories.name, tags.name";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var tags = new List<TrackTag>();
        while (reader.Read())
            tags.Add(new TrackTag(
                reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDouble(7),
                reader.IsDBNull(8) ? null : reader.GetDouble(8)));
        return tags;
    }

    public void SetTrackManualTags(int trackId, IReadOnlyCollection<int> tagIds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        ExecuteInsert(conn, tx, "DELETE FROM track_tags WHERE track_id = $trackId AND source = 'manual'",
            ("$trackId", trackId));

        foreach (var tagId in tagIds.Distinct())
        {
            ExecuteInsert(conn, tx, @"
                INSERT INTO track_tags (track_id, tag_id, source, strength, confidence, assigned_at, updated_at)
                VALUES ($trackId, $tagId, 'manual', 1, 1, $now, $now)
                ON CONFLICT(track_id, tag_id) DO UPDATE SET
                    source = 'manual',
                    strength = excluded.strength,
                    confidence = excluded.confidence,
                    updated_at = excluded.updated_at",
                ("$trackId", trackId), ("$tagId", tagId), ("$now", now));
        }

        tx.Commit();
    }

    public List<TagSignalSource> GetTagSignalSources()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT model_name, signal_key, MAX(description)
            FROM track_analysis_signals
            GROUP BY model_name, signal_key
            ORDER BY model_name, signal_key";
        using var reader = cmd.ExecuteReader();
        var sources = new List<TagSignalSource>();
        while (reader.Read())
            sources.Add(new TagSignalSource(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        return sources;
    }

    public List<TagRuleGroup> GetTagRuleGroups()
    {
        return [];
    }

    public int CreateTagRuleGroup(int tagId, TagRuleMatchMode matchMode, string sourceType, string sourceKey, double threshold)
    {
        return 0;
    }

    public void AddTagRuleCondition(int groupId, string sourceType, string sourceKey, double threshold)
    {
    }

    public void DeleteTagRuleCondition(int conditionId)
    {
    }

    public void SetTagRuleGroupEnabled(int groupId, bool enabled)
    {
    }

    public void SetTagRuleGroupMatchMode(int groupId, TagRuleMatchMode matchMode)
    {
    }

    public void DeleteTagRuleGroup(int groupId)
    {
    }

    public void RefreshAllTagSuggestions()
    {
    }

    public List<TrackTagSuggestion> GetTrackTagSuggestions(int trackId)
    {
        return [];
    }

    public void AcceptTrackTagSuggestion(int trackId, int tagId, int ruleGroupId)
    {
    }

    public void RejectTrackTagSuggestion(int trackId, int ruleGroupId)
    {
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
        cmd.CommandText = @"SELECT id, model_genre_id, name, description, classification_hint, bpm_min, bpm_max
                            FROM model_subgenres
                            WHERE $modelGenreId IS NULL OR model_genre_id = $modelGenreId
                            ORDER BY name";
        cmd.Parameters.AddWithValue("$modelGenreId", modelGenreId is null ? DBNull.Value : modelGenreId.Value);
        using var reader = cmd.ExecuteReader();
        var subgenres = new List<ModelSubgenre>();
        while (reader.Read()) subgenres.Add(new ModelSubgenre(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetInt32(5),
            reader.IsDBNull(6) ? null : reader.GetInt32(6)));
        return subgenres;
    }

    public void AddModelSubgenre(int modelGenreId, string name)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"
            INSERT INTO model_subgenres (model_genre_id, name)
            VALUES ($modelGenreId, $name)",
            ("$modelGenreId", modelGenreId), ("$name", name.Trim()));
    }

    public void UpdateModelSubgenre(int id, string name, string? description, string? classificationHint, int? bpmMin, int? bpmMax)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"
            UPDATE model_subgenres
            SET name = $name,
                description = $description,
                classification_hint = $hint,
                bpm_min = $bpmMin,
                bpm_max = $bpmMax
            WHERE id = $id",
            ("$id", id),
            ("$name", name.Trim()),
            ("$description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim()),
            ("$hint", string.IsNullOrWhiteSpace(classificationHint) ? DBNull.Value : classificationHint.Trim()),
            ("$bpmMin", bpmMin is null ? DBNull.Value : bpmMin.Value),
            ("$bpmMax", bpmMax is null ? DBNull.Value : bpmMax.Value));
    }

    public List<ModelSubgenreDistinction> GetModelSubgenreDistinctions()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT distinctions.model_subgenre_id, distinctions.distinguish_from_model_subgenre_id,
                   genres.name, subgenres.name, distinctions.difference
            FROM model_subgenre_distinctions distinctions
            JOIN model_subgenres subgenres ON subgenres.id = distinctions.distinguish_from_model_subgenre_id
            JOIN model_genres genres ON genres.id = subgenres.model_genre_id
            ORDER BY distinctions.model_subgenre_id, genres.name, subgenres.name";
        using var reader = cmd.ExecuteReader();
        var distinctions = new List<ModelSubgenreDistinction>();
        while (reader.Read()) distinctions.Add(new ModelSubgenreDistinction(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return distinctions;
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

    public TrackAudioAnalysis? GetTrackAudioAnalysis(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT bpm, integrated_loudness, loudness_range
            FROM track_analysis
            WHERE track_id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new TrackAudioAnalysis(
            reader.IsDBNull(0) ? null : reader.GetDouble(0),
            reader.IsDBNull(1) ? null : reader.GetDouble(1),
            reader.IsDBNull(2) ? null : reader.GetDouble(2));
    }

    public List<ExperimentalAnalysisModel> GetTrackAnalysisSignals(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT model_family, category, model_name, model_type, description, signal_key, score
            FROM track_analysis_signals signals
            JOIN track_analysis analysis ON analysis.id = signals.track_analysis_id
            WHERE analysis.track_id = $trackId
            ORDER BY model_family, category, model_name, score DESC";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var grouped = new Dictionary<(string Family, string Category, string Model, string Type, string Description), List<ExperimentalAnalysisValue>>();
        while (reader.Read())
        {
            var key = (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4));
            if (!grouped.TryGetValue(key, out var values)) grouped[key] = values = [];
            values.Add(new ExperimentalAnalysisValue(reader.GetString(5), reader.GetDouble(6)));
        }
        return grouped.Select(item => new ExperimentalAnalysisModel(
            item.Key.Family, item.Key.Category, item.Key.Model, item.Key.Type, item.Key.Description, item.Value)).ToList();
    }

    private static List<ExperimentalAnalysisModel> GetTrackAnalysisSignals(SqliteConnection conn, SqliteTransaction tx, long analysisId)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT model_family, category, model_name, model_type, description, signal_key, score
            FROM track_analysis_signals
            WHERE track_analysis_id = $analysisId
            ORDER BY model_family, category, model_name, score DESC";
        cmd.Parameters.AddWithValue("$analysisId", analysisId);
        using var reader = cmd.ExecuteReader();
        var grouped = new Dictionary<(string Family, string Category, string Model, string Type, string Description), List<ExperimentalAnalysisValue>>();
        while (reader.Read())
        {
            var key = (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4));
            if (!grouped.TryGetValue(key, out var values)) grouped[key] = values = [];
            values.Add(new ExperimentalAnalysisValue(reader.GetString(5), reader.GetDouble(6)));
        }
        return grouped.Select(item => new ExperimentalAnalysisModel(
            item.Key.Family, item.Key.Category, item.Key.Model, item.Key.Type, item.Key.Description, item.Value)).ToList();
    }

    private static void RebuildDerivedAttributes(SqliteConnection conn)
    {
        if (!TableExists(conn, "track_analysis") || !TableExists(conn, "track_derived_attributes"))
            return;

        var analysisIds = new List<long>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id FROM track_analysis";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) analysisIds.Add(reader.GetInt64(0));
        }

        using var tx = conn.BeginTransaction();
        foreach (var analysisId in analysisIds)
        {
            ExecuteInsert(conn, tx,
                "DELETE FROM track_derived_attributes WHERE track_analysis_id = $analysisId",
                ("$analysisId", analysisId));
            SaveDerivedAttributes(conn, tx, analysisId, DeriveAttributes(GetTrackAnalysisSignals(conn, tx, analysisId)));
        }
        tx.Commit();
    }

    public List<DerivedTrackAttribute> GetTrackDerivedAttributes(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT attribute_key, system_value, system_score, manual_value
                            FROM track_derived_attributes attributes
                            JOIN track_analysis analysis ON analysis.id = attributes.track_analysis_id
                            WHERE analysis.track_id = $trackId ORDER BY attribute_key";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var result = new List<DerivedTrackAttribute>();
        while (reader.Read()) result.Add(new DerivedTrackAttribute(
            reader.GetString(0), reader.GetString(1), reader.GetDouble(2),
            reader.IsDBNull(3) ? null : reader.GetString(3)));
        return result;
    }

    public void SetTrackDerivedAttributeOverride(int trackId, string key, string? manualValue)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE track_derived_attributes SET manual_value = $manualValue
                            WHERE track_analysis_id = (SELECT id FROM track_analysis WHERE track_id = $trackId)
                            AND attribute_key = $key";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$manualValue", (object?)manualValue ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static void SaveDerivedAttributes(SqliteConnection conn, SqliteTransaction tx, long analysisId,
        IEnumerable<(string Key, string Value, double Score)> attributes)
    {
        foreach (var attribute in attributes)
            ExecuteInsert(conn, tx, @"INSERT INTO track_derived_attributes
                (track_analysis_id, attribute_key, system_value, system_score)
                VALUES ($analysisId, $key, $value, $score)
                ON CONFLICT(track_analysis_id, attribute_key) DO UPDATE SET
                    system_value = excluded.system_value, system_score = excluded.system_score",
                ("$analysisId", analysisId), ("$key", attribute.Key), ("$value", attribute.Value), ("$score", attribute.Score));
    }

    private static List<(string Key, string Value, double Score)> DeriveAttributes(IReadOnlyList<ExperimentalAnalysisModel> models)
    {
        double Signal(string model, string label) => models.FirstOrDefault(item => item.Model == model)?.Values
            .FirstOrDefault(value => value.Label == label)?.Score ?? 0;
        double SignalStartingWith(string model, string labelPrefix) => models.FirstOrDefault(item => item.Model == model)?.Values
            .FirstOrDefault(value => value.Label.StartsWith(labelPrefix, StringComparison.OrdinalIgnoreCase))?.Score ?? 0;
        var arousal = Math.Clamp((Signal("arousal_valence", "arousal") - 1d) / 8d, 0d, 1d);
        var valence = Math.Clamp((Signal("arousal_valence", "valence") - 1d) / 8d, 0d, 1d);
        var happy = Signal("mood happy", "happy");
        var sad = Signal("mood sad", "sad");
        var relaxed = Signal("mood relaxed", "relaxed");
        var aggressive = Signal("mood aggressive", "aggressive");
        var party = Signal("mood party", "party");
        var engagement = Signal("engagement_regression", "engagement");
        var danceable = Signal("danceability classifier", "danceable");
        var mirexReflective = SignalStartingWith("moods mirex", "literate, poignant");
        var melancholy = .65 * sad + .25 * mirexReflective + .10 * (1 - happy);
        var positive = .75 * happy + .15 * valence + .10 * party;
        var calm = .70 * relaxed + .20 * (1 - engagement) + .10 * (1 - arousal);
        var active = .35 * engagement + .22 * party + .18 * aggressive + .15 * danceable + .10 * arousal;
        var intense = .45 * aggressive + .30 * engagement + .15 * danceable + .10 * arousal;
        var intensity = Math.Clamp(active * (1 - .55 * calm), 0, 1);
        var vocal = Signal("voice/instrumental classifiers", "voice");
        return [
            ("intensity", intensity < .34 ? "Low" : intensity < .67 ? "Medium" : "High", intensity),
            ("emotional_tone", melancholy >= .55 && melancholy > positive + .10 ? "Melancholic" : positive >= .55 && positive > melancholy + .10 ? "Positive" : "Neutral", Math.Max(melancholy, positive)),
            ("energy_context", calm >= .55 && calm > active ? "Calm" : intense >= .65 && intense > calm ? "Intense" : "Driving", Math.Max(calm, Math.Max(active, intense))),
            ("vocal_presence", vocal > .67 ? "Vocal" : vocal < .33 ? "Instrumental" : "Mixed", vocal)
        ];
    }

    private static void RefreshAllModelGenres(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();
        RefreshModelGenres(conn, tx, null);
        tx.Commit();
    }

    private static void RefreshModelGenres(SqliteConnection conn, SqliteTransaction tx, int? trackId)
    {
        ExecuteInsert(conn, tx, @"
            DELETE FROM track_genres WHERE ($trackId IS NULL OR track_id = $trackId)
            AND NOT EXISTS (
                SELECT 1 FROM track_genre_predictions predictions JOIN track_analysis analysis ON analysis.id = predictions.track_analysis_id
                WHERE analysis.track_id = track_genres.track_id AND predictions.model_subgenre_id = track_genres.genre_id AND predictions.score > 0.25)",
            ("$trackId", trackId));
        ExecuteInsert(conn, tx, @"
            INSERT INTO track_genres (track_id, genre_id, assigned_at, is_enabled)
            SELECT DISTINCT analysis.track_id, predictions.model_subgenre_id, $assignedAt, 1
            FROM track_genre_predictions predictions
            JOIN track_analysis analysis ON analysis.id = predictions.track_analysis_id
            WHERE predictions.score > 0.25 AND ($trackId IS NULL OR analysis.track_id = $trackId)
            ON CONFLICT(track_id, genre_id) DO UPDATE SET assigned_at = excluded.assigned_at",
            ("$trackId", trackId), ("$assignedAt", DateTime.UtcNow.ToString("O")));
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

    private static List<Tag> ReadTags(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        var tags = new List<Tag>();
        while (reader.Read())
            tags.Add(new Tag(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        return tags;
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

    private static void ExecuteNonQuery(SqliteConnection conn, string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
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

    private static long SelectId(SqliteConnection conn, SqliteTransaction tx, string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);
        return (long)cmd.ExecuteScalar()!;
    }

    private static long? FindTagId(SqliteConnection conn, SqliteTransaction tx, string categoryName, string tagName)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT tags.id
            FROM tags
            JOIN tag_categories categories ON categories.id = tags.category_id
            WHERE categories.name = $categoryName AND tags.name = $tagName";
        cmd.Parameters.AddWithValue("$categoryName", categoryName);
        cmd.Parameters.AddWithValue("$tagName", tagName);
        return cmd.ExecuteScalar() is long id ? id : null;
    }

    private static void AddParameters(SqliteCommand cmd, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static string SlugKey(string name) => string.Concat(name.Trim().ToLowerInvariant()
        .Select(character => char.IsLetterOrDigit(character) ? character : '-'))
        .Trim('-');

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

    private sealed record RatingSeedDocument(List<RatingSeed> Ratings);
    private sealed record RatingSeed(string Name, int SortOrder);
    private sealed record LookupSeedDocument(
        [property: JsonPropertyName("tagCategories")] List<LookupTagCategorySeed> TagCategories);
    private sealed record LookupTagCategorySeed(
        string Key,
        string Name,
        string? Color,
        [property: JsonPropertyName("sort_order")] int SortOrder,
        List<LookupTagSeed> Tags);
    private sealed record LookupTagSeed(string Name, string? Description);
    private sealed record ModelSubgenreMetadataSeed(
        string Label,
        string Genre,
        string Subgenre,
        string? Description,
        string? ClassificationHint,
        int? BpmMin,
        int? BpmMax,
        List<ModelSubgenreDistinctionSeed> DistinguishFrom);
    private sealed record ModelSubgenreDistinctionSeed(string Label, string Difference);
}
