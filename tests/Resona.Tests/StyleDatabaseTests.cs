using Microsoft.Data.Sqlite;
using Resona.Services;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Resona.Tests;

public sealed class StyleDatabaseTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"resona-styles-{Guid.NewGuid():N}");
    private readonly string _databasePath;
    private readonly MusicDatabase _database;

    public StyleDatabaseTests()
    {
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "styles.db");
        Execute(@"
            CREATE TABLE style_definitions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL COLLATE NOCASE UNIQUE
            );
            CREATE TABLE tracks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                edits TEXT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE TABLE track_styles (
                track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
                style_id INTEGER NOT NULL REFERENCES style_definitions(id) ON DELETE CASCADE,
                PRIMARY KEY (track_id, style_id)
            );");
        _database = new MusicDatabase(_databasePath);
    }

    [Fact]
    public void Styles_are_stored_independently_from_legacy_edits()
    {
        _database.AddStyle("Sped Up");
        _database.AddStyle("Reverb");
        var trackId = InsertTrack("Speed Up, Reverb, Custom");
        var styles = _database.GetStyles();
        var spedUp = styles.Single(style => style.Name == "Sped Up");
        var reverb = styles.Single(style => style.Name == "Reverb");
        AssignStyles(trackId, spedUp.Id, reverb.Id);

        Assert.Equal(new[] { reverb.Id, spedUp.Id }.Order(), _database.GetTrackStyleIds(trackId).Order());

        _database.RenameStyle(reverb.Id, "Echo");

        Assert.Equal("Speed Up, Reverb, Custom", Scalar<string>("SELECT edits FROM tracks WHERE id = $id", ("$id", trackId)));
        Assert.Equal("Echo", _database.GetStyles().Single(style => style.Id == reverb.Id).Name);
        Assert.Equal(new[] { reverb.Id, spedUp.Id }.Order(), _database.GetTrackStyleIds(trackId).Order());
    }

    [Fact]
    public void Used_styles_cannot_be_deleted_but_unused_styles_can()
    {
        _database.AddStyle("Nightcore");
        _database.AddStyle("Slowed");
        var trackId = InsertTrack("Nightcore");
        var styles = _database.GetStyles();
        AssignStyles(trackId, styles.Single(style => style.Name == "Nightcore").Id);

        Assert.Equal("Cannot delete: used by 1 track(s).",
            _database.DeleteStyleIfUnused(styles.Single(style => style.Name == "Nightcore").Id));
        Assert.Null(_database.DeleteStyleIfUnused(styles.Single(style => style.Name == "Slowed").Id));
        Assert.DoesNotContain(_database.GetStyles(), style => style.Name == "Slowed");
    }

    private int InsertTrack(string edits)
    {
        Execute("INSERT INTO tracks (edits, updated_at) VALUES ($edits, $now)",
            ("$edits", edits), ("$now", DateTime.UtcNow.ToString("O")));
        return Scalar<int>("SELECT MAX(id) FROM tracks");
    }

    private void AssignStyles(int trackId, params int[] styleIds)
    {
        foreach (var styleId in styleIds)
            Execute("INSERT INTO track_styles (track_id, style_id) VALUES ($trackId, $styleId)",
                ("$trackId", trackId), ("$styleId", styleId));
    }

    private void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        command.ExecuteNonQuery();
    }

    private T Scalar<T>(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch { }
    }
}
