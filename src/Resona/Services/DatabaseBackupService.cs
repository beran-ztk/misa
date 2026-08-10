using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Resona.Services;

public sealed class DatabaseBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static readonly DatabaseBackupService Current = new();

    private DatabaseBackupService() { }

    public IReadOnlyList<string> GetBackupDirectories() => LoadSettings().BackupDirectories;

    public void AddBackupDirectory(string path)
    {
        var normalizedPath = NormalizeDirectoryPath(path);
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return;

        var settings = LoadSettings();
        if (settings.BackupDirectories.Any(existing =>
                string.Equals(existing, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            return;

        settings.BackupDirectories.Add(normalizedPath);
        SaveSettings(settings);
    }

    public void RemoveBackupDirectory(string path)
    {
        var settings = LoadSettings();
        settings.BackupDirectories = settings.BackupDirectories
            .Where(existing => !string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        SaveSettings(settings);
    }

    public DatabaseBackupResult EnsureTodayBackups()
    {
        var settings = LoadSettings();
        var created = new List<string>();
        var skipped = new List<string>();
        var errors = new List<string>();

        if (!File.Exists(Values.DbPath))
            return new DatabaseBackupResult(created, skipped, ["Database file does not exist yet."]);

        var backupFileName = $"music-{DateTime.Today:yyyy-MM-dd}.db";
        foreach (var directory in settings.BackupDirectories)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var destinationPath = Path.Combine(directory, backupFileName);
                if (File.Exists(destinationPath))
                {
                    skipped.Add(destinationPath);
                    continue;
                }

                File.Copy(Values.DbPath, destinationPath);
                created.Add(destinationPath);
            }
            catch (Exception exception)
            {
                errors.Add($"{directory}: {exception.Message}");
            }
        }

        return new DatabaseBackupResult(created, skipped, errors);
    }

    private static BackupSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(Values.BackupSettingsPath))
                return new BackupSettings();

            var json = File.ReadAllText(Values.BackupSettingsPath);
            var settings = JsonSerializer.Deserialize<BackupSettings>(json, JsonOptions) ?? new BackupSettings();
            settings.BackupDirectories = settings.BackupDirectories
                .Select(NormalizeDirectoryPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return settings;
        }
        catch
        {
            return new BackupSettings();
        }
    }

    private static void SaveSettings(BackupSettings settings)
    {
        Directory.CreateDirectory(Values.LocalDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(Values.BackupSettingsPath, json);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var trimmed = path.Trim();
        return trimmed.Length == 0 ? string.Empty : Path.GetFullPath(trimmed);
    }

    private sealed class BackupSettings
    {
        public List<string> BackupDirectories { get; set; } = [];
    }
}

public sealed record DatabaseBackupResult(
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Errors);
