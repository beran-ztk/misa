using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Resona.Core;
using Resona.Models;
using Resona.Services;

namespace Resona.Tests;

public sealed class TrackVersionTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"resona-versions-{Guid.NewGuid():N}.db");
    private readonly MusicDatabase _db;

    public TrackVersionTests()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        typeof(MusicDatabase).GetMethod("CreateSchema", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [connection, transaction]);
        transaction.Commit();
        _db = new MusicDatabase(_path);
    }

    [Fact]
    public void Migration_only_adds_fields_and_preserves_existing_values()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE tracks(id INTEGER PRIMARY KEY, title TEXT, remix TEXT, rating_id INTEGER); INSERT INTO tracks VALUES(17, 'Existing', 'Old remix', 4);";
        command.ExecuteNonQuery();
        MusicDatabase.EnsureTrackVersionSchema(connection);
        MusicDatabase.EnsureTrackVersionSchema(connection);
        command.CommandText = "SELECT id, title, remix, rating_id, is_original, parent_track_id, edit_types FROM tracks";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(17, reader.GetInt32(0));
        Assert.Equal("Existing", reader.GetString(1));
        Assert.Equal("Old remix", reader.GetString(2));
        Assert.Equal(4, reader.GetInt32(3));
        Assert.Equal(1, reader.GetInt32(4));
        Assert.True(reader.IsDBNull(5));
        Assert.Equal(0, reader.GetInt32(6));
        Assert.False(reader.Read());
    }

    [Fact]
    public void Multiple_edits_round_trip_and_survive_parent_deletion()
    {
        var parent = Insert("original");
        var first = Insert("edit-one", false, parent, TrackEditTypes.Slowed | TrackEditTypes.Reverb, "My edit");
        var second = Insert("edit-two", false, parent, TrackEditTypes.Slowed);
        Assert.True(_db.GetTrackById(parent)!.IsOriginal);
        Assert.Equal(parent, _db.GetTrackById(first)!.ParentTrackId);
        Assert.Equal("Slowed · Reverb", TrackVersions.Label(_db.GetTrackById(first)!));
        Assert.Equal("My edit", _db.GetTrackById(first)!.Remix);
        Assert.Equal(3, _db.GetAllTracks().Count);

        _db.DeleteTrack(parent);

        Assert.Null(_db.GetTrackById(parent));
        var detached = _db.GetTrackById(first)!;
        Assert.False(detached.IsOriginal);
        Assert.Null(detached.ParentTrackId);
        Assert.Equal(TrackEditTypes.Slowed | TrackEditTypes.Reverb, detached.EditTypes);
        Assert.Equal("My edit", detached.Remix);
        Assert.Equal("edit-one.m4a", detached.FileName);
        Assert.Null(_db.GetTrackById(second)!.ParentTrackId);
    }

    [Fact]
    public void Assignment_rejects_cycles_and_preserves_metadata()
    {
        var parent = Insert("original");
        var edit = Insert("edit", false, parent, TrackEditTypes.Nightcore);
        Assert.Throws<InvalidOperationException>(() => _db.SetTrackVersion(parent, false, edit, TrackEditTypes.Remix));
        Assert.Throws<InvalidOperationException>(() => _db.SetTrackVersion(edit, false, edit, TrackEditTypes.Remix));
        Assert.Throws<InvalidOperationException>(() => _db.SetTrackVersion(edit, false, 9999, TrackEditTypes.Remix));
        var before = _db.GetTrackById(edit)!;
        _db.SetTrackVersion(edit, false, null, TrackEditTypes.Remix);
        var after = _db.GetTrackById(edit)!;
        Assert.Null(after.ParentTrackId);
        Assert.Equal(before.Title, after.Title);
        Assert.Equal(before.RatingId, after.RatingId);
        Assert.Equal(before.OriginalTitle, after.OriginalTitle);
        Assert.Equal(before.DownloadedAt, after.DownloadedAt);
    }

    [Fact]
    public void Filtered_group_has_context_parent_and_only_matching_children()
    {
        var original = Track(1);
        var nightcore = Track(2, false, 1, TrackEditTypes.Nightcore);
        var slowed = Track(3, false, 1, TrackEditTypes.Slowed | TrackEditTypes.Reverb);
        var orphan = Track(4, false, 999, TrackEditTypes.Nightcore);
        var all = new[] { original, nightcore, slowed, orphan };
        var matches = Filter(all, new FilterGroup(new HashSet<int>(), new HashSet<int>(), new HashSet<int>(), new HashSet<string>(), [], Versions: new HashSet<string> { "Nightcore" }));
        Assert.Equal(new[] { 2, 4 }, matches.Select(track => track.Id));
        var rows = TrackGrouping.Build(matches, all);
        Assert.Equal(new[] { 1, 2, 4 }, rows.Select(row => row.Track.Id));
        Assert.True(rows[0].IsContextOnly);
        Assert.True(rows[1].IsChild);
        Assert.False(rows[2].IsChild);
        Assert.Equal(matches.Select(track => track.Id), rows.Where(row => !row.IsContextOnly).Select(row => row.Track.Id));
        Assert.Empty(TrackGrouping.Build([], all));
    }

    [Fact]
    public void All_matching_versions_remain_playable_and_original_is_first()
    {
        var original = Track(1);
        var first = Track(2, false, 1, TrackEditTypes.Nightcore);
        var second = Track(3, false, 1, TrackEditTypes.Remix);
        var rows = TrackGrouping.Build([second, first, original], [original, first, second]);
        Assert.Equal(new[] { 1, 3, 2 }, rows.Select(row => row.Track.Id));
        Assert.All(rows, row => Assert.False(row.IsContextOnly));
    }

    [Fact]
    public void Version_filters_combine_with_metadata_and_exclusions()
    {
        var original = Track(1);
        var nightcore = Track(2, false, 1, TrackEditTypes.Nightcore) with { LanguageCode = "en" };
        var slowed = Track(3, false, 1, TrackEditTypes.Slowed | TrackEditTypes.Reverb);
        var all = new[] { original, nightcore, slowed };
        Assert.Equal(new[] { 2 }, Filter(all,
            new FilterGroup(new HashSet<int>(), new HashSet<int>(), new HashSet<int>(), new HashSet<string> { "en" }, [], Versions: new HashSet<string> { "Edit" }))
            .Select(track => track.Id));
        Assert.Equal(new[] { 1, 2 }, Filter(all,
            new FilterGroup(new HashSet<int>(), new HashSet<int>(), new HashSet<int>(), new HashSet<string>(), [], Negate: true, Versions: new HashSet<string> { "Reverb" }))
            .Select(track => track.Id));
    }

    [Fact]
    public void Portable_presets_round_trip_and_filter_versions()
    {
        var preset = new PortableFilterPreset("Nightcore", [new([], [], Versions: ["Nightcore"])]);
        var restored = JsonSerializer.Deserialize<PortableFilterPreset>(JsonSerializer.Serialize(preset))!;
        var original = new PortableTrack("Original", "original.m4a", 60, "Good", [], []);
        var edit = new PortableTrack("Edit", "edit.m4a", 60, "Good", [], [], IsOriginal: false, EditTypes: ["Nightcore"]);
        Assert.Equal(new[] { edit }, PortableTrackFilter.Apply([original, edit], null, new HashSet<string>(), restored.Groups));
    }

    [Fact]
    public void Queued_version_keeps_assignment_through_restart_download_and_analysis_handoff()
    {
        var parent = Insert("original");
        var preview = ImportQueueService.CreateVersionPreview(_db.GetTrackById(parent)!,
            TrackEditTypes.Nightcore, "https://youtu.be/abcdefghijk");
        _db.CreateImportBatch(preview.SourceUrl, [preview], rejectDuplicates: true);
        var reopened = new MusicDatabase(_path);
        var visible = Assert.Single(Assert.Single(reopened.GetImportQueueSources()).Items);
        Assert.Equal(TrackEditTypes.Nightcore, visible.EditTypes);
        Assert.Equal(parent, visible.ParentTrackId);
        var claimed = reopened.ClaimNextQueuedImport()!;
        Assert.Equal(ImportQueueStatus.Downloading, claimed.Status);
        Assert.Equal(claimed, Assert.Single(reopened.GetInterruptedImportQueueItems()));
        reopened.RequeueInterruptedImports();
        claimed = reopened.ClaimNextQueuedImport()!;
        var request = claimed.DownloadRequest;
        Assert.False(request.IsOriginal);
        Assert.Equal(parent, request.ParentTrackId);
        Assert.Equal(TrackEditTypes.Nightcore, request.EditTypes);
        var downloaded = reopened.InsertTrack(request.RawUrl, "Downloaded version", "version.m4a", [], null, [], 60, 100, 1,
            isOriginal: request.IsOriginal, parentTrackId: request.ParentTrackId, editTypes: request.EditTypes);
        reopened.CompleteImportQueueItem(claimed.Id, downloaded);
        Assert.Empty(reopened.GetImportQueueSources());
        var track = reopened.GetTrackById(downloaded)!;
        Assert.False(track.IsOriginal);
        Assert.Equal(parent, track.ParentTrackId);
        Assert.Equal(TrackEditTypes.Nightcore, track.EditTypes);
        Assert.True(TrackWorkflowPolicy.ShouldAnalyze(track.LibraryState, track.AnalysisDisabled, hasAnalysis: false));
        Assert.Null(track.RatingId);
        Assert.Equal(TrackLibraryState.PendingRating, track.LibraryState);
    }

    [Fact]
    public void Duplicate_queue_submissions_do_not_overwrite_a_running_version()
    {
        var parent = Insert("original");
        var preview = ImportQueueService.CreateVersionPreview(_db.GetTrackById(parent)!,
            TrackEditTypes.Nightcore, "https://youtu.be/abcdefghijk");
        _db.CreateImportBatch(preview.SourceUrl, [preview], rejectDuplicates: true);
        var claimed = _db.ClaimNextQueuedImport()!;
        Assert.Throws<InvalidOperationException>(() => _db.CreateImportBatch(preview.SourceUrl,
            [preview with { EditTypes = TrackEditTypes.Remix }], rejectDuplicates: true));
        // A regular Add Tracks preview can be submitted after this version was queued.
        _db.CreateImportBatch(preview.SourceUrl, [preview with { IsOriginal = true, ParentTrackId = null, EditTypes = TrackEditTypes.None }]);
        var existing = Assert.Single(Assert.Single(_db.GetImportQueueSources()).Items);
        Assert.Equal(claimed.Id, existing.Id);
        Assert.Equal(ImportQueueStatus.Downloading, existing.Status);
        Assert.False(existing.IsOriginal);
        Assert.Equal(TrackEditTypes.Nightcore, existing.EditTypes);
        Assert.Equal(parent, existing.ParentTrackId);
    }

    [Fact]
    public void Deleting_original_during_queued_download_keeps_the_edit()
    {
        var parent = Insert("original");
        var preview = ImportQueueService.CreateVersionPreview(_db.GetTrackById(parent)!,
            TrackEditTypes.Remix, "https://youtu.be/abcdefghijk");
        _db.CreateImportBatch(preview.SourceUrl, [preview], rejectDuplicates: true);
        var claimed = _db.ClaimNextQueuedImport()!;
        _db.DeleteTrack(parent);
        Assert.Null(Assert.Single(Assert.Single(_db.GetImportQueueSources()).Items).ParentTrackId);
        var request = claimed.DownloadRequest;
        var downloaded = _db.InsertTrack(request.RawUrl, "Remix", "remix.m4a", [], null, [], 60, 100, 1,
            isOriginal: request.IsOriginal, parentTrackId: request.ParentTrackId, editTypes: request.EditTypes);
        var track = _db.GetTrackById(downloaded)!;
        Assert.False(track.IsOriginal);
        Assert.Null(track.ParentTrackId);
        Assert.Equal(TrackEditTypes.Remix, track.EditTypes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a URL")]
    [InlineData("https://example.com/watch?v=abcdefghijk")]
    [InlineData("https://www.youtube.com/playlist?list=PL123456")]
    public void Version_entry_rejects_non_track_urls(string url)
    {
        Assert.Throws<InvalidOperationException>(() => ImportQueueService.CreateVersionPreview(Track(1), TrackEditTypes.Nightcore, url));
    }

    [Fact]
    public void Version_entry_requires_an_original_and_a_type()
    {
        const string url = "https://youtu.be/abcdefghijk";
        Assert.Throws<InvalidOperationException>(() => ImportQueueService.CreateVersionPreview(Track(1), TrackEditTypes.None, url));
        Assert.Throws<InvalidOperationException>(() => ImportQueueService.CreateVersionPreview(Track(1, false), TrackEditTypes.Nightcore, url));
    }

    [Fact]
    public void Queue_migration_preserves_existing_pending_downloads()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE tracks(id INTEGER PRIMARY KEY); CREATE TABLE import_queue_items(id INTEGER PRIMARY KEY, title TEXT, status TEXT); INSERT INTO import_queue_items VALUES(12, 'Waiting track', 'Queued');";
        command.ExecuteNonQuery();
        MusicDatabase.EnsureImportVersionSchema(connection);
        MusicDatabase.EnsureImportVersionSchema(connection);
        command.CommandText = "SELECT id, title, status, is_original, parent_track_id, edit_types FROM import_queue_items";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(12, reader.GetInt32(0));
        Assert.Equal("Waiting track", reader.GetString(1));
        Assert.Equal("Queued", reader.GetString(2));
        Assert.Equal(1, reader.GetInt32(3));
        Assert.True(reader.IsDBNull(4));
        Assert.Equal(0, reader.GetInt32(5));
        Assert.False(reader.Read());
    }

    private static List<MusicTrack> Filter(IEnumerable<MusicTrack> tracks, params FilterGroup[] groups) =>
        TrackFilter.Apply(tracks, new Dictionary<int, List<int>>(), new Dictionary<int, List<int>>(),
            new Dictionary<int, List<int>>(), new Dictionary<int, Dictionary<string, double>>(), new HashSet<int>(), groups, null);

    private static MusicTrack Track(int id, bool original = true, int? parent = null, TrackEditTypes types = TrackEditTypes.None) =>
        new(id, $"url-{id}", $"Track {id}", $"{id}.m4a", 1, "2026-09-02", 60, false, null, null, null, "2026-09-02",
            IsOriginal: original, ParentTrackId: parent, EditTypes: types);

    private int Insert(string name, bool original = true, int? parent = null, TrackEditTypes types = TrackEditTypes.None, string? versionName = null) =>
        _db.InsertTrack($"https://example.test/{name}", name, $"{name}.m4a", [], null, [], 60, 100, 10,
            isOriginal: original, parentTrackId: parent, editTypes: types, versionName: versionName);

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={_path}");
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }
}
