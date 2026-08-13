using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Resona.Cloud.Server;
using Resona.Models;
using Xunit;

namespace Resona.Cloud.Tests;

public sealed class CloudServerContractTests
{
    [Fact]
    public void Valid_snapshot_is_accepted()
    {
        var errors = CloudSnapshotValidator.Validate(Snapshot());

        Assert.Empty(errors);
    }

    [Fact]
    public void Mismatched_count_and_duplicate_video_ids_are_rejected()
    {
        var first = Track("abcdefghijk");
        var snapshot = Snapshot() with { TrackCount = 1, Tracks = [first, first] };

        var errors = CloudSnapshotValidator.Validate(snapshot);

        Assert.Contains("trackCount", errors.Keys);
        Assert.Contains("tracks[1].sourceVideoId", errors.Keys);
    }

    [Fact]
    public void Device_credentials_are_parsed_and_key_is_hashed()
    {
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var key = RandomNumberGenerator.GetBytes(32);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Resona-User-Id"] = userId.ToString("D");
        context.Request.Headers["X-Resona-Device-Id"] = deviceId.ToString("D");
        context.Request.Headers.Authorization = $"Device {Convert.ToBase64String(key)}";

        var success = DeviceCredentialsReader.TryRead(
            context.Request.Headers, out var credentials, out var error);

        Assert.True(success, error);
        Assert.Equal(userId, credentials.UserId);
        Assert.Equal(deviceId, credentials.DeviceId);
        Assert.True(CryptographicOperations.FixedTimeEquals(SHA256.HashData(key), credentials.DeviceKeyHash));
    }

    [Fact]
    public void Device_credentials_reject_short_or_malformed_keys()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Resona-User-Id"] = Guid.NewGuid().ToString("D");
        context.Request.Headers["X-Resona-Device-Id"] = Guid.NewGuid().ToString("D");
        context.Request.Headers.Authorization = "Device not-base64";

        Assert.False(DeviceCredentialsReader.TryRead(
            context.Request.Headers, out _, out _));
    }

    private static CloudLibrarySnapshot Snapshot() => new(
        1,
        new CloudPublicProfile(
            Guid.NewGuid().ToString("D"),
            "Listener",
            "Bio",
            null,
            "2026-08-13T00:00:00Z"),
        1,
        "2026-08-13T00:00:00Z",
        [Track("abcdefghijk")]);

    private static CloudPublicTrack Track(string videoId) => new(
        videoId,
        $"https://www.youtube.com/watch?v={videoId}",
        "Edited title",
        "Original title",
        "Channel",
        "https://www.youtube.com/channel/example",
        180,
        "2026-01-01T00:00:00Z",
        "https://i.ytimg.com/vi/example/hqdefault.jpg",
        "Amazing",
        "en",
        ["Shared"],
        ["Trance"],
        new CloudPublicTrackAnalysis(150, -7.2, 5.4),
        new Dictionary<string, double> { ["Intense"] = 0.82 },
        "2026-08-13T00:00:00Z");
}
