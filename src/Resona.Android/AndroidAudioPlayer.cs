using Android.Media;
using Android.OS;
using Resona.Companion;

namespace Resona.Android;

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

        var player = new MediaPlayer();
        try
        {
            // Passing the raw path string makes MediaPlayer interpret some
            // leading Unicode and URI-reserved characters as a remote source.
            // A file descriptor addresses the already resolved local file and
            // is independent of its display name.
            using var descriptor = ParcelFileDescriptor.Open(
                new Java.IO.File(filePath),
                ParcelFileMode.ReadOnly)
                ?? throw new IOException($"Could not open audio file '{filePath}'.");
            player.SetDataSource(descriptor.FileDescriptor);
            player.Completion += OnPlaybackCompleted;
            player.Prepare();
            player.Start();
            _player = player;
        }
        catch
        {
            player.Completion -= OnPlaybackCompleted;
            player.Release();
            player.Dispose();
            throw;
        }

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
