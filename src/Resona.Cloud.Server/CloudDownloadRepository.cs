using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using Resona.Models;

namespace Resona.Cloud.Server;

public sealed record ClaimedCloudDownload(
    Guid JobId,
    Guid UserId,
    CloudDownloadRequest Request);

public sealed class CloudDownloadRepository(NpgsqlDataSource dataSource)
{
    public async Task<CloudDownloadJob> EnqueueAsync(
        Guid userId,
        CloudDownloadRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var command = dataSource.CreateCommand("""
            INSERT INTO library_download_jobs (id, user_id, url, status, request_json)
            VALUES (@id, @userId, @url, 'Queued', @request)
            RETURNING created_at, updated_at;
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("url", request.Url);
        command.Parameters.Add("request", NpgsqlDbType.Jsonb).Value =
            JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new CloudDownloadJob(
            id.ToString("D"), request.Url, "Queued", 0, null, null, null,
            reader.GetDateTime(0).ToUniversalTime().ToString("O"),
            reader.GetDateTime(1).ToUniversalTime().ToString("O"));
    }

    public async Task<IReadOnlyList<CloudDownloadJob>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT id, url, status, progress_percent, track_key, title, error, created_at, updated_at
            FROM library_download_jobs
            WHERE user_id = @userId
            ORDER BY created_at DESC
            LIMIT 100;
            """);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var jobs = new List<CloudDownloadJob>();
        while (await reader.ReadAsync(cancellationToken))
            jobs.Add(Read(reader));
        return jobs;
    }

    public async Task<ClaimedCloudDownload?> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT id, user_id, url, request_json
            FROM library_download_jobs
            WHERE status = 'Queued'
            ORDER BY created_at
            FOR UPDATE SKIP LOCKED
            LIMIT 1;
            """, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await reader.CloseAsync();
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        var request = reader.IsDBNull(3)
            ? new CloudDownloadRequest(reader.GetString(2))
            : JsonSerializer.Deserialize<CloudDownloadRequest>(
                  reader.GetString(3), new JsonSerializerOptions(JsonSerializerDefaults.Web))
              ?? new CloudDownloadRequest(reader.GetString(2));
        var claimed = new ClaimedCloudDownload(reader.GetGuid(0), reader.GetGuid(1), request);
        await reader.CloseAsync();
        await using var update = new NpgsqlCommand("""
            UPDATE library_download_jobs
            SET status = 'Downloading', progress_percent = 10, updated_at = now(), error = NULL
            WHERE id = @id;
            """, connection, transaction);
        update.Parameters.AddWithValue("id", claimed.JobId);
        await update.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claimed;
    }

    public async Task SetStateAsync(
        Guid jobId,
        string status,
        int progress,
        string? trackKey,
        string? title,
        string? error,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE library_download_jobs SET
                status = @status,
                progress_percent = @progress,
                track_key = COALESCE(@trackKey, track_key),
                title = COALESCE(@title, title),
                error = @error,
                updated_at = now()
            WHERE id = @id;
            """);
        command.Parameters.AddWithValue("id", jobId);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("progress", Math.Clamp(progress, 0, 100));
        command.Parameters.AddWithValue("trackKey", (object?)trackKey ?? DBNull.Value);
        command.Parameters.AddWithValue("title", (object?)title ?? DBNull.Value);
        command.Parameters.AddWithValue("error", (object?)error ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RecoverInterruptedAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE library_download_jobs
            SET status = 'Queued', progress_percent = 0,
                error = 'Server restarted; job queued again.', updated_at = now()
            WHERE status IN ('Downloading', 'Analyzing');
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static CloudDownloadJob Read(NpgsqlDataReader reader) => new(
        reader.GetGuid(0).ToString("D"),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetInt32(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetString(6),
        reader.GetDateTime(7).ToUniversalTime().ToString("O"),
        reader.GetDateTime(8).ToUniversalTime().ToString("O"));
}
