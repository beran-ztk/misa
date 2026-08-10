using Android.Content;
using Resona.Companion;

namespace Resona.Android;

public sealed class AndroidMediaControls : IMediaControls
{
    private readonly Context _context;

    public event Action<MediaControlCommand>? CommandRequested;
    public event Action<TimeSpan>? SeekRequested;

    public AndroidMediaControls(Context context)
    {
        _context = context.ApplicationContext!;
        MusicPlaybackService.CommandReceived += OnCommandReceived;
        MusicPlaybackService.SeekReceived += OnSeekReceived;
    }

    public void Update(string title, string? coverPath, bool isPlaying, TimeSpan position, TimeSpan duration)
    {
        MusicPlaybackService.StartOrUpdate(_context, title, coverPath, isPlaying, position, duration);
    }

    public void Stop()
    {
        MusicPlaybackService.StopNotification(_context);
    }

    private void OnCommandReceived(MediaControlCommand command)
    {
        CommandRequested?.Invoke(command);
    }

    private void OnSeekReceived(TimeSpan position)
    {
        SeekRequested?.Invoke(position);
    }
}
