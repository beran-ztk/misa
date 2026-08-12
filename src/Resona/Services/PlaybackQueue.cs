using System;
using System.Collections.Generic;
using System.Linq;

namespace Resona.Services;

/// <summary>
/// Owns the active playback sequence independently from the currently visible
/// library filter. Items before the current track form playback history; only
/// upcoming items can be reordered or removed.
/// </summary>
public sealed class PlaybackQueue
{
    private readonly List<int> _trackIds = [];
    private int _currentIndex = -1;

    public bool IsInitialized => _currentIndex >= 0 && _currentIndex < _trackIds.Count;
    public int? CurrentTrackId => IsInitialized ? _trackIds[_currentIndex] : null;
    public IReadOnlyList<int> TrackIds => _trackIds;
    public IReadOnlyList<int> UpcomingTrackIds => IsInitialized
        ? _trackIds.Skip(_currentIndex + 1).ToList()
        : [];

    public void Reset(IEnumerable<int> trackIds, int currentTrackId)
    {
        _trackIds.Clear();
        _trackIds.AddRange(Distinct(trackIds));
        _currentIndex = _trackIds.IndexOf(currentTrackId);
        if (_currentIndex >= 0)
            return;

        _trackIds.Insert(0, currentTrackId);
        _currentIndex = 0;
    }

    public void ResetUpcoming(int currentTrackId, IEnumerable<int> upcomingTrackIds)
    {
        _trackIds.Clear();
        _trackIds.Add(currentTrackId);
        _trackIds.AddRange(Distinct(upcomingTrackIds).Where(trackId => trackId != currentTrackId));
        _currentIndex = 0;
    }

    public bool SetCurrent(int trackId)
    {
        var index = _trackIds.IndexOf(trackId);
        if (index < 0)
            return false;

        _currentIndex = index;
        return true;
    }

    public int? PeekNext(bool loopPlaylist)
    {
        if (!IsInitialized || _trackIds.Count == 0)
            return null;
        if (_currentIndex + 1 < _trackIds.Count)
            return _trackIds[_currentIndex + 1];
        return loopPlaylist ? _trackIds[0] : null;
    }

    public int? PeekPrevious()
    {
        if (!IsInitialized || _currentIndex <= 0)
            return null;
        return _trackIds[_currentIndex - 1];
    }

    public bool MoveUpcoming(int trackId, int offset)
    {
        if (!IsInitialized || offset == 0)
            return false;

        var index = _trackIds.IndexOf(trackId);
        if (index <= _currentIndex)
            return false;

        var target = Math.Clamp(index + offset, _currentIndex + 1, _trackIds.Count - 1);
        if (target == index)
            return false;

        _trackIds.RemoveAt(index);
        _trackIds.Insert(target, trackId);
        return true;
    }

    public bool Move(int trackId, int offset)
    {
        if (!IsInitialized || offset == 0 || trackId == CurrentTrackId)
            return false;

        var index = _trackIds.IndexOf(trackId);
        if (index < 0)
            return false;
        var target = Math.Clamp(index + offset, 0, _trackIds.Count - 1);
        if (target == index || target == _currentIndex)
            return false;

        var currentTrackId = CurrentTrackId!.Value;
        _trackIds.RemoveAt(index);
        _trackIds.Insert(target, trackId);
        _currentIndex = _trackIds.IndexOf(currentTrackId);
        return true;
    }

    public bool MoveNext(int trackId)
    {
        if (!IsInitialized || trackId == CurrentTrackId)
            return false;

        var index = _trackIds.IndexOf(trackId);
        if (index < 0 || index == _currentIndex + 1)
            return false;

        var currentTrackId = CurrentTrackId!.Value;
        _trackIds.RemoveAt(index);
        _currentIndex = _trackIds.IndexOf(currentTrackId);
        _trackIds.Insert(_currentIndex + 1, trackId);
        return true;
    }

    public bool Remove(int trackId)
    {
        if (!IsInitialized || trackId == CurrentTrackId)
            return false;
        return _trackIds.Remove(trackId);
    }

    public bool RemoveUpcoming(int trackId)
    {
        var index = _trackIds.IndexOf(trackId);
        if (!IsInitialized || index <= _currentIndex)
            return false;

        _trackIds.RemoveAt(index);
        return true;
    }

    public int ClearUpcoming()
    {
        if (!IsInitialized)
            return 0;

        var removed = _trackIds.Count - _currentIndex - 1;
        if (removed > 0)
            _trackIds.RemoveRange(_currentIndex + 1, removed);
        return removed;
    }

    public void Clear()
    {
        _trackIds.Clear();
        _currentIndex = -1;
    }

    public void Retain(IEnumerable<int> availableTrackIds, int? alwaysRetainTrackId = null)
    {
        if (!IsInitialized)
            return;

        var currentTrackId = CurrentTrackId;
        var available = availableTrackIds.ToHashSet();
        if (alwaysRetainTrackId is int retained)
            available.Add(retained);

        _trackIds.RemoveAll(trackId => !available.Contains(trackId));
        _currentIndex = currentTrackId is int current ? _trackIds.IndexOf(current) : -1;
    }

    private static IEnumerable<int> Distinct(IEnumerable<int> trackIds)
    {
        var seen = new HashSet<int>();
        foreach (var trackId in trackIds)
            if (seen.Add(trackId))
                yield return trackId;
    }
}
