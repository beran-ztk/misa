using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Resona.Models;

namespace Resona.Cloud.Server;

public enum DeviceLibraryWriteStatus { Success, NotFound, Conflict }

public sealed record DeviceLibraryWriteResult(
    DeviceLibraryWriteStatus Status,
    CloudDeviceLibrarySnapshot? Snapshot = null,
    CloudRevisionConflict? Conflict = null);

public sealed class CloudDeviceLibraryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;

    public CloudDeviceLibraryRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<DeviceLibraryWriteResult> ReplaceAsync(
        Guid userId,
        CloudDeviceLibrarySnapshot incoming,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var current = await ReadForUpdateAsync(connection, transaction, userId, cancellationToken);
        if (current is not null && incoming.LibraryRevision > 0
            && incoming.LibraryRevision != current.LibraryRevision)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict("library", incoming.LibraryRevision, current.LibraryRevision, current);
        }

        var now = DateTimeOffset.UtcNow;
        var revisions = await ReadTrackRevisionsAsync(connection, transaction, userId, cancellationToken);
        var currentTracks = current?.Tracks.ToDictionary(track => track.TrackKey, StringComparer.Ordinal) ?? [];
        var tracks = incoming.Tracks.Select(track =>
        {
            var currentRevision = revisions.GetValueOrDefault(
                track.TrackKey,
                currentTracks.GetValueOrDefault(track.TrackKey)?.Revision ?? 0);
            var changed = !currentTracks.TryGetValue(track.TrackKey, out var oldTrack)
                          || !SameTrackContent(oldTrack, track);
            return track with { Revision = Math.Max(1, changed ? currentRevision + 1 : currentRevision) };
        }).ToList();

        var presetsChanged = current is null || !JsonEquals(current.FilterPresets, incoming.FilterPresets);
        var libraryChanged = current is null
                             || presetsChanged
                             || !JsonEquals(current.Collections, incoming.Collections)
                             || !JsonEquals(current.Ratings, incoming.Ratings)
                             || !JsonEquals(
                                 current.Tracks.Select(WithoutServerFields).ToList(),
                                 tracks.Select(WithoutServerFields).ToList());
        var snapshot = incoming with
        {
            GeneratedAt = libraryChanged ? now.ToString("O") : current!.GeneratedAt,
            LibraryRevision = current is null ? 1 : current.LibraryRevision + (libraryChanged ? 1 : 0),
            PresetsRevision = current is null ? 1 : current.PresetsRevision + (presetsChanged ? 1 : 0),
            Tracks = tracks
        };

        await StoreAsync(connection, transaction, userId, snapshot, cancellationToken);
        await ReplaceTrackRevisionsAsync(connection, transaction, userId, tracks, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DeviceLibraryWriteResult(DeviceLibraryWriteStatus.Success, snapshot);
    }

    public async Task<DeviceLibraryWriteResult> UpdateTrackAsync(
        Guid userId,
        string trackKey,
        CloudTrackUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var snapshot = await ReadForUpdateAsync(connection, transaction, userId, cancellationToken);
        var current = snapshot?.Tracks.FirstOrDefault(track => track.TrackKey == trackKey);
        if (snapshot is null || current is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DeviceLibraryWriteResult(DeviceLibraryWriteStatus.NotFound);
        }
        if (request.ExpectedRevision != current.Revision)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict("track", request.ExpectedRevision, current.Revision, current);
        }

        var now = DateTimeOffset.UtcNow;
        var updated = request.Track with
        {
            TrackKey = trackKey,
            Revision = current.Revision + 1,
            UpdatedAt = now.ToString("O"),
            AudioAvailable = current.AudioAvailable,
            AudioFileSizeBytes = current.AudioFileSizeBytes,
            AudioSha256 = current.AudioSha256
        };
        var tracks = snapshot.Tracks.Select(track => track.TrackKey == trackKey ? updated : track).ToList();
        var result = snapshot with
        {
            Tracks = tracks,
            GeneratedAt = now.ToString("O"),
            LibraryRevision = snapshot.LibraryRevision + 1
        };
        await StoreAsync(connection, transaction, userId, result, cancellationToken);
        await UpsertTrackRevisionAsync(connection, transaction, userId, updated, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DeviceLibraryWriteResult(DeviceLibraryWriteStatus.Success, result);
    }

    public async Task<DeviceLibraryWriteResult> UpdatePresetsAsync(
        Guid userId,
        CloudPresetsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var snapshot = await ReadForUpdateAsync(connection, transaction, userId, cancellationToken);
        if (snapshot is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DeviceLibraryWriteResult(DeviceLibraryWriteStatus.NotFound);
        }
        if (request.ExpectedRevision != snapshot.PresetsRevision)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict("presets", request.ExpectedRevision, snapshot.PresetsRevision, snapshot.FilterPresets);
        }

        var now = DateTimeOffset.UtcNow;
        var result = snapshot with
        {
            FilterPresets = request.Presets,
            GeneratedAt = now.ToString("O"),
            LibraryRevision = snapshot.LibraryRevision + 1,
            PresetsRevision = snapshot.PresetsRevision + 1
        };
        await StoreAsync(connection, transaction, userId, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DeviceLibraryWriteResult(DeviceLibraryWriteStatus.Success, result);
    }

    public async Task<CloudDeviceLibrarySnapshot?> AddDownloadedTrackAsync(
        Guid userId,
        CloudDeviceTrack track,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var snapshot = await ReadForUpdateAsync(connection, transaction, userId, cancellationToken);
        if (snapshot is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
        if (snapshot.Tracks.Any(item => item.TrackKey == track.TrackKey))
        {
            await transaction.CommitAsync(cancellationToken);
            return snapshot;
        }

        var now = DateTimeOffset.UtcNow.ToString("O");
        var added = track with { Revision = 1, UpdatedAt = now, AudioAvailable = true };
        var result = snapshot with
        {
            Tracks = snapshot.Tracks.Append(added)
                .OrderBy(item => item.TrackKey, StringComparer.Ordinal)
                .ToList(),
            TrackCount = snapshot.TrackCount + 1,
            GeneratedAt = now,
            LibraryRevision = snapshot.LibraryRevision + 1
        };
        await StoreAsync(connection, transaction, userId, result, cancellationToken);
        await UpsertTrackRevisionAsync(connection, transaction, userId, added, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<CloudDeviceLibrarySnapshot?> GetCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var snapshot = await ReadAsync(connection, userId, cancellationToken);
        if (snapshot is null)
            return null;

        await using var inventoryCommand = new NpgsqlCommand("""
            SELECT track_key, file_size_bytes, sha256
            FROM library_media
            WHERE owner_user_id = @userId;
            """, connection);
        inventoryCommand.Parameters.AddWithValue("userId", userId);
        await using var reader = await inventoryCommand.ExecuteReaderAsync(cancellationToken);
        var media = new Dictionary<string, (long Size, string Sha256)>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
            media[reader.GetString(0)] = (reader.GetInt64(1), reader.GetString(2));

        return snapshot with
        {
            Tracks = snapshot.Tracks.Select(track => media.TryGetValue(track.TrackKey, out var file)
                ? track with { AudioAvailable = true, AudioFileSizeBytes = file.Size, AudioSha256 = file.Sha256 }
                : track with { AudioAvailable = false, AudioFileSizeBytes = null, AudioSha256 = null })
                .ToList()
        };
    }

    private static DeviceLibraryWriteResult Conflict(string entity, long expected, long current, object value) =>
        new(DeviceLibraryWriteStatus.Conflict, Conflict: new CloudRevisionConflict(entity, expected, current, value));

    private static bool SameTrackContent(CloudDeviceTrack left, CloudDeviceTrack right) =>
        JsonEquals(WithoutServerFields(left), WithoutServerFields(right));

    private static CloudDeviceTrack WithoutServerFields(CloudDeviceTrack track) => track with
    {
        Revision = 0,
        AudioAvailable = false,
        AudioFileSizeBytes = null,
        AudioSha256 = null
    };

    private static bool JsonEquals<T>(T left, T right) =>
        JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions);

    private static async Task<CloudDeviceLibrarySnapshot?> ReadAsync(
        NpgsqlConnection connection,
        Guid userId,
        CancellationToken cancellationToken,
        NpgsqlTransaction? transaction = null,
        bool forUpdate = false)
    {
        await using var command = new NpgsqlCommand($"""
            SELECT snapshot_json, library_revision, presets_revision
            FROM device_library_snapshots
            WHERE user_id = @userId{(forUpdate ? " FOR UPDATE" : string.Empty)};
            """, connection, transaction);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        var snapshot = JsonSerializer.Deserialize<CloudDeviceLibrarySnapshot>(reader.GetString(0), JsonOptions);
        return snapshot is null ? null : snapshot with
        {
            LibraryRevision = reader.GetInt64(1),
            PresetsRevision = reader.GetInt64(2)
        };
    }

    private static Task<CloudDeviceLibrarySnapshot?> ReadForUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        CancellationToken cancellationToken) =>
        ReadAsync(connection, userId, cancellationToken, transaction, forUpdate: true);

    private static async Task StoreAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        CloudDeviceLibrarySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO device_library_snapshots
                (user_id, schema_version, track_count, generated_at, snapshot_json,
                 synchronized_at, library_revision, presets_revision)
            VALUES (@userId, @schemaVersion, @trackCount, @generatedAt, @snapshot,
                    now(), @libraryRevision, @presetsRevision)
            ON CONFLICT (user_id) DO UPDATE SET
                schema_version = excluded.schema_version,
                track_count = excluded.track_count,
                generated_at = excluded.generated_at,
                snapshot_json = excluded.snapshot_json,
                synchronized_at = excluded.synchronized_at,
                library_revision = excluded.library_revision,
                presets_revision = excluded.presets_revision;
            """, connection, transaction);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("schemaVersion", snapshot.SchemaVersion);
        command.Parameters.AddWithValue("trackCount", snapshot.TrackCount);
        command.Parameters.AddWithValue("generatedAt", DateTimeOffset.Parse(snapshot.GeneratedAt));
        command.Parameters.Add("snapshot", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(snapshot, JsonOptions);
        command.Parameters.AddWithValue("libraryRevision", snapshot.LibraryRevision);
        command.Parameters.AddWithValue("presetsRevision", snapshot.PresetsRevision);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, long>> ReadTrackRevisionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT track_key, revision FROM device_library_track_revisions WHERE user_id = @userId;
            """, connection, transaction);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
            result[reader.GetString(0)] = reader.GetInt64(1);
        return result;
    }

    private static async Task ReplaceTrackRevisionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        IReadOnlyList<CloudDeviceTrack> tracks,
        CancellationToken cancellationToken)
    {
        await using (var delete = new NpgsqlCommand("""
            DELETE FROM device_library_track_revisions
            WHERE user_id = @userId AND NOT (track_key = ANY(@trackKeys));
            """, connection, transaction))
        {
            delete.Parameters.AddWithValue("userId", userId);
            delete.Parameters.AddWithValue("trackKeys", tracks.Select(track => track.TrackKey).ToArray());
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var track in tracks)
            await UpsertTrackRevisionAsync(connection, transaction, userId, track, cancellationToken);
    }

    private static async Task UpsertTrackRevisionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        CloudDeviceTrack track,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO device_library_track_revisions (user_id, track_key, revision, updated_at)
            VALUES (@userId, @trackKey, @revision, now())
            ON CONFLICT (user_id, track_key) DO UPDATE SET
                revision = excluded.revision,
                updated_at = excluded.updated_at;
            """, connection, transaction);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("trackKey", track.TrackKey);
        command.Parameters.AddWithValue("revision", track.Revision);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
