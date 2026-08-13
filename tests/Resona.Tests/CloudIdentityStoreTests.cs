using Resona.Services;

namespace Resona.Tests;

public sealed class CloudIdentityStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"resona-cloud-identity-{Guid.NewGuid():N}");
    private readonly string _path;

    public CloudIdentityStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "cloud-identity.json");
    }

    [Fact]
    public void Identity_is_created_once_and_remains_stable()
    {
        var store = new CloudIdentityStore(_path);

        var first = store.GetOrCreate();
        var second = new CloudIdentityStore(_path).GetOrCreate();

        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(first.DeviceId, second.DeviceId);
        Assert.Equal(first.DeviceKey, second.DeviceKey);
        Assert.True(Guid.TryParse(first.UserId, out _));
        Assert.True(Guid.TryParse(first.DeviceId, out _));
        Assert.Equal(32, Convert.FromBase64String(first.DeviceKey).Length);
    }

    [Fact]
    public void Profile_update_preserves_credentials_and_persists_profile_fields()
    {
        var store = new CloudIdentityStore(_path);
        var identity = store.GetOrCreate();
        byte[] image = [1, 2, 3, 4];

        var updated = store.UpdateProfile("  Listener  ", "  Nightcore and trance  ", image);
        image[0] = 99;
        var reloaded = new CloudIdentityStore(_path).GetOrCreate();

        Assert.Equal(identity.UserId, updated.UserId);
        Assert.Equal(identity.DeviceId, updated.DeviceId);
        Assert.Equal(identity.DeviceKey, updated.DeviceKey);
        Assert.Equal("Listener", reloaded.Username);
        Assert.Equal("Nightcore and trance", reloaded.Bio);
        Assert.Equal([1, 2, 3, 4], reloaded.ProfileImage);
    }

    [Fact]
    public void Profile_limits_are_enforced_without_overwriting_existing_profile()
    {
        var store = new CloudIdentityStore(_path);
        store.UpdateProfile("Listener", "Bio", null);

        Assert.Throws<ArgumentException>(() => store.UpdateProfile(
            new string('x', CloudIdentityStore.MaximumUsernameLength + 1), "Bio", null));
        Assert.Throws<ArgumentException>(() => store.UpdateProfile(
            "Listener", new string('x', CloudIdentityStore.MaximumBioLength + 1), null));

        var reloaded = store.GetOrCreate();
        Assert.Equal("Listener", reloaded.Username);
        Assert.Equal("Bio", reloaded.Bio);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
