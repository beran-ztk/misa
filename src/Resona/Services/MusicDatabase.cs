using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform;
using Microsoft.Data.Sqlite;
using Resona.Models;

namespace Resona.Services;

public class MusicDatabase
{
    private const string AssetBaseUri = "avares://Resona/Assets/";
    private const string RemovedMoodThemeModelName = "mtg_" + "jamen" + "do_" + "mood" + "theme";
    private readonly string _connectionString = $"Data Source={Values.DbPath};Default Timeout=30";

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA busy_timeout = 30000;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public void Initialize()
    {
        if (File.Exists(Values.DbPath))
        {
            using var existingConnection = Open();
            EnableConcurrentReads(existingConnection);
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

    private static void EnableConcurrentReads(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        command.ExecuteScalar();
        command.CommandText = "PRAGMA synchronous = NORMAL;";
        command.ExecuteNonQuery();
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
                inform_new_songs    INTEGER NOT NULL DEFAULT 0,
                subscribed          INTEGER NOT NULL DEFAULT 0,
                created_at          TEXT NULL,
                updated_at          TEXT NULL,
                last_checked_at     TEXT NULL,
                video_count         INTEGER NOT NULL DEFAULT 0,
                auto_download       INTEGER NOT NULL DEFAULT 0,
                follower_count      INTEGER NULL,
                thumbnail_url       TEXT NULL,
                thumbnail           BLOB NULL,
                remote_metadata_updated_at TEXT NULL,
                basic_metadata_checked_at TEXT NULL,
                max_duration_minutes INTEGER NULL,
                auto_download_from  TEXT NULL
            );

            CREATE TABLE channel_videos (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                channel_id          INTEGER NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
                video_id            TEXT NOT NULL UNIQUE,
                canonical_url       TEXT NOT NULL UNIQUE,
                title               TEXT NOT NULL,
                duration_seconds    INTEGER NULL,
                uploaded_at         TEXT NULL,
                discovered_at       TEXT NOT NULL,
                updated_at          TEXT NOT NULL,
                is_checked          INTEGER NOT NULL DEFAULT 0,
                download_status     TEXT NOT NULL DEFAULT 'Queued',
                download_error      TEXT NULL,
                download_attempts   INTEGER NOT NULL DEFAULT 0,
                manual_download_requested INTEGER NOT NULL DEFAULT 0,
                metadata_status     TEXT NOT NULL DEFAULT 'Pending',
                metadata_updated_at TEXT NULL,
                metadata_error      TEXT NULL,
                metadata_attempts   INTEGER NOT NULL DEFAULT 0,
                metadata_priority   INTEGER NOT NULL DEFAULT 0,
                view_count          INTEGER NULL,
                like_count          INTEGER NULL,
                thumbnail_url       TEXT NULL
            );

            CREATE TABLE channel_notifications (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                channel_id          INTEGER NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
                channel_video_id    INTEGER NULL REFERENCES channel_videos(id) ON DELETE CASCADE,
                kind                TEXT NOT NULL,
                title               TEXT NOT NULL,
                created_at          TEXT NOT NULL,
                read_at             TEXT NULL,
                archived_at         TEXT NULL,
                UNIQUE(channel_video_id, kind)
            );
            CREATE INDEX ix_channel_notifications_inbox
                ON channel_notifications(archived_at, read_at, created_at DESC);

            CREATE TABLE ratings (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT NOT NULL UNIQUE,
                sort_order  INTEGER NOT NULL UNIQUE
            );

            CREATE TABLE tags (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                name         TEXT NOT NULL UNIQUE
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
                thumbnail           BLOB NULL,
                library_state       TEXT NOT NULL DEFAULT 'Active',
                source_video_id     TEXT NULL,
                view_count          INTEGER NULL,
                like_count          INTEGER NULL,
                source_thumbnail_url TEXT NULL,
                source_metadata_updated_at TEXT NULL,
                analysis_disabled   INTEGER NOT NULL DEFAULT 0,
                is_public           INTEGER NOT NULL DEFAULT 1,
                needs_reevaluation  INTEGER NOT NULL DEFAULT 0,
                notes               TEXT NULL
            );
            CREATE INDEX ix_tracks_library_state ON tracks(library_state, downloaded_at DESC);

            CREATE TABLE track_genres (
                track_id                 INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                genre_id                 INTEGER NOT NULL REFERENCES model_subgenres(id),
                assigned_at              TEXT NOT NULL,
                is_enabled               INTEGER NOT NULL DEFAULT 1,
                is_manual                INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (track_id, genre_id)
            );

            CREATE TABLE track_tags (
                track_id     INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                tag_id       INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
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
            CREATE INDEX ix_channel_videos_channel_checked ON channel_videos(channel_id, is_checked, uploaded_at);
            CREATE INDEX ix_channel_videos_metadata_queue
                ON channel_videos(metadata_status, metadata_updated_at, id DESC);
            CREATE INDEX ix_tracks_channel_state ON tracks(channel_id, library_state);
            CREATE INDEX ix_track_tags_tag_id ON track_tags(tag_id);
            CREATE INDEX ix_model_subgenres_model_genre_id ON model_subgenres(model_genre_id);
            CREATE INDEX ix_model_subgenre_distinctions_source ON model_subgenre_distinctions(model_subgenre_id);
            CREATE INDEX ix_track_genre_predictions_analysis_id ON track_genre_predictions(track_analysis_id);
            CREATE INDEX ix_track_analysis_signals_analysis_id ON track_analysis_signals(track_analysis_id);
            CREATE TABLE portable_exports (
                id                    INTEGER PRIMARY KEY AUTOINCREMENT,
                export_id             TEXT NOT NULL UNIQUE,
                schema_version        INTEGER NOT NULL,
                exported_at           TEXT NOT NULL,
                track_count_total     INTEGER NOT NULL,
                new_track_count       INTEGER NOT NULL,
                cutoff_downloaded_at  TEXT NULL,
                archive_path          TEXT NULL
            );
            ";
        cmd.ExecuteNonQuery();
    }

    private static void ApplyMigrations(SqliteConnection conn)
    {
        EnsureColumn(conn, "tracks", "listened_seconds", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "tracks", "file_size_bytes", "INTEGER NULL");
        EnsureColumn(conn, "tracks", "download_duration_ms", "INTEGER NULL");
        EnsureColumn(conn, "tracks", "thumbnail", "BLOB NULL");
        EnsureColumn(conn, "tracks", "library_state", "TEXT NOT NULL DEFAULT 'Active'");
        EnsureColumn(conn, "tracks", "source_video_id", "TEXT NULL");
        EnsureColumn(conn, "tracks", "view_count", "INTEGER NULL");
        EnsureColumn(conn, "tracks", "like_count", "INTEGER NULL");
        EnsureColumn(conn, "tracks", "source_thumbnail_url", "TEXT NULL");
        EnsureColumn(conn, "tracks", "source_metadata_updated_at", "TEXT NULL");
        ExecuteNonQuery(conn,
            "CREATE INDEX IF NOT EXISTS ix_tracks_library_state ON tracks(library_state, downloaded_at DESC)");
        EnsureColumn(conn, "tracks", "analysis_disabled", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "tracks", "is_public", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(conn, "track_analysis", "analysis_duration_ms", "INTEGER NULL");
        EnsureColumn(conn, "model_subgenres", "description", "TEXT NULL");
        EnsureColumn(conn, "model_subgenres", "classification_hint", "TEXT NULL");
        EnsureColumn(conn, "model_subgenres", "bpm_min", "INTEGER NULL");
        EnsureColumn(conn, "model_subgenres", "bpm_max", "INTEGER NULL");
        EnsureTrackGenreSourceSchema(conn);
        RenameLegacySkipRating(conn);
        EnsureChannelSubscriptionSchema(conn);
        CreateImportQueueSchema(conn);
        CreateModelMetadataSchema(conn);
        CreateTagSchema(conn);
        SimplifyTagSchemaIfNeeded(conn);
        CreatePortableExportSchema(conn);
    }

    private static void EnsureTrackGenreSourceSchema(SqliteConnection conn)
    {
        if (ColumnExists(conn, "track_genres", "is_manual"))
            return;

        using var tx = conn.BeginTransaction();
        ExecuteInsert(conn, tx, "ALTER TABLE track_genres ADD COLUMN is_manual INTEGER NOT NULL DEFAULT 0");
        ExecuteInsert(conn, tx, @"
            UPDATE track_genres
            SET is_manual = 1
            WHERE NOT EXISTS (
                SELECT 1
                FROM track_analysis analysis
                JOIN track_genre_predictions predictions
                  ON predictions.track_analysis_id = analysis.id
                WHERE analysis.track_id = track_genres.track_id
                  AND predictions.model_subgenre_id = track_genres.genre_id
                  AND predictions.score > 0.25
            )");
        tx.Commit();
    }

    private static void RenameLegacySkipRating(SqliteConnection conn)
    {
        ExecuteNonQuery(conn, @"
            UPDATE ratings
            SET name = $avoid
            WHERE name = 'Skip'
              AND NOT EXISTS (SELECT 1 FROM ratings WHERE name = $avoid)",
            ("$avoid", RatingNames.Avoid));
    }

    private static void CreatePortableExportSchema(SqliteConnection conn)
    {
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS portable_exports (
                id                    INTEGER PRIMARY KEY AUTOINCREMENT,
                export_id             TEXT NOT NULL UNIQUE,
                schema_version        INTEGER NOT NULL,
                exported_at           TEXT NOT NULL,
                track_count_total     INTEGER NOT NULL,
                new_track_count       INTEGER NOT NULL,
                cutoff_downloaded_at  TEXT NULL,
                archive_path          TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_portable_exports_exported_at
                ON portable_exports(exported_at);");
    }

    private static void CreateTagSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE
            );
            CREATE TABLE IF NOT EXISTS track_tags (
                track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                PRIMARY KEY (track_id, tag_id)
            );
            CREATE INDEX IF NOT EXISTS ix_track_tags_tag_id ON track_tags(tag_id);
            ";
        cmd.ExecuteNonQuery();
    }

    private static void SimplifyTagSchemaIfNeeded(SqliteConnection conn)
    {
        if (!TableExists(conn, "tags") || !ColumnExists(conn, "tags", "category_id"))
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
                    name TEXT NOT NULL UNIQUE
                )");
            ExecuteInsert(conn, tx, @"
                INSERT INTO tags (id, name)
                SELECT MIN(id), name
                FROM tags_legacy
                GROUP BY name");

            ExecuteInsert(conn, tx, @"
                CREATE TABLE track_tags (
                    track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                    tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                    PRIMARY KEY (track_id, tag_id)
                )");
            ExecuteInsert(conn, tx, @"
                INSERT OR IGNORE INTO track_tags (track_id, tag_id)
                SELECT legacy_links.track_id, tags.id
                FROM track_tags_legacy legacy_links
                JOIN tags_legacy legacy_tags ON legacy_tags.id = legacy_links.tag_id
                JOIN tags ON tags.name = legacy_tags.name");

            ExecuteInsert(conn, tx, "DROP TABLE track_tags_legacy");
            ExecuteInsert(conn, tx, "DROP TABLE tags_legacy");
            ExecuteInsert(conn, tx, "DROP TABLE IF EXISTS tag_categories");
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

    private static void EnsureChannelSubscriptionSchema(SqliteConnection conn)
    {
        EnsureColumn(conn, "channels", "subscribed", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channels", "created_at", "TEXT NULL");
        EnsureColumn(conn, "channels", "updated_at", "TEXT NULL");
        EnsureColumn(conn, "channels", "last_checked_at", "TEXT NULL");
        EnsureColumn(conn, "channels", "video_count", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channels", "auto_download", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channels", "follower_count", "INTEGER NULL");
        EnsureColumn(conn, "channels", "thumbnail_url", "TEXT NULL");
        EnsureColumn(conn, "channels", "thumbnail", "BLOB NULL");
        EnsureColumn(conn, "channels", "remote_metadata_updated_at", "TEXT NULL");
        EnsureColumn(conn, "channels", "basic_metadata_checked_at", "TEXT NULL");
        EnsureColumn(conn, "channels", "max_duration_minutes", "INTEGER NULL");
        EnsureColumn(conn, "channels", "auto_download_from", "TEXT NULL");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS channel_videos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                channel_id INTEGER NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
                video_id TEXT NOT NULL UNIQUE,
                canonical_url TEXT NOT NULL UNIQUE,
                title TEXT NOT NULL,
                duration_seconds INTEGER NULL,
                uploaded_at TEXT NULL,
                discovered_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                is_checked INTEGER NOT NULL DEFAULT 0,
                download_status TEXT NOT NULL DEFAULT 'Queued',
                download_error TEXT NULL,
                download_attempts INTEGER NOT NULL DEFAULT 0,
                metadata_status TEXT NOT NULL DEFAULT 'Pending',
                metadata_updated_at TEXT NULL,
                metadata_error TEXT NULL,
                metadata_attempts INTEGER NOT NULL DEFAULT 0,
                metadata_priority INTEGER NOT NULL DEFAULT 0,
                view_count INTEGER NULL,
                like_count INTEGER NULL,
                thumbnail_url TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_channel_videos_channel_checked
                ON channel_videos(channel_id, is_checked, uploaded_at);
            CREATE TABLE IF NOT EXISTS channel_notifications (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                channel_id INTEGER NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
                channel_video_id INTEGER NULL REFERENCES channel_videos(id) ON DELETE CASCADE,
                kind TEXT NOT NULL,
                title TEXT NOT NULL,
                created_at TEXT NOT NULL,
                read_at TEXT NULL,
                archived_at TEXT NULL,
                UNIQUE(channel_video_id, kind)
            );
            CREATE INDEX IF NOT EXISTS ix_channel_notifications_inbox
                ON channel_notifications(archived_at, read_at, created_at DESC);";
        cmd.ExecuteNonQuery();
        EnsureColumn(conn, "channel_videos", "download_status", "TEXT NOT NULL DEFAULT 'Queued'");
        EnsureColumn(conn, "channel_videos", "download_error", "TEXT NULL");
        EnsureColumn(conn, "channel_videos", "download_attempts", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channel_videos", "manual_download_requested", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channel_videos", "metadata_status", "TEXT NOT NULL DEFAULT 'Pending'");
        EnsureColumn(conn, "channel_videos", "metadata_updated_at", "TEXT NULL");
        EnsureColumn(conn, "channel_videos", "metadata_error", "TEXT NULL");
        EnsureColumn(conn, "channel_videos", "metadata_attempts", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channel_videos", "metadata_priority", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(conn, "channel_videos", "view_count", "INTEGER NULL");
        EnsureColumn(conn, "channel_videos", "like_count", "INTEGER NULL");
        EnsureColumn(conn, "channel_videos", "thumbnail_url", "TEXT NULL");
        // Library-discovered/recommended channels must never download on their
        // own. Only an explicit Auto-download action on a followed channel may
        // enable this flag.
        ExecuteNonQuery(conn, @"
            UPDATE channels
            SET auto_download = 0, auto_download_from = NULL
            WHERE subscribed = 0 AND auto_download <> 0");
        // Follow is the single subscription switch. Keep legacy notification
        // flags aligned with it so non-followed channels cannot notify.
        ExecuteNonQuery(conn, @"
            UPDATE channels
            SET inform_new_songs = subscribed
            WHERE inform_new_songs <> subscribed");
        ExecuteNonQuery(conn, @"
            UPDATE channels
            SET basic_metadata_checked_at = COALESCE(remote_metadata_updated_at, last_checked_at)
            WHERE basic_metadata_checked_at IS NULL
              AND source_channel_id IS NOT NULL
              AND thumbnail IS NOT NULL
              AND length(thumbnail) > 0
              AND (remote_metadata_updated_at IS NOT NULL OR last_checked_at IS NOT NULL)");
        BackfillLibraryTrackChannels(conn);
        BackfillLibraryChannelVideos(conn);
        BackfillTrackSourceMetadata(conn);
    }

    private static void BackfillLibraryTrackChannels(SqliteConnection conn)
    {
        ExecuteNonQuery(conn, @"
            INSERT INTO channels
                (name, source_url, subscribed, inform_new_songs, auto_download, created_at, updated_at)
            SELECT MIN(TRIM(tracks.channel_name)), TRIM(tracks.channel_url), 0, 0, 0,
                   MIN(tracks.downloaded_at), MAX(tracks.updated_at)
            FROM tracks
            WHERE tracks.channel_id IS NULL
              AND tracks.channel_url IS NOT NULL
              AND TRIM(tracks.channel_url) <> ''
              AND tracks.channel_name IS NOT NULL
              AND TRIM(tracks.channel_name) <> ''
              AND NOT EXISTS (
                  SELECT 1 FROM channels
                  WHERE TRIM(channels.source_url) = TRIM(tracks.channel_url))
            GROUP BY TRIM(tracks.channel_url);

            UPDATE tracks
            SET channel_id = (
                SELECT channels.id
                FROM channels
                WHERE TRIM(channels.source_url) = TRIM(tracks.channel_url)
                ORDER BY channels.subscribed DESC, channels.id
                LIMIT 1)
            WHERE tracks.channel_id IS NULL
              AND tracks.channel_url IS NOT NULL
              AND TRIM(tracks.channel_url) <> ''
              AND EXISTS (
                  SELECT 1 FROM channels
                  WHERE TRIM(channels.source_url) = TRIM(tracks.channel_url));");
    }

    private static void BackfillTrackSourceMetadata(SqliteConnection conn)
    {
        ExecuteNonQuery(conn, @"
            UPDATE tracks
            SET source_video_id = COALESCE(source_video_id, (
                    SELECT CASE WHEN videos.video_id LIKE 'library-track-%' THEN NULL ELSE videos.video_id END
                    FROM channel_videos videos WHERE videos.canonical_url = tracks.canonical_url LIMIT 1)),
                view_count = COALESCE(view_count, (
                    SELECT videos.view_count FROM channel_videos videos
                    WHERE videos.canonical_url = tracks.canonical_url LIMIT 1)),
                like_count = COALESCE(like_count, (
                    SELECT videos.like_count FROM channel_videos videos
                    WHERE videos.canonical_url = tracks.canonical_url LIMIT 1)),
                source_thumbnail_url = COALESCE(source_thumbnail_url, (
                    SELECT videos.thumbnail_url FROM channel_videos videos
                    WHERE videos.canonical_url = tracks.canonical_url LIMIT 1)),
                source_metadata_updated_at = COALESCE(source_metadata_updated_at, (
                    SELECT videos.metadata_updated_at FROM channel_videos videos
                    WHERE videos.canonical_url = tracks.canonical_url AND videos.metadata_status = 'Ready' LIMIT 1))
            WHERE canonical_url IS NOT NULL
              AND EXISTS (SELECT 1 FROM channel_videos videos
                          WHERE videos.canonical_url = tracks.canonical_url)");
    }

    private static void BackfillLibraryChannelVideos(SqliteConnection conn)
    {
        using var insert = conn.CreateCommand();
        insert.CommandText = @"
            INSERT OR IGNORE INTO channel_videos
                (channel_id, video_id, canonical_url, title, duration_seconds, uploaded_at,
                 discovered_at, updated_at, is_checked, download_status, metadata_status)
            SELECT tracks.channel_id,
                   'library-track-' || tracks.id,
                   tracks.canonical_url,
                   tracks.title,
                   tracks.duration_seconds,
                   tracks.uploaded_at,
                   tracks.downloaded_at,
                   tracks.updated_at,
                   1,
                   'Ready',
                   'Pending'
            FROM tracks
            WHERE tracks.channel_id IS NOT NULL
              AND tracks.canonical_url IS NOT NULL
              AND TRIM(tracks.canonical_url) <> ''";
        insert.ExecuteNonQuery();

        using var resolveExisting = conn.CreateCommand();
        resolveExisting.CommandText = @"
            UPDATE channel_videos
            SET is_checked = CASE
                    WHEN video_id LIKE 'library-track-%'
                         OR EXISTS (
                             SELECT 1 FROM tracks
                             WHERE tracks.canonical_url = channel_videos.canonical_url
                               AND tracks.downloaded_at <= channel_videos.discovered_at)
                        THEN 1
                    ELSE is_checked
                END,
                download_status = 'Ready',
                download_error = NULL,
                metadata_status = CASE
                    WHEN channel_videos.metadata_status = 'Ready' THEN 'Ready'
                    WHEN channel_videos.metadata_status = 'Failed'
                         AND channel_videos.metadata_updated_at IS NOT NULL THEN 'Failed'
                    ELSE 'Pending'
                END,
                metadata_error = NULL,
                metadata_attempts = CASE
                    WHEN channel_videos.metadata_status IN ('Ready', 'Failed') THEN channel_videos.metadata_attempts
                    ELSE 0
                END,
                metadata_priority = 0
            WHERE EXISTS (
                SELECT 1
                FROM tracks
                WHERE tracks.canonical_url = channel_videos.canonical_url)
              AND ((is_checked <> 1
                    AND (video_id LIKE 'library-track-%'
                         OR EXISTS (
                             SELECT 1 FROM tracks
                             WHERE tracks.canonical_url = channel_videos.canonical_url
                               AND tracks.downloaded_at <= channel_videos.discovered_at)))
                   OR download_status <> 'Ready'
                   OR metadata_status IN ('Failed', 'Queued', 'Loading')
                   OR metadata_error IS NOT NULL
                   OR metadata_priority <> 0)";
        resolveExisting.ExecuteNonQuery();
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

    public Dictionary<int, TrackUsageStats> GetAllTrackUsageStats()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, listen_count, listened_seconds, skip_count, last_listened_at
                            FROM tracks";
        using var reader = cmd.ExecuteReader();
        var result = new Dictionary<int, TrackUsageStats>();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = new TrackUsageStats(
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4));
        }
        return result;
    }

    public PortableExportRecord? GetLastPortableExport()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, export_id, schema_version, exported_at, track_count_total,
                                   new_track_count, cutoff_downloaded_at, archive_path
                            FROM portable_exports
                            ORDER BY exported_at DESC, id DESC
                            LIMIT 1";
        using var reader = cmd.ExecuteReader();
        return reader.Read()
            ? new PortableExportRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7))
            : null;
    }

    public void RecordPortableExport(
        string exportId,
        int schemaVersion,
        string exportedAt,
        int trackCountTotal,
        int newTrackCount,
        string? cutoffDownloadedAt,
        string archivePath)
    {
        using var conn = Open();
        EnableConcurrentReads(conn);
        using var tx = conn.BeginTransaction();
        ExecuteInsert(conn, tx, @"
            INSERT INTO portable_exports
                (export_id, schema_version, exported_at, track_count_total, new_track_count, cutoff_downloaded_at, archive_path)
            VALUES
                ($exportId, $schemaVersion, $exportedAt, $trackCountTotal, $newTrackCount, $cutoffDownloadedAt, $archivePath)",
            ("$exportId", exportId),
            ("$schemaVersion", schemaVersion),
            ("$exportedAt", exportedAt),
            ("$trackCountTotal", trackCountTotal),
            ("$newTrackCount", newTrackCount),
            ("$cutoffDownloadedAt", cutoffDownloadedAt),
            ("$archivePath", archivePath));
        tx.Commit();
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

        SynchronizeModelMetadata(conn, tx);
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

    public ChannelRefreshResult SaveChannelSnapshot(YouTubeChannelSnapshot snapshot)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        var maxDurationSeconds = AppSettingsStore.Load().ChannelDownloadMaxDurationMinutes * 60;
        var channelId = FindChannelId(conn, tx, snapshot.ChannelId, snapshot.SourceUrl);
        var hadSnapshot = channelId is long existingChannelId
            && SelectId(conn, tx, @"
                SELECT CASE WHEN last_checked_at IS NOT NULL
                                  AND EXISTS (SELECT 1 FROM channel_videos WHERE channel_id = channels.id)
                            THEN 1 ELSE 0 END
                FROM channels WHERE id = $id", ("$id", existingChannelId)) != 0;

        if (channelId is null)
        {
            channelId = InsertAndGetId(conn, tx, @"
                INSERT INTO channels
                    (name, source_channel_id, source_url, subscribed, auto_download, created_at, updated_at,
                     last_checked_at, video_count, thumbnail_url, thumbnail, follower_count,
                     remote_metadata_updated_at, basic_metadata_checked_at)
                VALUES
                    ($name, $sourceChannelId, $sourceUrl, 1, 0, $now, $now, $now, $videoCount, $thumbnailUrl, $thumbnail,
                     $followerCount, $now, $now)",
                ("$name", snapshot.Name),
                ("$sourceChannelId", snapshot.ChannelId),
                ("$sourceUrl", snapshot.ChannelUrl ?? snapshot.SourceUrl),
                ("$thumbnailUrl", snapshot.ThumbnailUrl),
                ("$thumbnail", snapshot.Thumbnail),
                ("$followerCount", snapshot.FollowerCount),
                ("$now", now),
                ("$videoCount", snapshot.Videos.Count));
        }
        else
        {
            ExecuteInsert(conn, tx, @"
                UPDATE channels
                SET name = $name,
                    source_channel_id = COALESCE($sourceChannelId, source_channel_id),
                    source_url = COALESCE($sourceUrl, source_url),
                    auto_download = CASE WHEN subscribed = 0 THEN 0 ELSE auto_download END,
                    updated_at = $now,
                    last_checked_at = $now,
                    video_count = $videoCount
                    , thumbnail_url = COALESCE($thumbnailUrl, thumbnail_url)
                    , thumbnail = COALESCE($thumbnail, thumbnail)
                    , follower_count = COALESCE($followerCount, follower_count)
                    , remote_metadata_updated_at = $now
                    , basic_metadata_checked_at = $now
                WHERE id = $id",
                ("$id", channelId.Value),
                ("$name", snapshot.Name),
                ("$sourceChannelId", snapshot.ChannelId),
                ("$sourceUrl", snapshot.ChannelUrl ?? snapshot.SourceUrl),
                ("$thumbnailUrl", snapshot.ThumbnailUrl),
                ("$thumbnail", snapshot.Thumbnail),
                ("$followerCount", snapshot.FollowerCount),
                ("$now", now),
                ("$videoCount", snapshot.Videos.Count));
        }

        var added = 0;
        var updated = 0;
        foreach (var video in snapshot.Videos)
        {
            var existed = ChannelVideoExists(conn, tx, video.CanonicalUrl);
            ExecuteInsert(conn, tx, @"
                INSERT INTO channel_videos
                    (channel_id, video_id, canonical_url, title, duration_seconds, uploaded_at, discovered_at, updated_at,
                     is_checked, download_status)
                VALUES
                    ($channelId, $videoId, $canonicalUrl, $title, $duration, $uploadedAt, $now, $now, 0,
                     CASE
                         WHEN EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = $canonicalUrl) THEN 'Ready'
                         WHEN $duration IS NOT NULL AND $duration > COALESCE(
                             (SELECT max_duration_minutes * 60 FROM channels WHERE id = $channelId), $maxDuration)
                             THEN 'Skipped'
                         ELSE 'NotQueued'
                     END)
                ON CONFLICT(canonical_url) DO UPDATE SET
                    channel_id = excluded.channel_id,
                    video_id = excluded.video_id,
                    title = excluded.title,
                    duration_seconds = excluded.duration_seconds,
                    uploaded_at = COALESCE(excluded.uploaded_at, channel_videos.uploaded_at),
                    updated_at = excluded.updated_at,
                    metadata_status = CASE
                        WHEN channel_videos.metadata_status = 'Failed' THEN 'Pending'
                        ELSE channel_videos.metadata_status
                    END,
                    metadata_error = CASE
                        WHEN channel_videos.metadata_status = 'Failed' THEN NULL
                        ELSE channel_videos.metadata_error
                    END,
                    metadata_attempts = CASE
                        WHEN channel_videos.metadata_status = 'Failed' THEN 0
                        ELSE channel_videos.metadata_attempts
                    END,
                    download_status = CASE
                        WHEN EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = excluded.canonical_url) THEN 'Ready'
                        WHEN channel_videos.is_checked = 0 AND excluded.duration_seconds IS NOT NULL
                             AND excluded.duration_seconds > COALESCE(
                                 (SELECT max_duration_minutes * 60 FROM channels WHERE id = excluded.channel_id),
                                 $maxDuration) THEN 'Skipped'
                        WHEN channel_videos.metadata_status = 'Ready'
                             AND channel_videos.download_status IN ('NotQueued', 'Skipped')
                             AND EXISTS (SELECT 1 FROM channels WHERE id = excluded.channel_id
                                         AND auto_download = 1
                                         AND (auto_download_from IS NULL
                                              OR channel_videos.discovered_at >= auto_download_from))
                             AND channel_videos.is_checked = 0 THEN 'Queued'
                        ELSE channel_videos.download_status
                    END",
                ("$channelId", channelId.Value),
                ("$videoId", video.VideoId),
                ("$canonicalUrl", video.CanonicalUrl),
                ("$title", video.Title),
                ("$duration", video.DurationSeconds),
                ("$maxDuration", maxDurationSeconds),
                ("$uploadedAt", video.UploadedAt),
                ("$now", now));
            if (existed) updated++;
            else
            {
                added++;
                if (hadSnapshot)
                {
                    ExecuteInsert(conn, tx, @"
                        INSERT OR IGNORE INTO channel_notifications
                            (channel_id, channel_video_id, kind, title, created_at)
                        SELECT $channelId, id, 'NewVideo', title, $now
                        FROM channel_videos
                        WHERE canonical_url = $canonicalUrl
                          AND EXISTS (SELECT 1 FROM channels
                                      WHERE id = $channelId AND inform_new_songs = 1)",
                        ("$channelId", channelId.Value), ("$canonicalUrl", video.CanonicalUrl), ("$now", now));
                }
            }
        }

        tx.Commit();
        return new ChannelRefreshResult(true, added, updated);
    }

    public List<ChannelSubscription> GetChannelSubscriptions()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT channels.id, channels.name, channels.source_url, channels.source_channel_id,
                   channels.last_checked_at, COALESCE(COUNT(channel_videos.id), 0),
                   COALESCE(SUM(CASE WHEN channel_videos.is_checked = 0 THEN 1 ELSE 0 END), 0),
                   channels.auto_download,
                   COALESCE(SUM(CASE WHEN channel_videos.download_status = 'Queued' THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN channel_videos.download_status = 'Ready' THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN channel_videos.download_status = 'Downloading' THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN channel_videos.download_status = 'Failed' THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN channel_videos.download_status = 'Skipped' THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN channel_videos.download_status = 'NotQueued' THEN 1 ELSE 0 END), 0)
            FROM channels
            LEFT JOIN channel_videos ON channel_videos.channel_id = channels.id
            WHERE channels.subscribed = 1
            GROUP BY channels.id
            ORDER BY channels.name COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        var channels = new List<ChannelSubscription>();
        while (reader.Read())
            channels.Add(new ChannelSubscription(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                ReadInt32(reader, 5),
                ReadInt32(reader, 6),
                ReadInt32(reader, 7) != 0,
                ReadInt32(reader, 8),
                ReadInt32(reader, 9),
                ReadInt32(reader, 10),
                ReadInt32(reader, 11),
                ReadInt32(reader, 12),
                ReadInt32(reader, 13)));
        return channels;
    }

    public List<ChannelHubItem> GetChannelHubItems()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            WITH track_stats AS (
                SELECT tracks.channel_id,
                       COUNT(*) AS track_count,
                       SUM(CASE WHEN tracks.rating_id IS NOT NULL THEN 1 ELSE 0 END) AS rated_count,
                       AVG(CASE WHEN tracks.rating_id IS NOT NULL THEN ratings.sort_order END) AS average_rating,
                       SUM(CASE WHEN ratings.name = 'Favorite' THEN 1 ELSE 0 END) AS favorite_count,
                       SUM(CASE WHEN ratings.sort_order >= 4 THEN 1 ELSE 0 END) AS great_count,
                       SUM(tracks.listen_count) AS play_count,
                       SUM(tracks.skip_count) AS skip_count,
                       MAX(tracks.downloaded_at) AS last_downloaded_at
                FROM tracks
                LEFT JOIN ratings ON ratings.id = tracks.rating_id
                WHERE tracks.channel_id IS NOT NULL
                  AND tracks.library_state <> 'Rejected'
                GROUP BY tracks.channel_id
            ),
            video_stats AS (
                SELECT channel_id,
                       COUNT(*) AS video_count,
                       SUM(CASE WHEN is_checked = 0 THEN 1 ELSE 0 END) AS unchecked_count,
                       SUM(view_count) AS total_view_count
                FROM channel_videos
                GROUP BY channel_id
            )
            SELECT channels.id,
                   channels.name,
                   COALESCE(channels.source_url, ''),
                   channels.source_channel_id,
                   channels.subscribed,
                   channels.inform_new_songs,
                   channels.auto_download,
                   channels.last_checked_at,
                   COALESCE(track_stats.track_count, 0),
                   COALESCE(track_stats.rated_count, 0),
                   track_stats.average_rating,
                   COALESCE(track_stats.favorite_count, 0),
                   COALESCE(track_stats.great_count, 0),
                   COALESCE(track_stats.play_count, 0),
                   COALESCE(track_stats.skip_count, 0),
                   track_stats.last_downloaded_at,
                   COALESCE(video_stats.video_count, 0),
                   COALESCE(video_stats.unchecked_count, 0),
                   channels.follower_count,
                   video_stats.total_view_count,
                   channels.max_duration_minutes,
                   channels.auto_download_from,
                   channels.thumbnail,
                   channels.basic_metadata_checked_at
            FROM channels
            LEFT JOIN track_stats ON track_stats.channel_id = channels.id
            LEFT JOIN video_stats ON video_stats.channel_id = channels.id
            WHERE channels.subscribed = 1
               OR EXISTS (SELECT 1 FROM tracks
                          WHERE tracks.channel_id = channels.id
                            AND tracks.library_state <> 'Rejected')
            ORDER BY channels.name COLLATE NOCASE";

        using var reader = cmd.ExecuteReader();
        var rows = new List<ChannelHubItem>();
        while (reader.Read())
            rows.Add(new ChannelHubItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                ReadInt32(reader, 4) != 0,
                ReadInt32(reader, 5) != 0,
                ReadInt32(reader, 6) != 0,
                reader.IsDBNull(7) ? null : reader.GetString(7),
                ReadInt32(reader, 8),
                ReadInt32(reader, 9),
                reader.IsDBNull(10) ? null : reader.GetDouble(10),
                ReadInt32(reader, 11),
                ReadInt32(reader, 12),
                ReadInt32(reader, 13),
                ReadInt32(reader, 14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                ReadInt32(reader, 16),
                ReadInt32(reader, 17),
                reader.IsDBNull(18) ? null : reader.GetInt64(18),
                reader.IsDBNull(19) ? null : reader.GetInt64(19),
                reader.IsDBNull(20) ? null : reader.GetInt32(20),
                reader.IsDBNull(21) ? null : reader.GetString(21),
                [],
                reader.IsDBNull(22) ? null : (byte[])reader.GetValue(22),
                reader.IsDBNull(23) ? null : reader.GetString(23)));
        reader.Close();

        using var tracks = conn.CreateCommand();
        tracks.CommandText = @"
            SELECT tracks.channel_id, tracks.title
            FROM tracks
            LEFT JOIN ratings ON ratings.id = tracks.rating_id
            WHERE tracks.channel_id IS NOT NULL
              AND tracks.library_state = 'Active'
            ORDER BY tracks.channel_id,
                     COALESCE(ratings.sort_order, 0) DESC,
                     tracks.listen_count DESC,
                     tracks.downloaded_at DESC";
        using var trackReader = tracks.ExecuteReader();
        var topTracks = new Dictionary<int, List<string>>();
        while (trackReader.Read())
        {
            var channelId = trackReader.GetInt32(0);
            if (!topTracks.TryGetValue(channelId, out var titles))
                topTracks[channelId] = titles = [];
            if (titles.Count < 3)
                titles.Add(trackReader.GetString(1));
        }

        return rows
            .Select(channel => channel with
            {
                TopTracks = topTracks.TryGetValue(channel.Id, out var titles) ? titles : []
            })
            .ToList();
    }

    public void SetChannelFollowed(int channelId, bool followed)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        ExecuteInsert(conn, tx, @"
            UPDATE channels
            SET subscribed = $followed,
                inform_new_songs = $followed,
                auto_download = CASE
                    WHEN $followed = 0 OR subscribed = 0 THEN 0
                    ELSE auto_download
                END,
                updated_at = $now
            WHERE id = $id",
            ("$id", channelId), ("$followed", followed ? 1 : 0), ("$now", now));
        if (!followed)
        {
            ExecuteInsert(conn, tx, @"
                UPDATE channel_videos
                SET download_status = 'NotQueued', download_error = NULL, updated_at = $now
                WHERE channel_id = $id AND is_checked = 0
                  AND download_status IN ('Queued', 'Failed', 'Skipped')
                  AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url)",
                ("$id", channelId), ("$now", now));
            ExecuteInsert(conn, tx, @"
                UPDATE channel_videos
                SET metadata_status = 'Pending', metadata_error = NULL, metadata_priority = 0
                WHERE channel_id = $id AND metadata_status = 'Queued'",
                ("$id", channelId));
        }
        tx.Commit();
    }

    public void MarkChannelBasicMetadataChecked(int channelId)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"
            UPDATE channels
            SET basic_metadata_checked_at = $now, updated_at = $now
            WHERE id = $id",
            ("$id", channelId), ("$now", DateTime.UtcNow.ToString("O")));
    }

    public List<ChannelNotification> GetChannelNotifications()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT notifications.id, notifications.channel_id, notifications.channel_video_id,
                   channels.name, notifications.title, notifications.created_at,
                   notifications.read_at IS NOT NULL, videos.canonical_url
            FROM channel_notifications notifications
            JOIN channels ON channels.id = notifications.channel_id
            LEFT JOIN channel_videos videos ON videos.id = notifications.channel_video_id
            WHERE notifications.archived_at IS NULL
            ORDER BY notifications.read_at IS NULL DESC, notifications.created_at DESC, notifications.id DESC";
        using var reader = cmd.ExecuteReader();
        var notifications = new List<ChannelNotification>();
        while (reader.Read())
            notifications.Add(new ChannelNotification(
                reader.GetInt32(0), reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.GetBoolean(6), reader.IsDBNull(7) ? null : reader.GetString(7)));
        return notifications;
    }

