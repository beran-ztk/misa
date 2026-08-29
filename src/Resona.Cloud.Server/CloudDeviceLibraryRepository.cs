using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Resona.Models;

namespace Resona.Cloud.Server;

public sealed class CloudDeviceLibraryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;

    public CloudDeviceLibraryRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task ReplaceAsync(
        Guid userId,
        CloudDeviceLibrarySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            INSERT INTO device_library_snapshots
                (user_id, schema_version, track_count, generated_at, snapshot_json, synchronized_at)
            VALUES (@userId, @schemaVersion, @trackCount, @generatedAt, @snapshot, now())
            ON CONFLICT (user_id) DO UPDATE SET
                schema_version = excluded.schema_version,
                track_count = excluded.track_count,
                generated_at = excluded.generated_at,
                snapshot_json = excluded.snapshot_json,
                synchronized_at = excluded.synchronized_at;
            """);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("schemaVersion", snapshot.SchemaVersion);
        command.Parameters.AddWithValue("trackCount", snapshot.TrackCount);
        command.Parameters.AddWithValue("generatedAt", DateTimeOffset.Parse(snapshot.GeneratedAt));
        command.Parameters.Add("snapshot", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(snapshot, JsonOptions);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<CloudDeviceLibrarySnapshot?> GetCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT snapshot_json
            FROM device_library_snapshots
            WHERE user_id = @userId;
            """);
        command.Parameters.AddWithValue("userId", userId);
        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(json))
            return null;
        var snapshot = JsonSerializer.Deserialize<CloudDeviceLibrarySnapshot>(json, JsonOptions);
        if (snapshot is null)
            return null;

        await using var inventoryCommand = _dataSource.CreateCommand("""
            SELECT track_key, file_size_bytes, sha256
            FROM library_media
            WHERE owner_user_id = @userId;
            """);
        inventoryCommand.Parameters.AddWithValue("userId", userId);
        await using var reader = await inventoryCommand.ExecuteReaderAsync(cancellationToken);
        var media = new Dictionary<string, (long Size, string Sha256)>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
            media[reader.GetString(0)] = (reader.GetInt64(1), reader.GetString(2));

        return snapshot with
        {
            Tracks = snapshot.Tracks.Select(track => media.TryGetValue(track.TrackKey, out var file)
                ? track with
                {
                    AudioAvailable = true,
                    AudioFileSizeBytes = file.Size,
                    AudioSha256 = file.Sha256
                }
                : track with
                {
                    AudioAvailable = false,
                    AudioFileSizeBytes = null,
                    AudioSha256 = null
                }).ToList()
        };
    }
}
