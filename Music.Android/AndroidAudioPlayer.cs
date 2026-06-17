using Android.Media;
using Music.Companion;

namespace Music.Android;

public sealed class AndroidAudioPlayer : ICompanionAudioPlayer, IDisposable
{
    private MediaPlayer? _player;

    public event Action? PlaybackEnded;

    public bool IsPlaying => _player?.IsPlaying == true;
    public TimeSpan Position => TimeSpan.FromMilliseconds(_player?.CurrentPosition ?? 0);
    public TimeSpan Duration => TimeSpan.FromMilliseconds(_player?.Duration ?? 0);

    public Task PlayAsync(string filePath)
    {
        Stop();

        _player = new MediaPlayer();
        _player.SetDataSource(filePath);
        _player.Completion += OnPlaybackCompleted;
        _player.Prepare();
        _player.Start();

        return Task.CompletedTask;
    }

    public void Pause()
    {
        if (_player?.IsPlaying == true)
            _player.Pause();
    }

    public void Resume()
    {
        if (_player is { IsPlaying: false })
            _player.Start();
    }

    public void Seek(TimeSpan position)
    {
        _player?.SeekTo((int)position.TotalMilliseconds);
    }

    public void Stop()
    {
        if (_player == null)
            return;

        _player.Completion -= OnPlaybackCompleted;
        try { _player.Stop(); }
        catch { }
        _player.Release();
        _player.Dispose();
        _player = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        PlaybackEnded?.Invoke();
    }
}
