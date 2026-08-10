namespace Resona.Companion;

public static class CompanionServices
{
    public static ICompanionAudioPlayer AudioPlayer { get; set; } = new EmptyAudioPlayer();
    public static ILibraryStorage LibraryStorage { get; set; } = new DefaultLibraryStorage();
    public static IMediaControls MediaControls { get; set; } = new EmptyMediaControls();
}

public interface ILibraryStorage
{
    string LibraryDirectory { get; }
}

public interface ICompanionAudioPlayer
{
    event Action? PlaybackEnded;

    bool IsPlaying { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }

    Task PlayAsync(string filePath);
    void Pause();
    void Resume();
    void Seek(TimeSpan position);
    void Stop();
}

public enum MediaControlCommand
{
    Previous,
    PlayPause,
    Next
}

public interface IMediaControls
{
    event Action<MediaControlCommand>? CommandRequested;
    event Action<TimeSpan>? SeekRequested;

    void Update(string title, string? coverPath, bool isPlaying, TimeSpan position, TimeSpan duration);
    void Stop();
}

public sealed class DefaultLibraryStorage : ILibraryStorage
{
    public string LibraryDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MusicLibrary");
}

internal sealed class EmptyAudioPlayer : ICompanionAudioPlayer
{
    public event Action? PlaybackEnded;

    public bool IsPlaying => false;
    public TimeSpan Position => TimeSpan.Zero;
    public TimeSpan Duration => TimeSpan.Zero;

    public Task PlayAsync(string filePath)
    {
        PlaybackEnded?.Invoke();
        return Task.CompletedTask;
    }

    public void Pause() { }
    public void Resume() { }
    public void Seek(TimeSpan position) { }
    public void Stop() { }
}

internal sealed class EmptyMediaControls : IMediaControls
{
    public event Action<MediaControlCommand>? CommandRequested;
    public event Action<TimeSpan>? SeekRequested;

    public void Update(string title, string? coverPath, bool isPlaying, TimeSpan position, TimeSpan duration)
    {
        _ = CommandRequested;
        _ = SeekRequested;
    }

    public void Stop() { }
}
