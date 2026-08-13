using Npgsql;
using Resona.Models;

namespace Resona.Cloud.Server;

public sealed class CloudPublicLibraryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CloudPublicLibraryRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<CloudPage<CloudPublicProfileSummary>> GetProfilesAsync(
        PublicLibraryQuery query,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT count(*)
            FROM users u
            WHERE @search = ''
               OR u.username ILIKE '%' || @search || '%'
               OR u.bio ILIKE '%' || @search || '%';

            SELECT u.id, u.username, u.bio, u.profile_image IS NOT NULL,
                   COALESCE(s.track_count, 0), u.updated_at, s.synchronized_at
            FROM users u
            LEFT JOIN library_snapshots s ON s.user_id = u.id
            WHERE @search = ''
               OR u.username ILIKE '%' || @search || '%'
               OR u.bio ILIKE '%' || @search || '%'
            ORDER BY lower(u.username), u.id
            OFFSET @offset LIMIT @limit;
            """);
        AddQueryParameters(command, query);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var total = reader.GetInt64(0);

        await reader.NextResultAsync(cancellationToken);
        var items = new List<CloudPublicProfileSummary>();
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadProfile(reader));

        return new CloudPage<CloudPublicProfileSummary>(items, query.Offset, query.Limit, total);
    }

    public async Task<CloudPublicProfileSummary?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT u.id, u.username, u.bio, u.profile_image IS NOT NULL,
                   COALESCE(s.track_count, 0), u.updated_at, s.synchronized_at
            FROM users u
            LEFT JOIN library_snapshots s ON s.user_id = u.id
            WHERE u.id = @userId;
            """);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProfile(reader) : null;
    }

    public async Task<byte[]?> GetProfileImageAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT profile_image FROM users WHERE id = @userId");
        command.Parameters.AddWithValue("userId", userId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is byte[] image ? image : null;
    }

    public async Task<CloudPage<CloudPublicLibraryTrack>?> GetTracksAsync(
        Guid userId,
        PublicLibraryQuery query,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT EXISTS(SELECT 1 FROM users WHERE id = @userId);

            SELECT count(*)
            FROM public_tracks t
            WHERE t.owner_user_id = @userId
              AND (@search = ''
                   OR t.title ILIKE '%' || @search || '%'
                   OR t.original_title ILIKE '%' || @search || '%'
                   OR COALESCE(t.channel_name, '') ILIKE '%' || @search || '%');

            SELECT t.source_video_id, t.canonical_url, t.title, t.original_title,
                   t.channel_name, t.channel_url, t.duration_seconds, t.uploaded_at,
                   t.thumbnail_url, t.rating, t.language_code,
                   ARRAY(
                       SELECT tag.name
                       FROM public_track_tags tag
                       WHERE tag.owner_user_id = t.owner_user_id
                         AND tag.source_video_id = t.source_video_id
                       ORDER BY lower(tag.name), tag.name
                   ),
                   ARRAY(
                       SELECT genre.name
                       FROM public_track_genres genre
                       WHERE genre.owner_user_id = t.owner_user_id
                         AND genre.source_video_id = t.source_video_id
                       ORDER BY lower(genre.name), genre.name
                   ),
                   analysis.bpm, analysis.integrated_loudness, analysis.loudness_range,
                   ARRAY(
                       SELECT emotion.name
                       FROM public_track_emotional_character emotion
                       WHERE emotion.owner_user_id = t.owner_user_id
                         AND emotion.source_video_id = t.source_video_id
                       ORDER BY lower(emotion.name), emotion.name
                   ),
                   ARRAY(
                       SELECT emotion.score
                       FROM public_track_emotional_character emotion
                       WHERE emotion.owner_user_id = t.owner_user_id
                         AND emotion.source_video_id = t.source_video_id
                       ORDER BY lower(emotion.name), emotion.name
                   ),
                   t.source_updated_at
            FROM public_tracks t
            LEFT JOIN public_track_analysis analysis
              ON analysis.owner_user_id = t.owner_user_id
             AND analysis.source_video_id = t.source_video_id
            WHERE t.owner_user_id = @userId
              AND (@search = ''
                   OR t.title ILIKE '%' || @search || '%'
                   OR t.original_title ILIKE '%' || @search || '%'
                   OR COALESCE(t.channel_name, '') ILIKE '%' || @search || '%')
            ORDER BY lower(t.title), t.source_video_id
            OFFSET @offset LIMIT @limit;
            """);
        command.Parameters.AddWithValue("userId", userId);
        AddQueryParameters(command, query);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        if (!reader.GetBoolean(0))
            return null;

        await reader.NextResultAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var total = reader.GetInt64(0);

        await reader.NextResultAsync(cancellationToken);
        var items = new List<CloudPublicLibraryTrack>();
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadTrack(reader));

        return new CloudPage<CloudPublicLibraryTrack>(items, query.Offset, query.Limit, total);
    }

    private static void AddQueryParameters(NpgsqlCommand command, PublicLibraryQuery query)
    {
        command.Parameters.AddWithValue("search", query.Search);
        command.Parameters.AddWithValue("offset", query.Offset);
        command.Parameters.AddWithValue("limit", query.Limit);
    }

    private static CloudPublicProfileSummary ReadProfile(NpgsqlDataReader reader) => new(
        reader.GetGuid(0).ToString("D"),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetBoolean(3),
        reader.GetInt32(4),
        ReadUtc(reader, 5),
        reader.IsDBNull(6) ? null : ReadUtc(reader, 6));

    private static CloudPublicLibraryTrack ReadTrack(NpgsqlDataReader reader)
    {
        var emotionalNames = reader.GetFieldValue<string[]>(16);
        var emotionalScores = reader.GetFieldValue<double[]>(17);
        var emotionalCharacter = emotionalNames
            .Select((name, index) => new { Name = name, Score = emotionalScores[index] })
            .ToDictionary(item => item.Name, item => item.Score, StringComparer.OrdinalIgnoreCase);
        var hasAnalysis = !reader.IsDBNull(13) || !reader.IsDBNull(14) || !reader.IsDBNull(15);

        return new CloudPublicLibraryTrack(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetInt32(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.GetFieldValue<string[]>(11),
            reader.GetFieldValue<string[]>(12),
            hasAnalysis
                ? new CloudPublicTrackAnalysis(
                    reader.IsDBNull(13) ? null : reader.GetDouble(13),
                    reader.IsDBNull(14) ? null : reader.GetDouble(14),
                    reader.IsDBNull(15) ? null : reader.GetDouble(15))
                : null,
            emotionalCharacter,
            ReadUtc(reader, 18));
    }

    private static string ReadUtc(NpgsqlDataReader reader, int ordinal) =>
        reader.GetDateTime(ordinal).ToUniversalTime().ToString("O");
}
