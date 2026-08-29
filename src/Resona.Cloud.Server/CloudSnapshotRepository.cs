using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Resona.Models;

namespace Resona.Cloud.Server;

public enum SnapshotReplaceResult { Success, Unauthorized, DeviceConflict, EmptySnapshotRejected }

public sealed class CloudSnapshotRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;

    public CloudSnapshotRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<bool> AuthenticateDeviceAsync(
        DeviceCredentials credentials,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT user_id, device_key_hash FROM user_devices WHERE id = @deviceId");
        command.Parameters.AddWithValue("deviceId", credentials.DeviceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return false;

        return reader.GetGuid(0) == credentials.UserId
               && CryptographicOperations.FixedTimeEquals(
                   reader.GetFieldValue<byte[]>(1),
                   credentials.DeviceKeyHash);
    }

    public async Task<SnapshotReplaceResult> ReplaceSnapshotAsync(
        DeviceCredentials credentials,
        CloudLibrarySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync(connection, transaction,
            "SELECT pg_advisory_xact_lock(hashtextextended(@userId::text, 0))",
            cancellationToken, ("userId", credentials.UserId));

        var authentication = await AuthenticateOrRegisterAsync(
            connection, transaction, credentials, snapshot.Profile, cancellationToken);
        if (authentication != SnapshotReplaceResult.Success)
        {
            await transaction.RollbackAsync(cancellationToken);
            return authentication;
        }

        if (snapshot.TrackCount == 0 && await ExistingTrackCountAsync(
                connection, transaction, credentials.UserId, cancellationToken) > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return SnapshotReplaceResult.EmptySnapshotRejected;
        }

        await ExecuteAsync(connection, transaction, """
            UPDATE users
            SET username = @username,
                bio = @bio,
                profile_image = @profileImage,
                profile_updated_at = @profileUpdatedAt,
                updated_at = now()
            WHERE id = @userId;

            UPDATE user_devices SET last_seen_at = now() WHERE id = @deviceId;

            DELETE FROM public_tracks WHERE owner_user_id = @userId;
            """, cancellationToken,
            ("username", snapshot.Profile.Username.Trim()),
            ("bio", snapshot.Profile.Bio.Trim()),
            ("profileImage", (object?)snapshot.Profile.ProfileImage ?? DBNull.Value),
            ("profileUpdatedAt", DateTimeOffset.Parse(snapshot.Profile.UpdatedAt)),
            ("userId", credentials.UserId),
            ("deviceId", credentials.DeviceId));

        var tracksJson = JsonSerializer.Serialize(snapshot.Tracks, JsonOptions);
        await InsertTracksAsync(connection, transaction, credentials.UserId, tracksJson, cancellationToken);

        await ExecuteAsync(connection, transaction, """
            INSERT INTO library_snapshots
                (user_id, schema_version, track_count, generated_at, synchronized_at)
            VALUES (@userId, @schemaVersion, @trackCount, @generatedAt, now())
            ON CONFLICT (user_id) DO UPDATE SET
                schema_version = excluded.schema_version,
                track_count = excluded.track_count,
                generated_at = excluded.generated_at,
                synchronized_at = excluded.synchronized_at;
            """, cancellationToken,
            ("userId", credentials.UserId),
            ("schemaVersion", snapshot.SchemaVersion),
            ("trackCount", snapshot.TrackCount),
            ("generatedAt", DateTimeOffset.Parse(snapshot.GeneratedAt)));

        await transaction.CommitAsync(cancellationToken);
        return SnapshotReplaceResult.Success;
    }

    private static async Task<SnapshotReplaceResult> AuthenticateOrRegisterAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DeviceCredentials credentials,
        CloudPublicProfile profile,
        CancellationToken cancellationToken)
    {
        await using (var command = new NpgsqlCommand(
            "SELECT user_id, device_key_hash FROM user_devices WHERE id = @deviceId FOR UPDATE",
            connection, transaction))
        {
            command.Parameters.AddWithValue("deviceId", credentials.DeviceId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var userId = reader.GetGuid(0);
                var storedHash = reader.GetFieldValue<byte[]>(1);
                return userId == credentials.UserId
                       && CryptographicOperations.FixedTimeEquals(storedHash, credentials.DeviceKeyHash)
                    ? SnapshotReplaceResult.Success
                    : SnapshotReplaceResult.Unauthorized;
            }
        }

        await using (var exists = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM users WHERE id = @userId)", connection, transaction))
        {
            exists.Parameters.AddWithValue("userId", credentials.UserId);
            if ((bool)(await exists.ExecuteScalarAsync(cancellationToken))!)
                return SnapshotReplaceResult.DeviceConflict;
        }

        await ExecuteAsync(connection, transaction, """
            INSERT INTO users
                (id, username, bio, profile_image, profile_updated_at, created_at, updated_at)
            VALUES (@userId, @username, @bio, @profileImage, @profileUpdatedAt, now(), now());

            INSERT INTO user_devices
                (id, user_id, device_key_hash, registered_at, last_seen_at)
            VALUES (@deviceId, @userId, @deviceKeyHash, now(), now());
            """, cancellationToken,
            ("userId", credentials.UserId),
            ("username", profile.Username.Trim()),
            ("bio", profile.Bio.Trim()),
            ("profileImage", (object?)profile.ProfileImage ?? DBNull.Value),
            ("profileUpdatedAt", DateTimeOffset.Parse(profile.UpdatedAt)),
            ("deviceId", credentials.DeviceId),
            ("deviceKeyHash", credentials.DeviceKeyHash));
        return SnapshotReplaceResult.Success;
    }

    private static async Task<long> ExistingTrackCountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM public_tracks WHERE owner_user_id = @userId",
            connection, transaction);
        command.Parameters.AddWithValue("userId", userId);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task InsertTracksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userId,
        string tracksJson,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(InsertSnapshotSql, connection, transaction);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.Add("tracks", NpgsqlDbType.Jsonb).Value = tracksJson;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string InsertSnapshotSql = """
        CREATE TEMP TABLE snapshot_tracks ON COMMIT DROP AS
        SELECT * FROM jsonb_to_recordset(@tracks) AS item(
            "sourceVideoId" text,
            "canonicalUrl" text,
            "title" text,
            "originalTitle" text,
            "channelName" text,
            "channelUrl" text,
            "durationSeconds" integer,
            "uploadedAt" text,
            "thumbnailUrl" text,
            "rating" text,
            "languageCode" text,
            "tags" jsonb,
            "genres" jsonb,
            "analysis" jsonb,
            "emotionalCharacter" jsonb,
            "updatedAt" text
        );

        INSERT INTO public_tracks
            (owner_user_id, source_video_id, canonical_url, title, original_title,
             channel_name, channel_url, duration_seconds, uploaded_at, thumbnail_url,
             rating, language_code, source_updated_at, synchronized_at)
        SELECT @userId, "sourceVideoId", "canonicalUrl", "title", "originalTitle",
               "channelName", "channelUrl", "durationSeconds",
               NULLIF("uploadedAt", ''), "thumbnailUrl", "rating", "languageCode",
               "updatedAt"::timestamptz, now()
        FROM snapshot_tracks;

        INSERT INTO public_track_tags (owner_user_id, source_video_id, name)
        SELECT @userId, source."sourceVideoId", value
        FROM snapshot_tracks source
        CROSS JOIN LATERAL jsonb_array_elements_text(source."tags") value;

        INSERT INTO public_track_genres (owner_user_id, source_video_id, name)
        SELECT @userId, source."sourceVideoId", value
        FROM snapshot_tracks source
        CROSS JOIN LATERAL jsonb_array_elements_text(source."genres") value;

        INSERT INTO public_track_analysis
            (owner_user_id, source_video_id, bpm, integrated_loudness, loudness_range)
        SELECT @userId, "sourceVideoId",
               ("analysis"->>'bpm')::double precision,
               ("analysis"->>'integratedLoudness')::double precision,
               ("analysis"->>'loudnessRange')::double precision
        FROM snapshot_tracks
        WHERE "analysis" IS NOT NULL AND "analysis" <> 'null'::jsonb;

        INSERT INTO public_track_emotional_character
            (owner_user_id, source_video_id, name, score)
        SELECT @userId, source."sourceVideoId", value.key, value.value::double precision
        FROM snapshot_tracks source
        CROSS JOIN LATERAL jsonb_each_text(source."emotionalCharacter") value;
        """;
}
