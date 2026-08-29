using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Resona.Services;

public sealed record TrackFileNameMigrationResult(
    int Renamed,
    int Recovered,
    int AlreadyCanonical,
    int MissingFiles,
    string? BackupPath);

public static class TrackFileNameMigration
{
    public static TrackFileNameMigrationResult Run(string databasePath, string tracksDirectory)
    {
        databasePath = Path.GetFullPath(databasePath);
        tracksDirectory = Path.GetFullPath(tracksDirectory);
        Directory.CreateDirectory(tracksDirectory);

        var connectionString = $"Data Source={databasePath};Default Timeout=30";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using (var settings = connection.CreateCommand())
        {
            settings.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 30000;";
            settings.ExecuteNonQuery();
        }

        var candidates = LoadCandidates(connection, tracksDirectory);
        var pending = candidates.Where(candidate => !candidate.AlreadyCanonical).ToList();
        EnsureUniqueTargets(pending);
        if (pending.Count == 0)
            return new TrackFileNameMigrationResult(0, 0, candidates.Count, 0, null);

        var backupPath = CreateBackup(connection, databasePath);
        var moved = new List<TrackFileNameCandidate>();
        var renamed = 0;
        var recovered = 0;
        var missing = 0;
        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var candidate in pending)
            {
                var oldExists = File.Exists(candidate.OldPath);
                var targetExists = File.Exists(candidate.TargetPath);
                if (!oldExists && !targetExists)
                {
                    missing++;
                    continue;
                }
                if (oldExists && targetExists)
                    throw new IOException($"Both source and target exist for track {candidate.TrackId}.");

                if (oldExists)
                {
                    File.Move(candidate.OldPath, candidate.TargetPath);
                    moved.Add(candidate);
                    renamed++;
                }
                else
                {
                    recovered++;
                }

                using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = "UPDATE tracks SET file_name = $newName WHERE id = $id AND file_name = $oldName";
                update.Parameters.AddWithValue("$newName", candidate.TargetName);
                update.Parameters.AddWithValue("$id", candidate.TrackId);
                update.Parameters.AddWithValue("$oldName", candidate.OldName);
                if (update.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException($"Track {candidate.TrackId} changed during file-name migration.");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            foreach (var candidate in moved.AsEnumerable().Reverse())
            {
                if (File.Exists(candidate.TargetPath) && !File.Exists(candidate.OldPath))
                    File.Move(candidate.TargetPath, candidate.OldPath);
            }
            throw;
        }

        return new TrackFileNameMigrationResult(
            renamed,
            recovered,
            candidates.Count(candidate => candidate.AlreadyCanonical),
            missing,
            backupPath);
    }

    public static string CanonicalFileName(string videoId, string currentFileName)
    {
        videoId = videoId.Trim();
        if (videoId.Length == 0 || videoId.Any(character => !char.IsAsciiLetterOrDigit(character)
                                                           && character is not '_' and not '-'))
            throw new InvalidDataException("Track source video ID is invalid.");
        var extension = Path.GetExtension(currentFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidDataException("Track audio file has no extension.");
        return videoId + extension;
    }

    private static List<TrackFileNameCandidate> LoadCandidates(
        SqliteConnection connection,
        string tracksDirectory)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, file_name, source_video_id, canonical_url FROM tracks ORDER BY id";
        using var reader = command.ExecuteReader();
        var candidates = new List<TrackFileNameCandidate>();
        while (reader.Read())
        {
            var trackId = reader.GetInt32(0);
            var oldName = reader.GetString(1);
            var sourceVideoId = reader.IsDBNull(2) ? null : reader.GetString(2);
            var canonicalUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
            var videoId = string.IsNullOrWhiteSpace(sourceVideoId)
                ? YouTubeUrlNormalizer.ExtractVideoId(canonicalUrl ?? string.Empty)
                : sourceVideoId.Trim();
            if (string.IsNullOrWhiteSpace(videoId))
                continue;

            var targetName = CanonicalFileName(videoId, oldName);
            candidates.Add(new TrackFileNameCandidate(
                trackId,
                oldName,
                targetName,
                SafeTrackPath(tracksDirectory, oldName),
                SafeTrackPath(tracksDirectory, targetName),
                string.Equals(oldName, targetName, StringComparison.Ordinal)));
        }
        return candidates;
    }

    private static void EnsureUniqueTargets(IReadOnlyList<TrackFileNameCandidate> candidates)
    {
        var duplicate = candidates
            .GroupBy(candidate => candidate.TargetName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Multiple tracks resolve to {duplicate.Key}.");
    }

    private static string SafeTrackPath(string tracksDirectory, string fileName)
    {
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            throw new InvalidDataException("Track file name contains a path.");
        var path = Path.GetFullPath(Path.Combine(tracksDirectory, fileName));
        var prefix = tracksDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? tracksDirectory
            : tracksDirectory + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Track path escapes the tracks directory.");
        return path;
    }

    private static string CreateBackup(SqliteConnection source, string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath)
                        ?? throw new InvalidOperationException("Database directory is unavailable.");
        var backupPath = Path.Combine(
            directory,
            $"{Path.GetFileNameWithoutExtension(databasePath)}.pre-track-filenames-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        using var destination = new SqliteConnection($"Data Source={backupPath}");
        destination.Open();
        source.BackupDatabase(destination);
        return backupPath;
    }

    private sealed record TrackFileNameCandidate(
        int TrackId,
        string OldName,
        string TargetName,
        string OldPath,
        string TargetPath,
        bool AlreadyCanonical);
}
