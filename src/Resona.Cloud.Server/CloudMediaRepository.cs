using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Npgsql;
using Resona.Models;

namespace Resona.Cloud.Server;

public sealed record StoredCloudMedia(
    string TrackKey,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256,
    string StoragePath,
    string UploadedAt);

public sealed class CloudMediaRepository
{
    public const long MaximumFileSizeBytes = 256L * 1024 * 1024;
    private static readonly Regex TrackKeyPattern = new("^[A-Za-z0-9_-]{1,128}$", RegexOptions.Compiled);
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _storageRoot;

    public CloudMediaRepository(NpgsqlDataSource dataSource, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _dataSource = dataSource;
        var configuredPath = configuration["MediaStoragePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = "data/media";
        _storageRoot = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath));
        Directory.CreateDirectory(_storageRoot);
    }

    public static bool IsValidTrackKey(string? trackKey) =>
        !string.IsNullOrWhiteSpace(trackKey) && TrackKeyPattern.IsMatch(trackKey);

    public async Task<CloudMediaInventory> GetInventoryAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT track_key, file_name, file_size_bytes, sha256, uploaded_at
            FROM library_media
            WHERE owner_user_id = @userId
            ORDER BY track_key;
            """);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var files = new List<CloudMediaFile>();
        while (await reader.ReadAsync(cancellationToken))
            files.Add(new CloudMediaFile(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt64(2),
                reader.GetString(3),
                reader.GetDateTime(4).ToUniversalTime().ToString("O")));
        return new CloudMediaInventory(files);
    }

    public async Task<CloudMediaFile> StoreAsync(
        Guid userId,
        string trackKey,
        string fileName,
        string contentType,
        Stream source,
        long? declaredLength,
        string? expectedSha256,
        CancellationToken cancellationToken)
    {
        if (!IsValidTrackKey(trackKey))
            throw new ArgumentException("Track key is invalid.", nameof(trackKey));
        fileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            throw new ArgumentException("File name is invalid.", nameof(fileName));
        if (declaredLength is < 0 or > MaximumFileSizeBytes)
            throw new InvalidDataException("Media file exceeds the supported size.");

        var userDirectory = Path.Combine(_storageRoot, userId.ToString("N"));
        Directory.CreateDirectory(userDirectory);
        var extension = Path.GetExtension(fileName);
        var destinationPath = Path.GetFullPath(Path.Combine(userDirectory, trackKey + extension));
        if (!destinationPath.StartsWith(userDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Resolved media path is invalid.");
        var temporaryPath = destinationPath + ".upload-" + Guid.NewGuid().ToString("N");

        long totalBytes = 0;
        string sha256;
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var destination = new FileStream(
                             temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[128 * 1024];
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    totalBytes += read;
                    if (totalBytes > MaximumFileSizeBytes)
                        throw new InvalidDataException("Media file exceeds the supported size.");
                    hash.AppendData(buffer, 0, read);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                await destination.FlushAsync(cancellationToken);
            }

            if (declaredLength is long expectedLength && totalBytes != expectedLength)
                throw new InvalidDataException("Uploaded media length does not match Content-Length.");
            sha256 = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(expectedSha256)
                && !string.Equals(expectedSha256.Trim(), sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Uploaded media checksum does not match.");

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        await using var command = _dataSource.CreateCommand("""
            INSERT INTO library_media
                (owner_user_id, track_key, file_name, content_type, file_size_bytes, sha256, storage_path, uploaded_at)
            VALUES (@userId, @trackKey, @fileName, @contentType, @fileSizeBytes, @sha256, @storagePath, now())
            ON CONFLICT (owner_user_id, track_key) DO UPDATE SET
                file_name = excluded.file_name,
                content_type = excluded.content_type,
                file_size_bytes = excluded.file_size_bytes,
                sha256 = excluded.sha256,
                storage_path = excluded.storage_path,
                uploaded_at = excluded.uploaded_at
            RETURNING uploaded_at;
            """);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("trackKey", trackKey);
        command.Parameters.AddWithValue("fileName", fileName);
        command.Parameters.AddWithValue("contentType", string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        command.Parameters.AddWithValue("fileSizeBytes", totalBytes);
        command.Parameters.AddWithValue("sha256", sha256);
        command.Parameters.AddWithValue("storagePath", destinationPath);
        var uploadedAt = ((DateTime)(await command.ExecuteScalarAsync(cancellationToken))!).ToUniversalTime().ToString("O");
        return new CloudMediaFile(trackKey, fileName, totalBytes, sha256, uploadedAt);
    }

    public async Task<StoredCloudMedia?> FindAsync(Guid userId, string trackKey, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            SELECT track_key, file_name, content_type, file_size_bytes, sha256, storage_path, uploaded_at
            FROM library_media
            WHERE owner_user_id = @userId AND track_key = @trackKey;
            """);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("trackKey", trackKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return new StoredCloudMedia(
            reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3),
            reader.GetString(4), reader.GetString(5), reader.GetDateTime(6).ToUniversalTime().ToString("O"));
    }
}
