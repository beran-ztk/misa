namespace Music.Companion;

public static class CompanionServices
{
    public static ICompanionAudioPlayer AudioPlayer { get; set; } = new EmptyAudioPlayer();
    public static ILibraryStorage LibraryStorage { get; set; } = new DefaultLibraryStorage();
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
