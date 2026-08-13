using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resona.Services;

public sealed record CloudIdentity(
    int SchemaVersion,
    string UserId,
    string DeviceId,
    string DeviceKey,
    string Username,
    string Bio,
    byte[]? ProfileImage,
    string CreatedAt,
    string UpdatedAt);

public sealed class CloudIdentityStore
{
    public const int CurrentSchemaVersion = 1;
    public const int MaximumUsernameLength = 40;
    public const int MaximumBioLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly CloudIdentityStore Current = new(Values.CloudIdentityPath);

    private readonly object _gate = new();
    private readonly string _path;

    public CloudIdentityStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cloud identity path is required.", nameof(path));
        _path = Path.GetFullPath(path);
    }

    public CloudIdentity GetOrCreate()
    {
        lock (_gate)
        {
            if (File.Exists(_path))
                return ReadAndValidate();

            var now = DateTime.UtcNow.ToString("O");
            var identity = new CloudIdentity(
                CurrentSchemaVersion,
                Guid.NewGuid().ToString("D"),
                Guid.NewGuid().ToString("D"),
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                string.Empty,
                string.Empty,
                null,
                now,
                now);
            Write(identity);
            return identity;
        }
    }

    public CloudIdentity UpdateProfile(string? username, string? bio, byte[]? profileImage)
    {
        lock (_gate)
        {
            var current = File.Exists(_path) ? ReadAndValidate() : GetOrCreate();
            var normalizedUsername = (username ?? string.Empty).Trim();
            var normalizedBio = (bio ?? string.Empty).Trim();
            if (normalizedUsername.Length > MaximumUsernameLength)
                throw new ArgumentException($"Username must contain at most {MaximumUsernameLength} characters.", nameof(username));
            if (normalizedBio.Length > MaximumBioLength)
                throw new ArgumentException($"Bio must contain at most {MaximumBioLength} characters.", nameof(bio));

            var updated = current with
            {
                Username = normalizedUsername,
                Bio = normalizedBio,
                ProfileImage = profileImage is { Length: > 0 } ? profileImage.ToArray() : null,
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };
            Write(updated);
            return updated;
        }
    }

    private CloudIdentity ReadAndValidate()
    {
        var json = File.ReadAllText(_path);
        var identity = JsonSerializer.Deserialize<CloudIdentity>(json, JsonOptions)
            ?? throw new InvalidDataException("Cloud identity file is empty.");
        if (identity.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported cloud identity schema version {identity.SchemaVersion}.");
        if (!Guid.TryParse(identity.UserId, out _)
            || !Guid.TryParse(identity.DeviceId, out _)
            || string.IsNullOrWhiteSpace(identity.DeviceKey))
            throw new InvalidDataException("Cloud identity file is invalid.");
        try
        {
            if (Convert.FromBase64String(identity.DeviceKey).Length != 32)
                throw new InvalidDataException("Cloud device key has an invalid length.");
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Cloud device key is invalid.", exception);
        }
        return identity;
    }

    private void Write(CloudIdentity identity)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(identity, JsonOptions));
        File.Move(temporaryPath, _path, overwrite: true);
    }
}
