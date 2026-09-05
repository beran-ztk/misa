using Resona.Companion;
using Resona.Core;
using Resona.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Resona.Cloud.Tests;

public sealed class DeviceLibraryCloudClientTests
{
    [Theory]
    [InlineData("2026-09-05T12:00:01Z", "2026-09-05T12:00:00Z", true)]
    [InlineData("2026-09-05T12:00:00Z", "2026-09-05T12:00:00Z", false)]
    [InlineData("2026-09-05T11:59:59Z", "2026-09-05T12:00:00Z", false)]
    [InlineData("2026-09-05T14:00:00+02:00", "2026-09-05T12:00:00Z", false)]
    [InlineData("2026-09-05T12:00:00Z", "invalid", true)]
    public void Snapshot_is_replaced_only_when_candidate_timestamp_is_newer(
        string candidate,
        string current,
        bool expected)
    {
        Assert.Equal(expected, DeviceLibraryCloudClient.IsNewer(candidate, current));
    }

    [Fact]
    public async Task Newer_revision_replaces_cache_even_when_timestamp_is_equal()
    {
        var previousStorage = CompanionServices.LibraryStorage;
        var root = Path.Combine(Path.GetTempPath(), $"resona-companion-revision-{Guid.NewGuid():N}");
        try
        {
            CompanionServices.LibraryStorage = new TestLibraryStorage(root);
            var userId = Guid.NewGuid().ToString("D");
            var cached = Snapshot(userId, "2026-09-05T12:00:00Z", "Old") with { LibraryRevision = 4 };
            var remote = Snapshot(userId, "2026-09-05T12:00:00Z", "New") with { LibraryRevision = 5 };
            var client = new DeviceLibraryCloudClient(new HttpClient(new SnapshotHandler(remote)));
            client.SaveConnection(new CompanionCloudConnection("https://api.resona.home.arpa"));
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(client.SnapshotPath,
                JsonSerializer.Serialize(cached, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            var result = await client.RefreshMetadataAsync();
            var library = await PortableLibraryStore.LoadAsync(root);

            Assert.True(result.LibraryUpdated);
            Assert.Equal("New", Assert.Single(library.Library.FilterPresets!).Name);
        }
        finally
        {
            CompanionServices.LibraryStorage = previousStorage;
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Equal_timestamp_keeps_existing_local_library()
    {
        var previousStorage = CompanionServices.LibraryStorage;
        var root = Path.Combine(Path.GetTempPath(), $"resona-companion-test-{Guid.NewGuid():N}");
        try
        {
            CompanionServices.LibraryStorage = new TestLibraryStorage(root);
            var userId = Guid.NewGuid().ToString("D");
            var timestamp = "2026-09-05T12:00:00Z";
            var cached = Snapshot(userId, timestamp, "Local preset");
            var remote = Snapshot(userId, timestamp, "Remote preset");
            var handler = new SnapshotHandler(remote);
            var client = new DeviceLibraryCloudClient(new HttpClient(handler));
            client.SaveConnection(new CompanionCloudConnection("https://api.resona.home.arpa"));
            await File.WriteAllTextAsync(
                client.SnapshotPath,
                JsonSerializer.Serialize(cached, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            await PortableLibraryStore.SaveAsync(
                root,
                new PortableMusicLibrary([], [new PortableFilterPreset("Local preset", [])]));

            var result = await client.RefreshMetadataAsync();
            var loaded = await PortableLibraryStore.LoadAsync(root);

            Assert.False(result.LibraryUpdated);
            Assert.Equal("Local preset", Assert.Single(loaded.Library.FilterPresets!).Name);
        }
        finally
        {
            CompanionServices.LibraryStorage = previousStorage;
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static CloudDeviceLibrarySnapshot Snapshot(string userId, string generatedAt, string presetName) => new(
        1,
        userId,
        0,
        generatedAt,
        [],
        [new PortableFilterPreset(presetName, [])],
        [],
        []);

    private sealed class TestLibraryStorage(string directory) : ILibraryStorage
    {
        public string LibraryDirectory { get; } = directory;
    }

    private sealed class SnapshotHandler(CloudDeviceLibrarySnapshot snapshot) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                Encoding.UTF8,
                "application/json")
        });
    }
}
