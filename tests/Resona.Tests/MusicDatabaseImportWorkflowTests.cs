using Microsoft.Data.Sqlite;
using Resona.Models;
using Resona.Services;

namespace Resona.Tests;

public sealed class MusicDatabaseImportWorkflowTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"resona-tests-{Guid.NewGuid():N}");
    private readonly string _databasePath;
    private readonly MusicDatabase _database;

    public MusicDatabaseImportWorkflowTests()
    {
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "workflow.db");
        CreateMinimalSchema();
        _database = new MusicDatabase(_databasePath);
    }

    [Fact]
    public void Claim_changes_exactly_one_oldest_item_to_downloading()
    {
        var first = InsertQueueItem("Queued", "one", createdAt: "2026-01-01T00:00:00Z");
        var second = InsertQueueItem("Queued", "two", createdAt: "2026-01-02T00:00:00Z");

        var claimed = _database.ClaimNextQueuedImport();

        Assert.NotNull(claimed);
        Assert.Equal(first, claimed.Id);
        Assert.Equal(ImportQueueStatus.Downloading, claimed.Status);
        Assert.Equal("Downloading", Scalar<string>("SELECT status FROM import_queue_items WHERE id = $id", ("$id", first)));
        Assert.Equal("Queued", Scalar<string>("SELECT status FROM import_queue_items WHERE id = $id", ("$id", second)));
    }

    [Fact]
    public void Invalid_transition_is_rejected_without_mutating_item()
    {
        var itemId = InsertQueueItem("Queued", "one");

        Assert.Throws<InvalidOperationException>(() =>
            _database.UpdateImportQueueItem(itemId, ImportQueueStatus.ReadyForReview));

        Assert.Equal("Queued", Scalar<string>("SELECT status FROM import_queue_items WHERE id = $id", ("$id", itemId)));
    }

    [Fact]
    public void Completion_atomically_promotes_unrated_track_and_removes_empty_batch()
    {
        var itemId = InsertQueueItem("Downloading", "one");
        var batchId = Scalar<long>("SELECT batch_id FROM import_queue_items WHERE id = $id", ("$id", itemId));
        var trackId = InsertTrack(ratingId: null, state: "Active", needsReview: 0);

        _database.CompleteImportQueueItem(itemId, trackId);

        Assert.Equal("PendingRating", Scalar<string>("SELECT library_state FROM tracks WHERE id = $id", ("$id", trackId)));
        Assert.Equal(1L, Scalar<long>("SELECT needs_reevaluation FROM tracks WHERE id = $id", ("$id", trackId)));
        Assert.Equal(0L, Scalar<long>("SELECT COUNT(*) FROM import_queue_items WHERE id = $id", ("$id", itemId)));
        Assert.Equal(0L, Scalar<long>("SELECT COUNT(*) FROM import_batches WHERE id = $id", ("$id", batchId)));
    }

    [Fact]
    public void Missing_queue_item_rolls_back_track_transition()
    {
        var trackId = InsertTrack(ratingId: null, state: "Active", needsReview: 0);

        Assert.Throws<InvalidOperationException>(() => _database.CompleteImportQueueItem(999, trackId));

        Assert.Equal("Active", Scalar<string>("SELECT library_state FROM tracks WHERE id = $id", ("$id", trackId)));
        Assert.Equal(0L, Scalar<long>("SELECT needs_reevaluation FROM tracks WHERE id = $id", ("$id", trackId)));
    }

    [Fact]
    public void Removing_last_queued_item_also_removes_its_empty_batch()
    {
        var itemId = InsertQueueItem("Queued", "one");
        var batchId = Scalar<long>("SELECT batch_id FROM import_queue_items WHERE id = $id", ("$id", itemId));

        Assert.True(_database.RemoveQueuedImport(itemId));

        Assert.Equal(0L, Scalar<long>("SELECT COUNT(*) FROM import_queue_items WHERE id = $id", ("$id", itemId)));
        Assert.Equal(0L, Scalar<long>("SELECT COUNT(*) FROM import_batches WHERE id = $id", ("$id", batchId)));
    }

    [Fact]
    public void Production_guards_reject_invalid_track_states()
    {
        using var connection = Open();
        MusicDatabase.EnsureTrackWorkflowGuards(connection);

        var invalidInsert = connection.CreateCommand();
        invalidInsert.CommandText = @"INSERT INTO tracks
            (rating_id, title, file_name, downloaded_at, updated_at, library_state, needs_reevaluation)
            VALUES (NULL, 'Invalid', 'invalid.mp3', $now, $now, 'Active', 0)";
        invalidInsert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        Assert.Throws<SqliteException>(() => invalidInsert.ExecuteNonQuery());

        var trackId = InsertTrack(ratingId: null, state: "PendingRating", needsReview: 1);
        var invalidUpdate = connection.CreateCommand();
        invalidUpdate.CommandText = "UPDATE tracks SET needs_reevaluation = 0 WHERE id = $id";
        invalidUpdate.Parameters.AddWithValue("$id", trackId);
        Assert.Throws<SqliteException>(() => invalidUpdate.ExecuteNonQuery());
    }

    [Fact]
    public void Production_normalization_repairs_legacy_rows_before_guards_are_created()
    {
        var unratedId = InsertTrack(ratingId: null, state: "Active", needsReview: 0);
        var ratedId = InsertTrack(ratingId: 4, state: "PendingRating", needsReview: 1);
        var rejectedId = InsertTrack(ratingId: 1, state: "Rejected", needsReview: 1);

        using var connection = Open();
        MusicDatabase.NormalizeTrackWorkflowState(connection);
        MusicDatabase.EnsureTrackWorkflowGuards(connection);

        Assert.Equal("PendingRating", Scalar<string>("SELECT library_state FROM tracks WHERE id = $id", ("$id", unratedId)));
        Assert.Equal(1L, Scalar<long>("SELECT needs_reevaluation FROM tracks WHERE id = $id", ("$id", unratedId)));
        Assert.Equal("Active", Scalar<string>("SELECT library_state FROM tracks WHERE id = $id", ("$id", ratedId)));
        Assert.Equal(0L, Scalar<long>("SELECT needs_reevaluation FROM tracks WHERE id = $id", ("$id", rejectedId)));
        Assert.Equal(1L, Scalar<long>("SELECT analysis_disabled FROM tracks WHERE id = $id", ("$id", rejectedId)));
    }

    [Fact]
    public void Retrying_download_failure_requeues_only_that_video()
    {
        var failedId = InsertChannelVideo("Failed", "Ready", "network error", null, 3);
        var skippedId = InsertChannelVideo("Skipped", "Ready", null, null, 0);

        Assert.True(_database.RetryChannelVideoIssue(failedId));

        Assert.Equal("Queued", Scalar<string>("SELECT download_status FROM channel_videos WHERE id = $id", ("$id", failedId)));
        Assert.Equal(0L, Scalar<long>("SELECT download_attempts FROM channel_videos WHERE id = $id", ("$id", failedId)));
        Assert.Equal(1L, Scalar<long>("SELECT manual_download_requested FROM channel_videos WHERE id = $id", ("$id", failedId)));
        Assert.Equal(0L, Scalar<long>("SELECT COUNT(download_error) FROM channel_videos WHERE id = $id", ("$id", failedId)));
        Assert.Equal("Skipped", Scalar<string>("SELECT download_status FROM channel_videos WHERE id = $id", ("$id", skippedId)));
    }

    [Fact]
    public void Retrying_metadata_failure_preserves_duration_skip()
    {
        var videoId = InsertChannelVideo("Skipped", "Failed", null, "metadata error", 0);

        Assert.True(_database.RetryChannelVideoIssue(videoId));

        Assert.Equal("Queued", Scalar<string>("SELECT metadata_status FROM channel_videos WHERE id = $id", ("$id", videoId)));
        Assert.Equal(200L, Scalar<long>("SELECT metadata_priority FROM channel_videos WHERE id = $id", ("$id", videoId)));
        Assert.Equal(0L, Scalar<long>("SELECT metadata_attempts FROM channel_videos WHERE id = $id", ("$id", videoId)));
        Assert.Equal(0L, Scalar<long>("SELECT COUNT(metadata_error) FROM channel_videos WHERE id = $id", ("$id", videoId)));
        Assert.Equal("Skipped", Scalar<string>("SELECT download_status FROM channel_videos WHERE id = $id", ("$id", videoId)));
    }

    [Fact]
    public void Duration_skip_without_real_error_cannot_be_retried_as_issue()
    {
        var videoId = InsertChannelVideo("Skipped", "Ready", null, null, 0);

        Assert.False(_database.RetryChannelVideoIssue(videoId));

        Assert.Equal("Skipped", Scalar<string>("SELECT download_status FROM channel_videos WHERE id = $id", ("$id", videoId)));
    }

    private int InsertQueueItem(string status, string suffix, string createdAt = "2026-01-01T00:00:00Z")
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var batchId = Insert(connection, transaction,
            "INSERT INTO import_batches(source_url, created_at) VALUES ($url, $createdAt)",
            ("$url", $"https://example.test/{suffix}"), ("$createdAt", createdAt));
        var itemId = Insert(connection, transaction, @"INSERT INTO import_queue_items
            (batch_id, source_url, canonical_url, title, status, created_at, updated_at)
            VALUES ($batchId, $url, $canonicalUrl, $title, $status, $createdAt, $createdAt)",
            ("$batchId", batchId), ("$url", $"https://example.test/{suffix}"),
            ("$canonicalUrl", $"https://youtu.be/{suffix}"), ("$title", suffix),
            ("$status", status), ("$createdAt", createdAt));
        transaction.Commit();
        return itemId;
    }

    private int InsertTrack(int? ratingId, string state, int needsReview)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO tracks
            (rating_id, title, file_name, downloaded_at, updated_at, library_state, needs_reevaluation)
            VALUES ($ratingId, 'Track', $fileName, $now, $now, $state, $needsReview);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$ratingId", ratingId is null ? DBNull.Value : ratingId.Value);
        command.Parameters.AddWithValue("$fileName", $"{Guid.NewGuid():N}.mp3");
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$state", state);
        command.Parameters.AddWithValue("$needsReview", needsReview);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private int InsertChannelVideo(
        string downloadStatus,
        string metadataStatus,
        string? downloadError,
        string? metadataError,
        int downloadAttempts)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var suffix = Guid.NewGuid().ToString("N");
        var id = Insert(connection, transaction, @"INSERT INTO channel_videos
            (canonical_url, download_status, download_error, download_attempts,
             manual_download_requested, metadata_status, metadata_error,
             metadata_attempts, metadata_priority, updated_at)
            VALUES ($url, $downloadStatus, $downloadError, $downloadAttempts,
                    0, $metadataStatus, $metadataError, 4, 0, $now)",
            ("$url", $"https://youtu.be/{suffix}"),
            ("$downloadStatus", downloadStatus),
            ("$downloadError", (object?)downloadError ?? DBNull.Value),
            ("$downloadAttempts", downloadAttempts),
            ("$metadataStatus", metadataStatus),
            ("$metadataError", (object?)metadataError ?? DBNull.Value),
            ("$now", DateTime.UtcNow.ToString("O")));
        transaction.Commit();
        return id;
    }

    private void CreateMinimalSchema()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            PRAGMA foreign_keys = ON;
            CREATE TABLE tracks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                rating_id INTEGER NULL,
                canonical_url TEXT NULL UNIQUE,
                title TEXT NOT NULL,
                file_name TEXT NOT NULL UNIQUE,
                downloaded_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                library_state TEXT NOT NULL,
                needs_reevaluation INTEGER NOT NULL,
                analysis_disabled INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE import_batches (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_url TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE TABLE import_queue_items (
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
            CREATE TABLE channel_videos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                canonical_url TEXT NOT NULL UNIQUE,
                download_status TEXT NOT NULL,
                download_error TEXT NULL,
                download_attempts INTEGER NOT NULL DEFAULT 0,
                manual_download_requested INTEGER NOT NULL DEFAULT 0,
                metadata_status TEXT NOT NULL,
                metadata_error TEXT NULL,
                metadata_attempts INTEGER NOT NULL DEFAULT 0,
                metadata_priority INTEGER NOT NULL DEFAULT 0,
                updated_at TEXT NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON";
        command.ExecuteNonQuery();
        return connection;
    }

    private T Scalar<T>(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    private static int Insert(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"{sql}; SELECT last_insert_rowid();";
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A failing test must remain the primary failure; temp cleanup is best effort.
        }
    }
}
