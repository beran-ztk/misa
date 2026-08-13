using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Resona.Models;
using Resona.Services;

namespace Resona.Tests;

public sealed class CloudLibrarySnapshotTests
{
    private static readonly CloudIdentity Identity = new(
        1,
        "5d8e1600-4822-4ef8-9985-3f61ca7c9370",
        "417f56c9-b52c-4570-a960-217fc11ee51f",
        Convert.ToBase64String(new byte[32]),
        "Listener",
        "Nightcore and trance",
        [1, 2, 3],
        "2026-08-13T00:00:00Z",
        "2026-08-13T00:00:00Z");

    [Fact]
    public void Snapshot_contains_only_public_tracks_and_derives_missing_video_id()
    {
        var publicTrack = Track(1, "https://www.youtube.com/watch?v=abcdefghijk", isPublic: true) with
        {
            RatingId = 5,
            LanguageCode = "en",
            SourceVideoId = null,
            SourceThumbnailUrl = "https://img.test/cover.jpg"
        };
        var privateTrack = Track(2, "https://youtu.be/12345678901", isPublic: false);

        var snapshot = CloudLibrarySnapshotBuilder.Build(
            Identity,
            [publicTrack, privateTrack],
            new Dictionary<int, string> { [5] = "Amazing" },
            new Dictionary<int, string> { [10] = "Shared", [11] = "Nightcore" },
            new Dictionary<int, string> { [20] = "Trance" },
            new Dictionary<int, List<int>> { [1] = [11, 10] },
            new Dictionary<int, List<int>> { [1] = [20] },
            new Dictionary<int, TrackAudioAnalysis> { [1] = new(150, -7.2, 5.4) },
            new Dictionary<int, Dictionary<string, double>>
            {
                [1] = new() { ["aggressive, fiery, tense/anxious, intense, volatile, visceral"] = 0.82 }
            });

        Assert.Equal(1, snapshot.TrackCount);
        var track = Assert.Single(snapshot.Tracks);
        Assert.Equal("abcdefghijk", track.SourceVideoId);
        Assert.Equal(["Nightcore", "Shared"], track.Tags);
        Assert.Equal(["Trance"], track.Genres);
        Assert.Equal("Amazing", track.Rating);
        Assert.Equal(150, track.Analysis?.Bpm);
        Assert.Equal(0.82, track.EmotionalCharacter["Intense"]);
    }

    [Fact]
    public void Snapshot_rejects_duplicate_video_ids_without_dropping_tracks()
    {
        var first = Track(1, "https://youtu.be/abcdefghijk", true);
        var second = Track(2, "https://www.youtube.com/watch?v=abcdefghijk", true);

        Assert.Throws<InvalidOperationException>(() => CloudLibrarySnapshotBuilder.Build(
            Identity,
            [first, second],
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, List<int>>(),
            new Dictionary<int, List<int>>(),
            new Dictionary<int, TrackAudioAnalysis>(),
            new Dictionary<int, Dictionary<string, double>>()));
    }

    [Fact]
    public async Task Upload_uses_one_authenticated_put_for_the_complete_snapshot()
    {
        var snapshot = CloudLibrarySnapshotBuilder.Build(
            Identity,
            [Track(1, "https://youtu.be/abcdefghijk", true)],
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            new Dictionary<int, List<int>>(),
            new Dictionary<int, List<int>>(),
            new Dictionary<int, TrackAudioAnalysis>(),
            new Dictionary<int, Dictionary<string, double>>());
        var handler = new CapturingHandler();
        var service = new CloudLibrarySyncService(new HttpClient(handler));

        await service.UploadSnapshotAsync(new Uri("https://cloud.test/base/"), Identity, snapshot);

        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal("https://cloud.test/base/api/v1/library-snapshot", handler.Uri?.AbsoluteUri);
        Assert.Equal("Device", handler.AuthorizationScheme);
        Assert.Equal(Identity.DeviceKey, handler.AuthorizationParameter);
        Assert.Contains("\"trackCount\":1", handler.Body);
        Assert.Contains("\"sourceVideoId\":\"abcdefghijk\"", handler.Body);
    }

    private static MusicTrack Track(int id, string url, bool isPublic) => new(
        id, url, $"Title {id}", $"{id}.m4a", null, "2026-08-13T00:00:00Z", 180,
        false, "Channel", "https://youtube.com/channel/test", "2026-01-01", "2026-08-13T00:00:00Z",
        IsPublic: isPublic,
        OriginalTitle: $"Original title {id}");

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
