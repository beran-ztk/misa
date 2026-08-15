using Resona.Services;

namespace Resona.Tests;

public sealed class PlaybackQueueTests
{
    [Fact]
    public void Reset_preserves_order_and_selects_current_track()
    {
        var queue = new PlaybackQueue();

        queue.Reset([1, 2, 2, 3, 4], 2);

        Assert.Equal(2, queue.CurrentTrackId);
        Assert.Equal([3, 4], queue.UpcomingTrackIds);
        Assert.Equal(3, queue.PeekNext(loopPlaylist: false));
        Assert.Equal(1, queue.PeekPrevious());
    }

    [Fact]
    public void Upcoming_tracks_can_move_without_crossing_current_track()
    {
        var queue = new PlaybackQueue();
        queue.Reset([1, 2, 3, 4], 2);

        Assert.True(queue.MoveUpcoming(4, -1));
        Assert.Equal([4, 3], queue.UpcomingTrackIds);
        Assert.False(queue.MoveUpcoming(4, -1));
        Assert.False(queue.MoveUpcoming(2, 1));
    }

    [Fact]
    public void Removing_and_clearing_only_affects_upcoming_tracks()
    {
        var queue = new PlaybackQueue();
        queue.Reset([1, 2, 3, 4], 2);

        Assert.False(queue.RemoveUpcoming(1));
        Assert.False(queue.RemoveUpcoming(2));
        Assert.True(queue.RemoveUpcoming(3));
        Assert.Equal([4], queue.UpcomingTrackIds);
        Assert.Equal(1, queue.ClearUpcoming());
        Assert.Empty(queue.UpcomingTrackIds);
        Assert.Equal([1, 2], queue.TrackIds);
    }

    [Fact]
    public void Playlist_loop_wraps_but_normal_playback_stops()
    {
        var queue = new PlaybackQueue();
        queue.Reset([1, 2, 3], 3);

        Assert.Null(queue.PeekNext(loopPlaylist: false));
        Assert.Equal(1, queue.PeekNext(loopPlaylist: true));
    }

    [Fact]
    public void Reset_upcoming_restarts_after_current_without_duplicates()
    {
        var queue = new PlaybackQueue();

        queue.ResetUpcoming(3, [1, 2, 3, 4, 1]);

        Assert.Equal(3, queue.CurrentTrackId);
        Assert.Equal([1, 2, 4], queue.UpcomingTrackIds);
    }

    [Fact]
    public void Menu_actions_reorder_and_remove_without_moving_current_track()
    {
        var queue = new PlaybackQueue();
        queue.Reset([1, 2, 3, 4], 2);

        Assert.True(queue.MoveNext(4));
        Assert.Equal([1, 2, 4, 3], queue.TrackIds);
        Assert.True(queue.Move(4, 1));
        Assert.Equal([1, 2, 3, 4], queue.TrackIds);
        Assert.True(queue.Remove(3));
        Assert.Equal([1, 2, 4], queue.TrackIds);
        Assert.False(queue.Remove(2));
        Assert.False(queue.Move(2, 1));
        Assert.False(queue.Move(4, -1));
        Assert.False(queue.MoveNext(4));
    }

    [Fact]
    public void Retain_prunes_unavailable_tracks_without_restoring_removed_items()
    {
        var queue = new PlaybackQueue();
        queue.Reset([1, 2, 3, 4, 5], 2);
        queue.RemoveUpcoming(4);

        queue.Retain([2, 3, 4, 5], alwaysRetainTrackId: 2);

        Assert.Equal([2, 3, 5], queue.TrackIds);
        Assert.Equal(2, queue.CurrentTrackId);
    }

    [Fact]
    public void Removing_current_track_advances_to_existing_next_track()
    {
        var queue = new PlaybackQueue();
        queue.Reset([1, 2, 4, 3], 2);

        var nextTrackId = queue.RemoveCurrentAndAdvance(loopPlaylist: false);

        Assert.Equal(4, nextTrackId);
        Assert.Equal(4, queue.CurrentTrackId);
        Assert.Equal([1, 4, 3], queue.TrackIds);
        Assert.Equal([3], queue.UpcomingTrackIds);
    }

    [Fact]
    public void Removing_last_current_track_only_wraps_when_playlist_loops()
    {
        var normalQueue = new PlaybackQueue();
        normalQueue.Reset([1, 2, 3], 3);
        Assert.Null(normalQueue.RemoveCurrentAndAdvance(loopPlaylist: false));
        Assert.False(normalQueue.IsInitialized);

        var loopingQueue = new PlaybackQueue();
        loopingQueue.Reset([1, 2, 3], 3);
        Assert.Equal(1, loopingQueue.RemoveCurrentAndAdvance(loopPlaylist: true));
        Assert.Equal(1, loopingQueue.CurrentTrackId);
        Assert.Equal([1, 2], loopingQueue.TrackIds);
    }
}