    public int GetUnreadChannelNotificationCount()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(*) FROM channel_notifications
                            WHERE archived_at IS NULL AND read_at IS NULL";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void MarkChannelNotificationRead(int notificationId)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"UPDATE channel_notifications
                                SET read_at = COALESCE(read_at, $now) WHERE id = $id",
            ("$id", notificationId), ("$now", DateTime.UtcNow.ToString("O")));
    }

    public void ArchiveChannelNotification(int notificationId)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"UPDATE channel_notifications
                                SET archived_at = $now, read_at = COALESCE(read_at, $now) WHERE id = $id",
            ("$id", notificationId), ("$now", DateTime.UtcNow.ToString("O")));
    }

    public List<ChannelVideo> GetChannelVideos(int channelId, bool uncheckedFirst = true)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            SELECT videos.id, videos.channel_id, videos.video_id, videos.canonical_url, videos.title,
                   videos.duration_seconds, videos.uploaded_at, videos.discovered_at, videos.is_checked,
                   videos.download_status, videos.download_error, videos.download_attempts, tracks.id,
                   videos.metadata_status, videos.metadata_updated_at, videos.metadata_error,
                   videos.metadata_attempts, videos.view_count, videos.like_count, videos.thumbnail_url,
                   tracks.library_state, channels.name, ratings.name, ratings.sort_order,
                   COALESCE(tracks.listen_count, 0), COALESCE(tracks.listened_seconds, 0),
                   COALESCE(tracks.skip_count, 0)
            FROM channel_videos videos
            JOIN channels ON channels.id = videos.channel_id
            LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
            LEFT JOIN ratings ON ratings.id = tracks.rating_id
            WHERE videos.channel_id = $channelId
            ORDER BY {(uncheckedFirst ? "videos.is_checked ASC," : string.Empty)}
                     COALESCE(videos.uploaded_at, videos.discovered_at) DESC,
                     videos.id DESC";
        cmd.Parameters.AddWithValue("$channelId", channelId);
        using var reader = cmd.ExecuteReader();
        var videos = new List<ChannelVideo>();
        while (reader.Read())
            videos.Add(ReadChannelVideo(reader));
        return videos;
    }

    public void RecoverChannelMetadataQueue()
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"
            UPDATE channel_videos
            SET metadata_status = 'Pending', metadata_error = NULL, metadata_priority = 0
            WHERE metadata_status IN ('Queued', 'Loading')");
    }

    public void EnsureChannelMetadataQueueIndexes()
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ix_channel_videos_metadata_queue
                                ON channel_videos(metadata_status, metadata_updated_at, id DESC)");
        ExecuteNonQuery(conn, @"CREATE INDEX IF NOT EXISTS ix_tracks_channel_state
                                ON tracks(channel_id, library_state)");
    }

    public int QueueChannelVideoMetadata(int channelId, int limit)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE channel_videos
            SET metadata_status = 'Queued', metadata_error = NULL, metadata_priority = 100
            WHERE id IN (
                SELECT videos.id
                FROM channel_videos videos
                LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
                WHERE videos.channel_id = $channelId
                  AND EXISTS (SELECT 1 FROM channels
                              WHERE channels.id = videos.channel_id AND channels.subscribed = 1)
                  AND (tracks.id IS NULL OR tracks.source_metadata_updated_at IS NULL)
                  AND videos.metadata_status = 'Pending'
                  AND NOT EXISTS (
                      SELECT 1 FROM channel_videos active
                      WHERE active.channel_id = $channelId
                        AND active.metadata_status IN ('Queued', 'Loading'))
                ORDER BY COALESCE(videos.uploaded_at, videos.discovered_at) DESC,
                         videos.id DESC
                LIMIT $limit
            )";
        cmd.Parameters.AddWithValue("$channelId", channelId);
        cmd.Parameters.AddWithValue("$limit", 1);
        return cmd.ExecuteNonQuery();
    }

    public bool QueueSpecificChannelVideoMetadata(int videoId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE channel_videos
            SET metadata_status = 'Queued', metadata_error = NULL, metadata_priority = 200
            WHERE id = $videoId AND metadata_status IN ('Pending', 'Failed')
              AND EXISTS (SELECT 1 FROM channels
                          WHERE channels.id = channel_videos.channel_id AND channels.subscribed = 1)";
        cmd.Parameters.AddWithValue("$videoId", videoId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public int QueueAutoDownloadMetadata(int limit)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE channel_videos
            SET metadata_status = 'Queued', metadata_error = NULL, metadata_priority = 25
            WHERE id IN (
                SELECT videos.id
                FROM channel_videos videos
                JOIN channels ON channels.id = videos.channel_id
                LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
                WHERE channels.subscribed = 1
                  AND channels.auto_download = 1
                  AND tracks.id IS NULL
                  AND videos.is_checked = 0
                  AND videos.metadata_status = 'Pending'
                ORDER BY COALESCE(videos.uploaded_at, videos.discovered_at) DESC,
                         videos.id DESC
                LIMIT $limit
            )";
        cmd.Parameters.AddWithValue("$limit", 1);
        return cmd.ExecuteNonQuery();
    }

    public int QueueBackgroundChannelVideoMetadata(int limit)
    {
        limit = Math.Clamp(limit, 1, 10);
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var activeCommand = conn.CreateCommand();
        activeCommand.Transaction = tx;
        activeCommand.CommandText = @"SELECT COUNT(*) FROM channel_videos videos
                                      LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
                                      WHERE videos.metadata_status IN ('Queued', 'Loading')
                                        AND (tracks.id IS NOT NULL OR EXISTS (
                                            SELECT 1 FROM channels
                                            WHERE channels.id = videos.channel_id
                                              AND channels.subscribed = 1))";
        var remaining = Math.Max(0, limit - Convert.ToInt32(activeCommand.ExecuteScalar()));
        if (remaining == 0)
        {
            tx.Commit();
            return 0;
        }

        const string followed = @"EXISTS (SELECT 1 FROM channels followed_channel
                                           WHERE followed_channel.id = videos.channel_id
                                             AND followed_channel.subscribed = 1)";
        const string missingLibraryMetadata = @"EXISTS (SELECT 1 FROM tracks library_track
                                                WHERE library_track.canonical_url = videos.canonical_url
                                                  AND library_track.source_metadata_updated_at IS NULL)";
        var eligible = $"({followed} OR {missingLibraryMetadata})";
        var queued = QueueMetadataTiers(conn, tx,
            $"videos.metadata_status = 'Pending' AND {eligible}", remaining);
        remaining -= queued;
        if (remaining > 0)
        {
            var failed = QueueMetadataTiers(conn, tx,
                $"videos.metadata_status = 'Failed' AND videos.metadata_updated_at < datetime('now', '-7 days') AND {eligible}",
                remaining);
            queued += failed;
            remaining -= failed;
        }
        if (remaining > 0)
            queued += QueueMetadataTiers(conn, tx,
                $"videos.metadata_status = 'Ready' AND (videos.metadata_updated_at IS NULL OR videos.metadata_updated_at < datetime('now', '-30 days')) AND {followed}",
                remaining);

        tx.Commit();
        return queued;
    }

    private static int QueueMetadataTiers(
        SqliteConnection conn,
        SqliteTransaction tx,
        string candidateCondition,
        int limit)
    {
        var queued = 0;
        var tiers = new (int Priority, string Condition)[]
        {
            (20, @"EXISTS (SELECT 1 FROM tracks t
                           WHERE t.canonical_url = videos.canonical_url
                             AND t.source_metadata_updated_at IS NULL)"),
            (10, "EXISTS (SELECT 1 FROM channels c WHERE c.id = videos.channel_id AND c.subscribed = 1)"),
            (5, @"EXISTS (SELECT 1 FROM tracks t
                          WHERE t.channel_id = videos.channel_id AND t.library_state = 'Active')"),
            (1, "1 = 1")
        };

        foreach (var (priority, tierCondition) in tiers)
        {
            var remaining = limit - queued;
            if (remaining <= 0)
                break;
            using var command = conn.CreateCommand();
            command.Transaction = tx;
            command.CommandText = $@"
                UPDATE channel_videos
                SET metadata_status = 'Queued', metadata_error = NULL, metadata_priority = $priority
                WHERE id IN (
                    SELECT videos.id
                    FROM channel_videos videos
                    WHERE {candidateCondition} AND {tierCondition}
                    ORDER BY videos.id DESC
                    LIMIT $limit
                )";
            command.Parameters.AddWithValue("$priority", priority);
            command.Parameters.AddWithValue("$limit", remaining);
            queued += command.ExecuteNonQuery();
        }

        return queued;
    }

    public ChannelVideo? ClaimNextChannelVideoMetadata()
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var select = conn.CreateCommand();
        select.Transaction = tx;
        select.CommandText = @"
            SELECT videos.id, videos.channel_id, videos.video_id, videos.canonical_url, videos.title,
                   videos.duration_seconds, videos.uploaded_at, videos.discovered_at, videos.is_checked,
                   videos.download_status, videos.download_error, videos.download_attempts, tracks.id,
                   videos.metadata_status, videos.metadata_updated_at, videos.metadata_error,
                   videos.metadata_attempts, videos.view_count, videos.like_count, videos.thumbnail_url,
                   tracks.library_state, channels.name, NULL, NULL, 0, 0, 0
            FROM channel_videos videos
            JOIN channels ON channels.id = videos.channel_id
            LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
            WHERE videos.metadata_status = 'Queued'
              AND (channels.subscribed = 1 OR tracks.id IS NOT NULL)
            ORDER BY videos.metadata_priority DESC,
                     videos.is_checked ASC,
                     COALESCE(videos.uploaded_at, videos.discovered_at) DESC,
                     videos.id DESC
            LIMIT 1";
        using var reader = select.ExecuteReader();
        if (!reader.Read())
        {
            tx.Commit();
            return null;
        }

        var video = ReadChannelVideo(reader);
        reader.Close();
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET metadata_status = 'Loading', metadata_error = NULL,
                metadata_attempts = metadata_attempts + 1
            WHERE id = $id",
            ("$id", video.Id));
        tx.Commit();
        return video with
        {
            MetadataStatus = ChannelMetadataStatus.Loading,
            MetadataAttempts = video.MetadataAttempts + 1
        };
    }

    public bool HasQueuedChannelVideoMetadata()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT EXISTS(
                                SELECT 1 FROM channel_videos videos
                                JOIN channels ON channels.id = videos.channel_id
                                LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
                                WHERE videos.metadata_status = 'Queued'
                                  AND (channels.subscribed = 1 OR tracks.id IS NOT NULL))";
        return Convert.ToInt32(cmd.ExecuteScalar()) != 0;
    }

    public int CountBackgroundChannelVideoMetadataWork()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*)
            FROM channel_videos videos
            JOIN channels ON channels.id = videos.channel_id
            LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
            WHERE (channels.subscribed = 1
                   OR (tracks.id IS NOT NULL AND tracks.source_metadata_updated_at IS NULL))
              AND (videos.metadata_status IN ('Pending', 'Queued', 'Loading')
               OR (videos.metadata_status = 'Failed'
                   AND videos.metadata_updated_at < datetime('now', '-7 days'))
               OR (videos.metadata_status = 'Ready'
                   AND (videos.metadata_updated_at IS NULL
                        OR videos.metadata_updated_at < datetime('now', '-30 days'))))";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int ResetChannelMetadataIssues(int channelId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE channel_videos
            SET metadata_status = 'Pending', metadata_error = NULL, metadata_priority = 0
            WHERE channel_id = $channelId AND metadata_status = 'Failed'";
        cmd.Parameters.AddWithValue("$channelId", channelId);
        return cmd.ExecuteNonQuery();
    }

    public void CompleteChannelVideoMetadata(
        int videoId,
        YouTubeTrackMetadata? metadata,
        string? error,
        int maxDurationMinutes)
    {
        using var conn = Open();
        var now = DateTime.UtcNow.ToString("O");
        if (metadata is null)
        {
            ExecuteNonQuery(conn, @"
                UPDATE channel_videos
                SET metadata_status = 'Failed', metadata_error = $error,
                    metadata_updated_at = $now, metadata_priority = 0
                WHERE id = $id",
                ("$id", videoId), ("$error", error ?? "Metadata unavailable"), ("$now", now));
            return;
        }

        ExecuteNonQuery(conn, @"
            UPDATE channel_videos
            SET title = COALESCE($title, title),
                duration_seconds = COALESCE($duration, duration_seconds),
                uploaded_at = COALESCE($uploadedAt, uploaded_at),
                view_count = $viewCount,
                like_count = $likeCount,
                thumbnail_url = COALESCE($thumbnailUrl, thumbnail_url),
                metadata_status = 'Ready', metadata_error = NULL,
                metadata_updated_at = $now, metadata_priority = 0,
                download_status = CASE
                    WHEN EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url)
                        THEN 'Ready'
                    WHEN manual_download_requested = 1 THEN 'Queued'
                    WHEN $duration IS NOT NULL AND $duration > COALESCE(
                        (SELECT max_duration_minutes * 60 FROM channels
                         WHERE channels.id = channel_videos.channel_id), $maxDuration)
                        THEN 'Skipped'
                    WHEN is_checked = 0 AND EXISTS (
                        SELECT 1 FROM channels
                        WHERE channels.id = channel_videos.channel_id
                          AND channels.subscribed = 1
                          AND channels.auto_download = 1
                          AND (channels.auto_download_from IS NULL
                               OR channel_videos.discovered_at >= channels.auto_download_from))
                        THEN 'Queued'
                    ELSE download_status
                END
            WHERE id = $id",
            ("$id", videoId),
            ("$title", metadata.Title),
            ("$duration", metadata.DurationSeconds),
            ("$uploadedAt", metadata.UploadedAt),
            ("$viewCount", metadata.ViewCount),
            ("$likeCount", metadata.LikeCount),
            ("$thumbnailUrl", metadata.ThumbnailUrl),
            ("$maxDuration", Math.Clamp(maxDurationMinutes, 1, 24 * 60) * 60),
            ("$now", now));
        ExecuteNonQuery(conn, @"
            UPDATE tracks
            SET duration_seconds = COALESCE(duration_seconds, $duration),
                uploaded_at = COALESCE(uploaded_at, $uploadedAt),
                view_count = $viewCount,
                like_count = $likeCount,
                source_thumbnail_url = COALESCE($thumbnailUrl, source_thumbnail_url),
                source_metadata_updated_at = $now
            WHERE canonical_url = (
                SELECT canonical_url FROM channel_videos WHERE id = $id)",
            ("$id", videoId),
            ("$duration", metadata.DurationSeconds), ("$uploadedAt", metadata.UploadedAt),
            ("$viewCount", metadata.ViewCount), ("$likeCount", metadata.LikeCount),
            ("$thumbnailUrl", metadata.ThumbnailUrl), ("$now", now));
        if (metadata.ChannelFollowerCount is long followerCount)
            ExecuteNonQuery(conn, @"
                UPDATE channels
                SET follower_count = $followerCount, remote_metadata_updated_at = $now
                WHERE id = (SELECT channel_id FROM channel_videos WHERE id = $videoId)",
                ("$videoId", videoId), ("$followerCount", followerCount), ("$now", now));
    }

    public void SetChannelAutoDownload(int channelId, bool enabled, int maxDurationMinutes)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        ExecuteInsert(conn, tx, @"
            UPDATE channels
            SET auto_download_from = CASE
                    WHEN $enabled = 1 AND auto_download = 0 THEN $now
                    WHEN $enabled = 0 THEN NULL
                    ELSE auto_download_from
                END,
                auto_download = $enabled,
                updated_at = $now
            WHERE id = $id AND ($enabled = 0 OR subscribed = 1)",
            ("$id", channelId), ("$enabled", enabled ? 1 : 0), ("$now", now));
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET download_status = CASE
                    WHEN duration_seconds IS NOT NULL AND duration_seconds > COALESCE(
                        (SELECT max_duration_minutes * 60 FROM channels WHERE id = $id), $maxDuration)
                        THEN 'Skipped'
                    WHEN $enabled = 1 AND metadata_status = 'Ready'
                         AND discovered_at >= $now THEN 'Queued'
                    ELSE 'NotQueued'
                END,
                download_error = NULL,
                download_attempts = CASE WHEN $enabled = 1 THEN 0 ELSE download_attempts END,
                updated_at = $now
            WHERE channel_id = $id AND is_checked = 0 AND manual_download_requested = 0
              AND download_status IN ('Queued', 'Failed', 'NotQueued', 'Skipped')
              AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url)",
            ("$id", channelId), ("$enabled", enabled ? 1 : 0),
            ("$maxDuration", Math.Clamp(maxDurationMinutes, 1, 24 * 60) * 60), ("$now", now));
        tx.Commit();
    }

    public void SetChannelMaxDownloadDuration(int channelId, int? maxDurationMinutes, int globalMaxDurationMinutes)
    {
        int? normalized = maxDurationMinutes is int minutes ? Math.Clamp(minutes, 1, 24 * 60) : null;
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        ExecuteInsert(conn, tx, @"UPDATE channels
                                  SET max_duration_minutes = $minutes, updated_at = $now WHERE id = $id",
            ("$id", channelId), ("$minutes", normalized), ("$now", now));
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET download_status = CASE
                    WHEN duration_seconds IS NOT NULL AND duration_seconds > $maxDuration THEN 'Skipped'
                    WHEN metadata_status = 'Ready' AND EXISTS (
                        SELECT 1 FROM channels WHERE id = $id AND auto_download = 1
                          AND (auto_download_from IS NULL OR channel_videos.discovered_at >= auto_download_from))
                        THEN 'Queued'
                    ELSE 'NotQueued'
                END,
                download_error = NULL, updated_at = $now
            WHERE channel_id = $id AND is_checked = 0 AND manual_download_requested = 0
              AND download_status IN ('Queued', 'Failed', 'NotQueued', 'Skipped')
              AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url)",
            ("$id", channelId),
            ("$maxDuration", (normalized ?? Math.Clamp(globalMaxDurationMinutes, 1, 24 * 60)) * 60),
            ("$now", now));
        tx.Commit();
    }

    public void SetGlobalChannelMaxDownloadDuration(int maxDurationMinutes)
    {
        var maxDurationSeconds = Math.Clamp(maxDurationMinutes, 1, 24 * 60) * 60;
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET download_status = CASE
                    WHEN duration_seconds IS NOT NULL AND duration_seconds > COALESCE(
                        (SELECT max_duration_minutes * 60 FROM channels
                         WHERE channels.id = channel_videos.channel_id), $maxDuration) THEN 'Skipped'
                    WHEN EXISTS (SELECT 1 FROM channels
                                 WHERE channels.id = channel_videos.channel_id AND auto_download = 1
                                   AND (auto_download_from IS NULL
                                        OR channel_videos.discovered_at >= auto_download_from))
                         AND metadata_status = 'Ready' THEN 'Queued'
                    ELSE 'NotQueued'
                END,
                download_error = NULL,
                download_attempts = 0,
                updated_at = $now
            WHERE is_checked = 0 AND manual_download_requested = 0
              AND download_status IN ('Queued', 'Failed', 'NotQueued', 'Skipped')
              AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url)",
            ("$maxDuration", maxDurationSeconds), ("$now", now));
        tx.Commit();
    }

    public void RecoverChannelDownloads(int maxDurationMinutes)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"
            UPDATE channel_videos
            SET download_status = 'Ready', manual_download_requested = 0,
                download_error = NULL, updated_at = $now
            WHERE EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url);
            UPDATE channel_videos
            SET download_status = 'NotQueued', download_error = NULL, updated_at = $now
            WHERE is_checked = 1
              AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url);
            UPDATE channel_videos
            SET download_status = 'Skipped', download_error = NULL, updated_at = $now
            WHERE is_checked = 0 AND manual_download_requested = 0 AND duration_seconds IS NOT NULL
              AND duration_seconds > COALESCE(
                  (SELECT max_duration_minutes * 60 FROM channels
                   WHERE channels.id = channel_videos.channel_id), $maxDuration)
              AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url);
            UPDATE channel_videos
            SET download_status = 'Queued', download_error = NULL, updated_at = $now
            WHERE download_status = 'Skipped' AND is_checked = 0
              AND (duration_seconds IS NULL OR duration_seconds <= COALESCE(
                   (SELECT max_duration_minutes * 60 FROM channels
                    WHERE channels.id = channel_videos.channel_id), $maxDuration))
              AND EXISTS (SELECT 1 FROM channels
                          WHERE channels.id = channel_videos.channel_id AND auto_download = 1
                            AND (auto_download_from IS NULL
                                 OR channel_videos.discovered_at >= auto_download_from))
              AND metadata_status = 'Ready'
              AND NOT EXISTS (SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url);
            UPDATE channel_videos
            SET download_status = 'Queued', download_error = NULL, updated_at = $now
            WHERE download_status = 'Downloading' AND is_checked = 0
              AND (manual_download_requested = 1 OR duration_seconds IS NULL OR duration_seconds <= COALESCE(
                   (SELECT max_duration_minutes * 60 FROM channels
                    WHERE channels.id = channel_videos.channel_id), $maxDuration))
              AND (manual_download_requested = 1 OR EXISTS (SELECT 1 FROM channels WHERE channels.id = channel_videos.channel_id
                          AND auto_download = 1 AND (auto_download_from IS NULL
                               OR channel_videos.discovered_at >= auto_download_from)));
            UPDATE channel_videos
            SET download_status = 'Queued', download_error = NULL, updated_at = $now
            WHERE download_status = 'Failed' AND download_attempts < 3 AND is_checked = 0
              AND (manual_download_requested = 1 OR duration_seconds IS NULL OR duration_seconds <= COALESCE(
                   (SELECT max_duration_minutes * 60 FROM channels
                    WHERE channels.id = channel_videos.channel_id), $maxDuration))
              AND (manual_download_requested = 1 OR EXISTS (SELECT 1 FROM channels WHERE channels.id = channel_videos.channel_id
                          AND auto_download = 1 AND (auto_download_from IS NULL
                               OR channel_videos.discovered_at >= auto_download_from)));",
            ("$maxDuration", Math.Clamp(maxDurationMinutes, 1, 24 * 60) * 60),
            ("$now", DateTime.UtcNow.ToString("O")));
    }

    public ChannelVideo? ClaimNextChannelDownload(int maxDurationMinutes)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var select = conn.CreateCommand();
        select.Transaction = tx;
        select.CommandText = @"
            SELECT videos.id, videos.channel_id, videos.video_id, videos.canonical_url, videos.title,
                   videos.duration_seconds, videos.uploaded_at, videos.discovered_at, videos.is_checked,
                   videos.download_status, videos.download_error, videos.download_attempts, tracks.id,
                   videos.metadata_status, videos.metadata_updated_at, videos.metadata_error,
                   videos.metadata_attempts, videos.view_count, videos.like_count, videos.thumbnail_url,
                   tracks.library_state, channels.name, ratings.name, ratings.sort_order,
                   COALESCE(tracks.listen_count, 0), COALESCE(tracks.listened_seconds, 0),
                   COALESCE(tracks.skip_count, 0)
            FROM channel_videos videos
            JOIN channels ON channels.id = videos.channel_id
            LEFT JOIN tracks ON tracks.canonical_url = videos.canonical_url
            LEFT JOIN ratings ON ratings.id = tracks.rating_id
            WHERE videos.download_status = 'Queued' AND videos.is_checked = 0
              AND videos.metadata_status = 'Ready'
              AND (videos.manual_download_requested = 1 OR (
                   channels.auto_download = 1
                   AND (channels.auto_download_from IS NULL OR videos.discovered_at >= channels.auto_download_from)))
              AND (videos.manual_download_requested = 1 OR videos.duration_seconds IS NULL
                   OR videos.duration_seconds <= COALESCE(channels.max_duration_minutes * 60, $maxDuration))
            ORDER BY videos.discovered_at, videos.id LIMIT 1";
        select.Parameters.AddWithValue("$maxDuration", Math.Clamp(maxDurationMinutes, 1, 24 * 60) * 60);
        using var reader = select.ExecuteReader();
        if (!reader.Read())
        {
            tx.Commit();
            return null;
        }

        var video = ReadChannelVideo(reader);
        reader.Close();
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET download_status = 'Downloading', download_error = NULL,
                download_attempts = download_attempts + 1, updated_at = $now
            WHERE id = $id",
            ("$id", video.Id), ("$now", DateTime.UtcNow.ToString("O")));
        tx.Commit();
        return video with
        {
            DownloadStatus = ChannelDownloadStatus.Downloading,
            DownloadAttempts = video.DownloadAttempts + 1
        };
    }

    public void CompleteChannelDownload(int videoId, bool success, string? error)
    {
        using var conn = Open();
        ExecuteNonQuery(conn, @"
            UPDATE channel_videos
            SET download_status = CASE
                    WHEN $success = 1 THEN 'Ready'
                    WHEN download_attempts < 3 AND is_checked = 0 AND
                         (manual_download_requested = 1 OR EXISTS (SELECT 1 FROM channels
                                     WHERE channels.id = channel_videos.channel_id
                                       AND auto_download = 1
                                       AND (auto_download_from IS NULL
                                            OR channel_videos.discovered_at >= auto_download_from)))
                        THEN 'Queued'
                    ELSE 'Failed'
                END,
                manual_download_requested = CASE WHEN $success = 1 THEN 0 ELSE manual_download_requested END,
                download_error = $error, updated_at = $now
            WHERE id = $id",
            ("$id", videoId), ("$success", success ? 1 : 0), ("$error", error),
            ("$now", DateTime.UtcNow.ToString("O")));
    }

    public bool RequestChannelVideoDownload(int videoId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE channel_videos
            SET manual_download_requested = 1,
                is_checked = 0,
                download_status = CASE WHEN metadata_status = 'Ready' THEN 'Queued' ELSE 'NotQueued' END,
                download_error = NULL,
                download_attempts = 0,
                updated_at = $now
            WHERE id = $videoId
              AND NOT EXISTS (
                  SELECT 1 FROM tracks WHERE tracks.canonical_url = channel_videos.canonical_url)";
        cmd.Parameters.AddWithValue("$videoId", videoId);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        return cmd.ExecuteNonQuery() > 0;
    }

    public ChannelDownloadSummary GetChannelDownloadSummary()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT download_status, COUNT(*) FROM channel_videos GROUP BY download_status";
        using var reader = cmd.ExecuteReader();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) counts[reader.GetString(0)] = ReadInt32(reader, 1);
        return new ChannelDownloadSummary(
            counts.GetValueOrDefault("Queued"),
            counts.GetValueOrDefault("Downloading"),
            counts.GetValueOrDefault("Ready"),
            counts.GetValueOrDefault("Failed"),
            counts.GetValueOrDefault("Skipped"));
    }

    private static ChannelVideo ReadChannelVideo(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetInt32(5),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.GetString(7), ReadInt32(reader, 8) != 0,
        Enum.TryParse<ChannelDownloadStatus>(reader.GetString(9), true, out var status)
            ? status
            : ChannelDownloadStatus.NotQueued,
        reader.IsDBNull(10) ? null : reader.GetString(10),
        ReadInt32(reader, 11),
        reader.IsDBNull(12) ? null : reader.GetInt32(12),
        reader.FieldCount > 13 && Enum.TryParse<ChannelMetadataStatus>(reader.GetString(13), true, out var metadataStatus)
            ? metadataStatus
            : ChannelMetadataStatus.Pending,
        reader.FieldCount > 14 && !reader.IsDBNull(14) ? reader.GetString(14) : null,
        reader.FieldCount > 15 && !reader.IsDBNull(15) ? reader.GetString(15) : null,
        reader.FieldCount > 16 ? ReadInt32(reader, 16) : 0,
        reader.FieldCount > 17 && !reader.IsDBNull(17) ? reader.GetInt64(17) : null,
        reader.FieldCount > 18 && !reader.IsDBNull(18) ? reader.GetInt64(18) : null,
        reader.FieldCount > 19 && !reader.IsDBNull(19) ? reader.GetString(19) : null,
        reader.FieldCount > 20 && !reader.IsDBNull(20)
            && Enum.TryParse<TrackLibraryState>(reader.GetString(20), true, out var libraryState)
                ? libraryState
                : null,
        reader.FieldCount > 21 && !reader.IsDBNull(21) ? reader.GetString(21) : null,
        reader.FieldCount > 22 && !reader.IsDBNull(22) ? reader.GetString(22) : null,
        reader.FieldCount > 23 && !reader.IsDBNull(23) ? ReadInt32(reader, 23) : null,
        reader.FieldCount > 24 ? ReadInt32(reader, 24) : 0,
        reader.FieldCount > 25 ? ReadInt32(reader, 25) : 0,
        reader.FieldCount > 26 ? ReadInt32(reader, 26) : 0);

    public bool DeleteChannel(int channelId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM channels WHERE id = $channelId";
        cmd.Parameters.AddWithValue("$channelId", channelId);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static int ReadInt32(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));

    private static long? FindChannelId(SqliteConnection conn, SqliteTransaction tx, string? sourceChannelId, string sourceUrl)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            SELECT id FROM channels
            WHERE ($sourceChannelId IS NOT NULL AND source_channel_id = $sourceChannelId)
               OR ($sourceUrl IS NOT NULL AND source_url = $sourceUrl)
            LIMIT 1";
        cmd.Parameters.AddWithValue("$sourceChannelId", (object?)sourceChannelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sourceUrl", sourceUrl);
        return cmd.ExecuteScalar() is long id ? id : null;
    }

    private static bool ChannelVideoExists(SqliteConnection conn, SqliteTransaction tx, string canonicalUrl)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COUNT(*) FROM channel_videos WHERE canonical_url = $canonicalUrl";
        cmd.Parameters.AddWithValue("$canonicalUrl", canonicalUrl);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public int InsertTrack(string canonicalUrl, string title, string fileName,
        List<int> genreIds, int? ratingId, List<int> _, int? durationSeconds, long? fileSizeBytes,
        int? downloadDurationMilliseconds, YouTubeTrackMetadata? metadata = null, byte[]? thumbnail = null)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        long? channelId = null;
        if (!string.IsNullOrWhiteSpace(metadata?.ChannelId))
        {
            ExecuteInsert(conn, tx, @"INSERT INTO channels
                    (name, source_channel_id, source_url, subscribed, auto_download,
                     follower_count, remote_metadata_updated_at)
                VALUES ($name, $channelId, $channelUrl, 0, 0, $followerCount, $metadataUpdatedAt)
                ON CONFLICT(source_channel_id) DO UPDATE SET
                    name = excluded.name,
                    source_url = COALESCE(excluded.source_url, channels.source_url),
                    follower_count = COALESCE(excluded.follower_count, channels.follower_count),
                    remote_metadata_updated_at = excluded.remote_metadata_updated_at",
                ("$name", metadata.ChannelName ?? "Unknown channel"), ("$channelId", metadata.ChannelId),
                ("$channelUrl", metadata.ChannelUrl), ("$followerCount", metadata.ChannelFollowerCount),
                ("$metadataUpdatedAt", now));
            channelId = InsertAndGetId(conn, tx, "SELECT id FROM channels WHERE source_channel_id = $channelId",
                ("$channelId", metadata.ChannelId));
        }

        var trackId = InsertAndGetId(conn, tx, @"
            INSERT INTO tracks
                (canonical_url, title, file_name, channel_id, rating_id, uploaded_at, downloaded_at, updated_at,
                 duration_seconds, file_size_bytes, download_duration_ms, thumbnail, is_public, library_state,
                 needs_reevaluation,
                 source_video_id, view_count, like_count, source_thumbnail_url, source_metadata_updated_at)
            VALUES
                ($url, $title, $fileName, $channelId, $ratingId, $uploadedAt, $downloadedAt, $updatedAt,
                 $duration, $fileSizeBytes, $downloadDurationMs, $thumbnail, 1, $libraryState,
                 $needsReview,
                 $videoId, $viewCount, $likeCount, $thumbnailUrl, $metadataUpdatedAt)",
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
            ("$downloadDurationMs", downloadDurationMilliseconds),
            ("$thumbnail", thumbnail),
            ("$libraryState", ratingId is null ? "PendingRating" : "Active"),
            ("$needsReview", ratingId is null ? 1 : 0),
            ("$videoId", YouTubeUrlNormalizer.ExtractVideoId(canonicalUrl)),
            ("$viewCount", metadata?.ViewCount),
            ("$likeCount", metadata?.LikeCount),
            ("$thumbnailUrl", metadata?.ThumbnailUrl),
            ("$metadataUpdatedAt", metadata is null ? null : now));

        SynchronizeLibraryChannelVideo(conn, tx, trackId, now);

        tx.Commit();
        return (int)trackId;
    }

    private static void SynchronizeLibraryChannelVideo(
        SqliteConnection conn,
        SqliteTransaction tx,
        long trackId,
        string now)
    {
        ExecuteInsert(conn, tx, @"
            INSERT INTO channel_videos
                (channel_id, video_id, canonical_url, title, duration_seconds, uploaded_at,
                 discovered_at, updated_at, is_checked, download_status, metadata_status,
                 metadata_updated_at, view_count, like_count, thumbnail_url)
            SELECT channel_id, COALESCE(source_video_id, 'library-track-' || id), canonical_url, title,
                   duration_seconds, uploaded_at, downloaded_at, $now, 1, 'Ready',
                   CASE WHEN source_metadata_updated_at IS NULL THEN 'Pending' ELSE 'Ready' END,
                   source_metadata_updated_at, view_count, like_count, source_thumbnail_url
            FROM tracks
            WHERE id = $trackId AND channel_id IS NOT NULL
              AND canonical_url IS NOT NULL AND TRIM(canonical_url) <> ''
            ON CONFLICT(canonical_url) DO UPDATE SET
                channel_id = excluded.channel_id,
                title = excluded.title,
                duration_seconds = COALESCE(channel_videos.duration_seconds, excluded.duration_seconds),
                uploaded_at = COALESCE(channel_videos.uploaded_at, excluded.uploaded_at),
                metadata_status = CASE
                    WHEN excluded.metadata_status = 'Ready' THEN 'Ready'
                    ELSE channel_videos.metadata_status
                END,
                metadata_updated_at = COALESCE(excluded.metadata_updated_at, channel_videos.metadata_updated_at),
                view_count = COALESCE(excluded.view_count, channel_videos.view_count),
                like_count = COALESCE(excluded.like_count, channel_videos.like_count),
                thumbnail_url = COALESCE(excluded.thumbnail_url, channel_videos.thumbnail_url),
                is_checked = 1,
                download_status = 'Ready',
                download_error = NULL,
                metadata_error = NULL,
                metadata_priority = 0",
            ("$trackId", trackId), ("$now", now));
    }

    public int InsertPreloadedChannelTrack(
        ChannelVideo video,
        string fileName,
        int? durationSeconds,
        long fileSizeBytes,
        int downloadDurationMilliseconds,
        byte[]? thumbnail)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using (var existing = conn.CreateCommand())
        {
            existing.Transaction = tx;
            existing.CommandText = "SELECT id FROM tracks WHERE canonical_url = $url";
            existing.Parameters.AddWithValue("$url", video.CanonicalUrl);
            if (existing.ExecuteScalar() is long existingId)
            {
                tx.Commit();
                return (int)existingId;
            }
        }

        var now = DateTime.UtcNow.ToString("O");
        var trackId = InsertAndGetId(conn, tx, @"
            INSERT INTO tracks
                (canonical_url, title, file_name, channel_id, rating_id, uploaded_at, downloaded_at, updated_at,
                 duration_seconds, file_size_bytes, download_duration_ms, thumbnail, analysis_disabled, needs_reevaluation, is_public)
            VALUES
                ($url, $title, $fileName, $channelId, NULL, $uploadedAt, $now, $now,
                 $duration, $fileSize, $downloadDuration, $thumbnail, 0, 1, 1)",
            ("$url", video.CanonicalUrl),
            ("$title", video.Title),
            ("$fileName", fileName),
            ("$channelId", video.ChannelId),
            ("$uploadedAt", video.UploadedAt),
            ("$now", now),
            ("$duration", durationSeconds),
            ("$fileSize", fileSizeBytes),
            ("$downloadDuration", downloadDurationMilliseconds),
            ("$thumbnail", thumbnail));
        ExecuteInsert(conn, tx, @"
            UPDATE tracks
            SET library_state = 'PendingRating', source_video_id = $sourceVideoId,
                view_count = $viewCount, like_count = $likeCount,
                source_thumbnail_url = $thumbnailUrl,
                source_metadata_updated_at = $metadataUpdatedAt
            WHERE id = $trackId",
            ("$trackId", trackId), ("$sourceVideoId", video.VideoId),
            ("$viewCount", video.ViewCount), ("$likeCount", video.LikeCount),
            ("$thumbnailUrl", video.ThumbnailUrl), ("$metadataUpdatedAt", video.MetadataUpdatedAt));
        tx.Commit();
        return (int)trackId;
    }

    public int? CompleteChannelVideoReview(int videoId, bool skip)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        long? trackId;
        using (var find = conn.CreateCommand())
        {
            find.Transaction = tx;
            find.CommandText = @"
                SELECT tracks.id
                FROM channel_videos videos
                JOIN tracks ON tracks.canonical_url = videos.canonical_url
                WHERE videos.id = $videoId";
            find.Parameters.AddWithValue("$videoId", videoId);
            trackId = find.ExecuteScalar() as long?;
        }
        if (trackId is null)
        {
            tx.Commit();
            return null;
        }

        ExecuteInsert(conn, tx,
            "UPDATE channel_videos SET is_checked = 1, updated_at = $now WHERE id = $videoId",
            ("$videoId", videoId), ("$now", DateTime.UtcNow.ToString("O")));
        if (skip)
        {
            ExecuteInsert(conn, tx, @"
                UPDATE tracks
                SET rating_id = (SELECT id FROM ratings WHERE name = $avoidRating),
                    library_state = 'Rejected', analysis_disabled = 1,
                    needs_reevaluation = 0, updated_at = $now
                WHERE id = $trackId",
                ("$trackId", trackId.Value), ("$avoidRating", RatingNames.Avoid),
                ("$now", DateTime.UtcNow.ToString("O")));
        }
        else
        {
            ExecuteInsert(conn, tx, @"
                UPDATE tracks
                SET library_state = 'Active', analysis_disabled = 0,
                    needs_reevaluation = 1, updated_at = $now
                WHERE id = $trackId",
                ("$trackId", trackId.Value), ("$now", DateTime.UtcNow.ToString("O")));
        }

        tx.Commit();
        return (int)trackId.Value;
    }

    public bool DismissChannelVideo(int videoId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE channel_videos
            SET is_checked = 1,
                download_status = 'NotQueued',
                download_error = NULL,
                updated_at = $now
            WHERE id = $videoId AND is_checked = 0";
        cmd.Parameters.AddWithValue("$videoId", videoId);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        return cmd.ExecuteNonQuery() > 0;
    }

    public HashSet<int> GetTrackIdsMissingAnalysis()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT tracks.id FROM tracks
            LEFT JOIN track_analysis analysis ON analysis.track_id = tracks.id
            WHERE analysis.id IS NULL
              AND tracks.analysis_disabled = 0
              AND tracks.library_state = 'Active'";
        using var reader = cmd.ExecuteReader();
        var ids = new HashSet<int>();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        return ids;
    }

    public void SaveTrackAnalysis(int trackId, TrackAnalysisResult analysis, int? analysisDurationMilliseconds = null)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

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
            ("$analyzedAt", now),
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

        RefreshModelGenres(conn, tx, trackId);
        ExecuteInsert(conn, tx,
            "UPDATE tracks SET analysis_disabled = 0, needs_reevaluation = 1 WHERE id = $trackId",
            ("$trackId", trackId));
        TouchTrack(conn, tx, trackId, now);

        tx.Commit();
    }

    public List<MusicTrack> GetAllTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.id, tracks.canonical_url, tracks.title, tracks.file_name, tracks.rating_id, tracks.downloaded_at,
                                   tracks.duration_seconds, tracks.needs_reevaluation, channels.name, channels.source_url, tracks.uploaded_at,
                                   tracks.updated_at, tracks.analysis_disabled, tracks.is_public, tracks.library_state,
                                   tracks.source_video_id, tracks.view_count, tracks.like_count,
                                   tracks.source_thumbnail_url, tracks.source_metadata_updated_at, channels.id
                            FROM tracks LEFT JOIN channels ON channels.id = tracks.channel_id
                            WHERE tracks.library_state <> 'Rejected'
                            ORDER BY tracks.downloaded_at DESC";
        using var reader = cmd.ExecuteReader();
        var tracks = new List<MusicTrack>();
        while (reader.Read())
        {
            tracks.Add(ReadMusicTrack(reader));
        }
        return tracks;
    }

    public MusicTrack? GetTrackById(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.id, tracks.canonical_url, tracks.title, tracks.file_name, tracks.rating_id, tracks.downloaded_at,
                                   tracks.duration_seconds, tracks.needs_reevaluation, channels.name, channels.source_url, tracks.uploaded_at,
                                   tracks.updated_at, tracks.analysis_disabled, tracks.is_public, tracks.library_state,
                                   tracks.source_video_id, tracks.view_count, tracks.like_count,
                                   tracks.source_thumbnail_url, tracks.source_metadata_updated_at, channels.id
                            FROM tracks LEFT JOIN channels ON channels.id = tracks.channel_id
                            WHERE tracks.id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadMusicTrack(reader) : null;
    }

    public byte[]? GetTrackThumbnail(int trackId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT thumbnail FROM tracks WHERE id = $trackId";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        var value = cmd.ExecuteScalar();
        return value is DBNull or null ? null : (byte[])value;
    }

    public HashSet<int> GetAnalyzedTrackIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT track_id FROM track_analysis";
        using var reader = cmd.ExecuteReader();
        var result = new HashSet<int>();
        while (reader.Read())
            result.Add(reader.GetInt32(0));
        return result;
    }

    public List<MusicTrack> GetUnanalyzedTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.id, tracks.canonical_url, tracks.title, tracks.file_name, tracks.rating_id, tracks.downloaded_at,
                                   tracks.duration_seconds, tracks.needs_reevaluation, channels.name, channels.source_url, tracks.uploaded_at,
                                   tracks.updated_at, tracks.analysis_disabled, tracks.is_public, tracks.library_state,
                                   tracks.source_video_id, tracks.view_count, tracks.like_count,
                                   tracks.source_thumbnail_url, tracks.source_metadata_updated_at, channels.id
                            FROM tracks
                            LEFT JOIN channels ON channels.id = tracks.channel_id
                            LEFT JOIN track_analysis analysis ON analysis.track_id = tracks.id
                            WHERE analysis.id IS NULL
                              AND tracks.analysis_disabled = 0
                              AND tracks.library_state = 'Active'
                            ORDER BY tracks.downloaded_at, tracks.id";
        using var reader = cmd.ExecuteReader();
        var tracks = new List<MusicTrack>();
        while (reader.Read())
        {
            tracks.Add(ReadMusicTrack(reader));
        }
        return tracks;
    }

    public int CountUnratedTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(*) FROM tracks
                            WHERE rating_id IS NULL AND library_state <> 'Rejected'";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<MusicTrack> GetUnratedTracks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT tracks.id, tracks.canonical_url, tracks.title, tracks.file_name, tracks.rating_id, tracks.downloaded_at,
                                   tracks.duration_seconds, tracks.needs_reevaluation, channels.name, channels.source_url, tracks.uploaded_at,
                                   tracks.updated_at, tracks.analysis_disabled, tracks.is_public, tracks.library_state,
                                   tracks.source_video_id, tracks.view_count, tracks.like_count,
                                   tracks.source_thumbnail_url, tracks.source_metadata_updated_at, channels.id
                            FROM tracks LEFT JOIN channels ON channels.id = tracks.channel_id
                            WHERE tracks.rating_id IS NULL AND tracks.library_state <> 'Rejected'
                            ORDER BY tracks.downloaded_at DESC";
        using var reader = cmd.ExecuteReader();
        var tracks = new List<MusicTrack>();
        while (reader.Read())
            tracks.Add(ReadMusicTrack(reader));
        return tracks;
    }

    private static MusicTrack ReadMusicTrack(SqliteDataReader reader)
    {
        var libraryState = TrackLibraryState.Active;
        if (!reader.IsDBNull(14)
            && Enum.TryParse<TrackLibraryState>(reader.GetString(14), ignoreCase: true, out var parsedState))
            libraryState = parsedState;

        return new MusicTrack(
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
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.GetString(11),
            reader.GetInt32(12) != 0,
            reader.GetInt32(13) != 0,
            null,
            libraryState,
            reader.IsDBNull(15) ? null : reader.GetString(15),
            reader.IsDBNull(16) ? null : reader.GetInt64(16),
            reader.IsDBNull(17) ? null : reader.GetInt64(17),
            reader.IsDBNull(18) ? null : reader.GetString(18),
            reader.IsDBNull(19) ? null : reader.GetString(19),
            reader.IsDBNull(20) ? null : reader.GetInt32(20));
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
        cmd.CommandText = @"SELECT track_genres.genre_id, msg.name, track_genres.is_enabled, track_genres.is_manual,
                                   mg.name, msg.name, predictions.score
                            FROM track_genres
                            JOIN model_subgenres msg ON msg.id = track_genres.genre_id
                            JOIN model_genres mg ON mg.id = msg.model_genre_id
                            LEFT JOIN track_analysis analysis ON analysis.track_id = track_genres.track_id
                            LEFT JOIN track_genre_predictions predictions ON predictions.track_analysis_id = analysis.id AND predictions.model_subgenre_id = msg.id
                            WHERE track_genres.track_id = $trackId
                            ORDER BY predictions.score IS NULL, predictions.score DESC, msg.name";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var groups = new Dictionary<int, (string Name, bool Enabled, bool Manual, List<ModelGenreReason> Reasons)>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            if (!groups.TryGetValue(id, out var group))
                groups[id] = group = (reader.GetString(1), reader.GetInt32(2) != 0, reader.GetInt32(3) != 0, []);
            if (!reader.IsDBNull(6))
                group.Reasons.Add(new ModelGenreReason(reader.GetString(4), reader.GetString(5), reader.GetDouble(6)));
        }
        return groups.Select(x => new TrackModelGenre(
            x.Key, x.Value.Name, x.Value.Enabled, x.Value.Manual, x.Value.Reasons)).ToList();
    }

    public void SetTrackModelGenreEnabled(int trackId, int genreId, bool isEnabled)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        var now = DateTime.UtcNow.ToString("O");
        if (isEnabled)
        {
            cmd.CommandText = @"
                INSERT INTO track_genres (track_id, genre_id, assigned_at, is_enabled, is_manual)
                VALUES ($trackId, $genreId, $assignedAt, 1, 1)
                ON CONFLICT(track_id, genre_id) DO UPDATE SET
                    is_enabled = 1,
                    is_manual = 1,
                    assigned_at = excluded.assigned_at";
            cmd.Parameters.AddWithValue("$assignedAt", now);
        }
        else
        {
            cmd.CommandText = @"UPDATE track_genres SET is_enabled = 0 WHERE track_id = $trackId AND genre_id = $genreId";
        }
        cmd.Parameters.AddWithValue("$trackId", trackId);
        cmd.Parameters.AddWithValue("$genreId", genreId);
        cmd.ExecuteNonQuery();
        TouchTrack(conn, tx, trackId, now);
        tx.Commit();
    }

    // Styles are intentionally no longer part of the schema. These compatibility
    // methods keep the current UI operational until its dedicated filter redesign.
    public Dictionary<int, List<int>> GetAllTrackStyleIds() => [];
    public List<int> GetTrackStyleIds(int trackId) => [];
    public List<Style> GetStyles() => [];

    public void UpdateTrack(int id, string title, List<int> genreIds, int? ratingId, List<int> _, bool isPublic)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        ExecuteInsert(conn, tx,
            @"UPDATE tracks
              SET title = $title,
                  rating_id = $ratingId,
                  library_state = CASE WHEN $ratingId IS NULL THEN library_state ELSE 'Active' END,
                  is_public = $isPublic,
                  updated_at = $updatedAt,
                  needs_reevaluation = 0
              WHERE id = $id",
            ("$id", id), ("$title", title), ("$ratingId", ratingId), ("$isPublic", isPublic ? 1 : 0), ("$updatedAt", now));

        if (ratingId is not null)
            ExecuteInsert(conn, tx, @"
                UPDATE channel_videos
                SET is_checked = 1, download_status = 'Ready', download_error = NULL, updated_at = $updatedAt
                WHERE canonical_url = (SELECT canonical_url FROM tracks WHERE id = $id)",
                ("$id", id), ("$updatedAt", now));

        tx.Commit();
    }

    public void SetTrackNeedsReview(int id, bool needsReview)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tracks SET needs_reevaluation = $needsReview, updated_at = $updatedAt WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$needsReview", needsReview ? 1 : 0);
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void SetTrackAnalysisDisabled(int id, bool analysisDisabled)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tracks SET analysis_disabled = $analysisDisabled, updated_at = $updatedAt WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$analysisDisabled", analysisDisabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteTrack(int id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        DeleteTrack(conn, tx, id);
        tx.Commit();
    }

    public void SetTrackRating(int id, int ratingId)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        ExecuteInsert(conn, tx, @"
            UPDATE tracks
            SET rating_id = $ratingId, library_state = 'Active', needs_reevaluation = 0, updated_at = $now
            WHERE id = $id",
            ("$id", id), ("$ratingId", ratingId), ("$now", now));
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET is_checked = 1, download_status = 'Ready', download_error = NULL, updated_at = $now
            WHERE canonical_url = (SELECT canonical_url FROM tracks WHERE id = $id)",
            ("$id", id), ("$now", now));
        tx.Commit();
    }

    public void DeleteTracks(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return;

        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var hasImportQueue = TableExists(conn, "import_queue_items");
        var hasTrackTags = TableExists(conn, "track_tags");
        var hasTrackGenres = TableExists(conn, "track_genres");
        var hasTrackAnalysis = TableExists(conn, "track_analysis");
        var hasGenrePredictions = hasTrackAnalysis && TableExists(conn, "track_genre_predictions");
        var hasAnalysisSignals = hasTrackAnalysis && TableExists(conn, "track_analysis_signals");
        var hasDerivedAttributes = hasTrackAnalysis && TableExists(conn, "track_derived_attributes");
        foreach (var id in ids)
        {
            DeleteTrack(
                conn,
                tx,
                id,
                hasImportQueue,
                hasTrackTags,
                hasTrackGenres,
                hasTrackAnalysis,
                hasGenrePredictions,
                hasAnalysisSignals,
                hasDerivedAttributes);
        }
        tx.Commit();
    }

    private static void DeleteTrack(SqliteConnection conn, SqliteTransaction tx, int id)
    {
        var hasTrackAnalysis = TableExists(conn, "track_analysis");
        DeleteTrack(
            conn,
            tx,
            id,
            TableExists(conn, "import_queue_items"),
            TableExists(conn, "track_tags"),
            TableExists(conn, "track_genres"),
            hasTrackAnalysis,
            hasTrackAnalysis && TableExists(conn, "track_genre_predictions"),
            hasTrackAnalysis && TableExists(conn, "track_analysis_signals"),
            hasTrackAnalysis && TableExists(conn, "track_derived_attributes"));
    }

    private static void DeleteTrack(
        SqliteConnection conn,
        SqliteTransaction tx,
        int id,
        bool hasImportQueue,
        bool hasTrackTags,
        bool hasTrackGenres,
        bool hasTrackAnalysis,
        bool hasGenrePredictions,
        bool hasAnalysisSignals,
        bool hasDerivedAttributes)
    {
        ExecuteInsert(conn, tx, @"
            UPDATE channel_videos
            SET is_checked = 1, download_status = 'NotQueued', download_error = NULL, updated_at = $now
            WHERE canonical_url = (SELECT canonical_url FROM tracks WHERE id = $id)",
            ("$id", id), ("$now", DateTime.UtcNow.ToString("O")));

        // Older import_queue_items tables were created without ON DELETE SET NULL.
        // Detach queued/history rows explicitly so deleting a library track does not
        // fail with a foreign-key constraint.
        if (hasImportQueue)
        {
            ExecuteInsert(conn, tx,
                "UPDATE import_queue_items SET track_id = NULL WHERE track_id = $id",
                ("$id", id));
        }

        if (hasTrackTags)
            ExecuteInsert(conn, tx, "DELETE FROM track_tags WHERE track_id = $id", ("$id", id));
        if (hasTrackGenres)
            ExecuteInsert(conn, tx, "DELETE FROM track_genres WHERE track_id = $id", ("$id", id));
        if (hasTrackAnalysis)
        {
            if (hasGenrePredictions)
                ExecuteInsert(conn, tx, @"DELETE FROM track_genre_predictions
                    WHERE track_analysis_id IN (SELECT id FROM track_analysis WHERE track_id = $id)", ("$id", id));
            if (hasAnalysisSignals)
                ExecuteInsert(conn, tx, @"DELETE FROM track_analysis_signals
                    WHERE track_analysis_id IN (SELECT id FROM track_analysis WHERE track_id = $id)", ("$id", id));
            if (hasDerivedAttributes)
                ExecuteInsert(conn, tx, @"DELETE FROM track_derived_attributes
                    WHERE track_analysis_id IN (SELECT id FROM track_analysis WHERE track_id = $id)", ("$id", id));
            ExecuteInsert(conn, tx, "DELETE FROM track_analysis WHERE track_id = $id", ("$id", id));
        }

        ExecuteInsert(conn, tx, "DELETE FROM tracks WHERE id = $id", ("$id", id));
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

    public List<Tag> GetTags()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM tags ORDER BY name";
        return ReadTags(cmd);
    }

    public void AddTag(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO tags (name) VALUES ($name)";
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.ExecuteNonQuery();
    }

    public void RenameTag(int id, string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE tags SET name = $name WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name.Trim());
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
            SELECT tags.id, tags.name
            FROM track_tags
            JOIN tags ON tags.id = track_tags.tag_id
            WHERE track_tags.track_id = $trackId
            ORDER BY tags.name";
        cmd.Parameters.AddWithValue("$trackId", trackId);
        using var reader = cmd.ExecuteReader();
        var tags = new List<TrackTag>();
        while (reader.Read())
            tags.Add(new TrackTag(reader.GetInt32(0), reader.GetString(1)));
        return tags;
    }

    public void SetTrackManualTags(int trackId, IReadOnlyCollection<int> tagIds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");

        ExecuteInsert(conn, tx, "DELETE FROM track_tags WHERE track_id = $trackId",
            ("$trackId", trackId));

        foreach (var tagId in tagIds.Distinct())
        {
            ExecuteInsert(conn, tx, @"
                INSERT INTO track_tags (track_id, tag_id)
                VALUES ($trackId, $tagId)
                ON CONFLICT(track_id, tag_id) DO NOTHING",
                ("$trackId", trackId), ("$tagId", tagId));
        }

        TouchTrack(conn, tx, trackId, now);
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

    public List<ManualModelGenreUsage> GetTopManualModelGenres(int limit = 10)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT model_subgenres.id, model_subgenres.model_genre_id, model_subgenres.name,
                   model_genres.name, COUNT(*) AS usage_count
            FROM track_genres
            JOIN model_subgenres ON model_subgenres.id = track_genres.genre_id
            JOIN model_genres ON model_genres.id = model_subgenres.model_genre_id
            WHERE track_genres.is_enabled = 1
              AND track_genres.is_manual = 1
            GROUP BY model_subgenres.id, model_subgenres.model_genre_id, model_subgenres.name, model_genres.name
            ORDER BY usage_count DESC, model_genres.name, model_subgenres.name
            LIMIT $limit";
        cmd.Parameters.AddWithValue("$limit", Math.Max(1, limit));
        using var reader = cmd.ExecuteReader();
        var usages = new List<ManualModelGenreUsage>();
        while (reader.Read())
        {
            usages.Add(new ManualModelGenreUsage(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                Convert.ToInt32(reader.GetInt64(4))));
        }
        return usages;
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

    private static void TouchTrack(SqliteConnection conn, SqliteTransaction tx, int trackId, string updatedAt) =>
        ExecuteInsert(conn, tx, "UPDATE tracks SET updated_at = $updatedAt WHERE id = $trackId",
            ("$updatedAt", updatedAt), ("$trackId", trackId));

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
            AND is_manual = 0
            AND NOT EXISTS (
                SELECT 1 FROM track_genre_predictions predictions JOIN track_analysis analysis ON analysis.id = predictions.track_analysis_id
                WHERE analysis.track_id = track_genres.track_id AND predictions.model_subgenre_id = track_genres.genre_id AND predictions.score > 0.25)",
            ("$trackId", trackId));
        ExecuteInsert(conn, tx, @"
            INSERT INTO track_genres (track_id, genre_id, assigned_at, is_enabled, is_manual)
            SELECT DISTINCT analysis.track_id, predictions.model_subgenre_id, $assignedAt, 1, 0
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
            tags.Add(new Tag(reader.GetInt32(0), reader.GetString(1)));
        return tags;
    }

    public Dictionary<int, TrackAudioAnalysis> GetAllTrackAudioAnalyses()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT track_id, bpm, integrated_loudness, loudness_range FROM track_analysis";
        using var reader = cmd.ExecuteReader();
        var results = new Dictionary<int, TrackAudioAnalysis>();
        while (reader.Read())
            results[reader.GetInt32(0)] = new TrackAudioAnalysis(
                reader.IsDBNull(1) ? null : reader.GetDouble(1),
                reader.IsDBNull(2) ? null : reader.GetDouble(2),
                reader.IsDBNull(3) ? null : reader.GetDouble(3));
        return results;
    }

    public Dictionary<int, Dictionary<string, double>> GetAllMirexScores()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT analysis.track_id, signals.signal_key, signals.score
            FROM track_analysis_signals signals
            JOIN track_analysis analysis ON analysis.id = signals.track_analysis_id
            WHERE signals.model_name = 'moods mirex'";
        using var reader = cmd.ExecuteReader();
        var results = new Dictionary<int, Dictionary<string, double>>();
        while (reader.Read())
        {
            var trackId = reader.GetInt32(0);
            if (!results.TryGetValue(trackId, out var scores))
                results[trackId] = scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            scores[reader.GetString(1)] = reader.GetDouble(2);
        }
        return results;
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

    private sealed record RatingSeedDocument(List<RatingSeed> Ratings);
    private sealed record RatingSeed(string Name, int SortOrder);
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
