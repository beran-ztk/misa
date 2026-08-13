using Resona.Models;

namespace Resona.Cloud.Server;

public static class CloudSnapshotValidator
{
    public const int CurrentSchemaVersion = 1;
    public const int MaximumTracks = 100_000;
    public const int MaximumProfileImageBytes = 1_000_000;

    public static Dictionary<string, string[]> Validate(CloudLibrarySnapshot? snapshot)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
                errors[key] = messages = [];
            messages.Add(message);
        }

        if (snapshot is null)
        {
            Add("snapshot", "Snapshot is required.");
            return Result(errors);
        }

        if (snapshot.Profile is null)
        {
            Add("profile", "Profile is required.");
            return Result(errors);
        }
        if (snapshot.Tracks is null)
        {
            Add("tracks", "Track array is required.");
            return Result(errors);
        }
        if (snapshot.SchemaVersion != CurrentSchemaVersion)
            Add("schemaVersion", $"Only schema version {CurrentSchemaVersion} is supported.");
        if (!Guid.TryParse(snapshot.Profile.UserId, out _))
            Add("profile.userId", "A valid user ID is required.");
        if (string.IsNullOrWhiteSpace(snapshot.Profile.Username) || snapshot.Profile.Username.Trim().Length > 40)
            Add("profile.username", "Username must contain between 1 and 40 characters.");
        if ((snapshot.Profile.Bio ?? string.Empty).Length > 500)
            Add("profile.bio", "Bio must contain at most 500 characters.");
        if (snapshot.Profile.ProfileImage?.Length > MaximumProfileImageBytes)
            Add("profile.profileImage", "Profile image must not exceed 1 MB.");
        if (!TryUtcTimestamp(snapshot.Profile.UpdatedAt))
            Add("profile.updatedAt", "A valid UTC timestamp is required.");
        if (!TryUtcTimestamp(snapshot.GeneratedAt))
            Add("generatedAt", "A valid UTC timestamp is required.");
        if (snapshot.TrackCount != snapshot.Tracks.Count)
            Add("trackCount", "Track count does not match the transmitted track array.");
        if (snapshot.TrackCount < 0 || snapshot.TrackCount > MaximumTracks)
            Add("trackCount", $"Track count must be between 0 and {MaximumTracks}.");

        var videoIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < snapshot.Tracks.Count; index++)
        {
            var track = snapshot.Tracks[index];
            var key = $"tracks[{index}]";
            if (track is null)
            {
                Add(key, "Track is required.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(track.SourceVideoId) || track.SourceVideoId.Length > 64)
                Add($"{key}.sourceVideoId", "A video ID with at most 64 characters is required.");
            else if (!videoIds.Add(track.SourceVideoId))
                Add($"{key}.sourceVideoId", "Video ID must be unique within the snapshot.");
            if (!AbsoluteHttpUrl(track.CanonicalUrl, 2048))
                Add($"{key}.canonicalUrl", "An absolute HTTP or HTTPS URL is required.");
            if (string.IsNullOrWhiteSpace(track.Title) || track.Title.Length > 500)
                Add($"{key}.title", "Title must contain between 1 and 500 characters.");
            if (string.IsNullOrWhiteSpace(track.OriginalTitle) || track.OriginalTitle.Length > 500)
                Add($"{key}.originalTitle", "Original title must contain between 1 and 500 characters.");
            if (track.ChannelName?.Length > 300)
                Add($"{key}.channelName", "Channel name must contain at most 300 characters.");
            if (track.ChannelUrl is not null && !AbsoluteHttpUrl(track.ChannelUrl, 2048))
                Add($"{key}.channelUrl", "Channel URL must be an absolute HTTP or HTTPS URL.");
            if (track.ThumbnailUrl is not null && !AbsoluteHttpUrl(track.ThumbnailUrl, 2048))
                Add($"{key}.thumbnailUrl", "Thumbnail URL must be an absolute HTTP or HTTPS URL.");
            if (track.DurationSeconds is < 0 or > 86_400)
                Add($"{key}.durationSeconds", "Duration must be between 0 and 86400 seconds.");
            if (track.Rating?.Length > 50 || track.LanguageCode?.Length > 20)
                Add(key, "Rating or language code exceeds its supported length.");
            if (!TryUtcTimestamp(track.UpdatedAt))
                Add($"{key}.updatedAt", "A valid UTC timestamp is required.");
            ValidateNames(track.Tags, $"{key}.tags", Add);
            ValidateNames(track.Genres, $"{key}.genres", Add);
            if (track.EmotionalCharacter is null)
                Add($"{key}.emotionalCharacter", "Emotional character object is required.");
            else if (track.EmotionalCharacter.Count > 32
                || track.EmotionalCharacter.Any(item => item.Key.Length > 100 || !double.IsFinite(item.Value)))
                Add($"{key}.emotionalCharacter", "Emotional character values are invalid.");
            if (track.Analysis is { } analysis
                && new[] { analysis.Bpm, analysis.IntegratedLoudness, analysis.LoudnessRange }
                    .Any(value => value is double number && !double.IsFinite(number)))
                Add($"{key}.analysis", "Analysis values must be finite numbers.");
        }

        return Result(errors);
    }

    private static void ValidateNames(
        IReadOnlyList<string> names,
        string key,
        Action<string, string> add)
    {
        if (names is null)
        {
            add(key, "An array is required.");
            return;
        }
        if (names.Count > 100)
            add(key, "At most 100 values are allowed.");
        if (names.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 100))
            add(key, "Values must contain between 1 and 100 characters.");
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
            add(key, "Values must be unique ignoring case.");
    }

    private static bool AbsoluteHttpUrl(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= maximumLength
        && Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https";

    private static bool TryUtcTimestamp(string? value) =>
        DateTimeOffset.TryParse(value, out var timestamp) && timestamp.Offset == TimeSpan.Zero;

    private static Dictionary<string, string[]> Result(Dictionary<string, List<string>> errors) =>
        errors.ToDictionary(item => item.Key, item => item.Value.ToArray(), StringComparer.Ordinal);
}
