using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

/// <summary>
/// Owns Channel Hub cache refreshes and slow remote enrichment. Public methods
/// only enqueue work; database, yt-dlp and image downloads never run on the UI
/// thread.
/// </summary>
public sealed class ChannelHubBackgroundService
{
    public static ChannelHubBackgroundService Current { get; } = new();

    private readonly object _gate = new();
    private readonly SemaphoreSlim _remoteGate = new(1, 1);
    private readonly Queue<ChannelHubItem> _enrichmentQueue = new();
    private readonly HashSet<int> _queuedEnrichments = [];
    private readonly HashSet<int> _attemptedEnrichments = [];
    private Task? _cacheWorker;
    private Task? _enrichmentWorker;
    private Task? _followedRefreshWorker;
    private int _refreshRequested;
    private bool _initialized;
    private bool _startupFollowedRefreshQueued;
    private IReadOnlyList<ChannelHubItem> _snapshot = Array.Empty<ChannelHubItem>();

    public event Action<IReadOnlyList<ChannelHubItem>>? SnapshotChanged;

    public IReadOnlyList<ChannelHubItem> Snapshot
    {
        get
        {
            lock (_gate)
                return _snapshot;
        }
    }

    private ChannelHubBackgroundService() { }

    public void Initialize()
    {
        lock (_gate)
        {
            if (_initialized)
                return;
            _initialized = true;
        }

        RequestRefresh();
    }

    public void RequestRefresh()
    {
        Interlocked.Exchange(ref _refreshRequested, 1);
        lock (_gate)
        {
            if (_cacheWorker is { IsCompleted: false })
                return;
            _cacheWorker = Task.Run(RefreshCacheLoop);
        }
    }

    public void RequestEnrichment(ChannelHubItem channel)
    {
        if (string.IsNullOrWhiteSpace(channel.SourceUrl))
            return;

        lock (_gate)
        {
            if (_attemptedEnrichments.Contains(channel.Id) || !_queuedEnrichments.Add(channel.Id))
                return;
            _enrichmentQueue.Enqueue(channel);
            if (_enrichmentWorker is { IsCompleted: false })
                return;
            _enrichmentWorker = Task.Run(ProcessEnrichmentQueueAsync);
        }
    }

    public void RequestFollowedChannelRefresh()
    {
        lock (_gate)
        {
            if (_followedRefreshWorker is { IsCompleted: false })
                return;
            _followedRefreshWorker = Task.Run(RefreshFollowedChannelsAsync);
        }
    }

    private void RefreshCacheLoop()
    {
        while (Interlocked.Exchange(ref _refreshRequested, 0) != 0)
        {
            try
            {
                var snapshot = MusicLibraryService.Current.GetChannelHubItems();
                lock (_gate)
                    _snapshot = snapshot;
                SnapshotChanged?.Invoke(snapshot);
                QueueMissingChannelData(snapshot);
                lock (_gate)
                {
                    if (!_startupFollowedRefreshQueued)
                    {
                        _startupFollowedRefreshQueued = true;
                        RequestFollowedChannelRefresh();
                    }
                }
            }
            catch
            {
                // A later request retries. Channel Hub cache failures must never
                // terminate the application or block its UI.
            }
        }
    }

    private void QueueMissingChannelData(IReadOnlyList<ChannelHubItem> channels)
    {
        foreach (var channel in channels.Where(NeedsRemoteEnrichment))
            RequestEnrichment(channel);
    }

    private static bool NeedsRemoteEnrichment(ChannelHubItem channel) =>
        channel.Thumbnail is not { Length: > 0 }
        || channel.FollowerCount is null
        || channel.SourceChannelId is null
        || channel.LastCheckedAt is null;

    private async Task ProcessEnrichmentQueueAsync()
    {
        while (true)
        {
            ChannelHubItem channel;
            lock (_gate)
            {
                if (_enrichmentQueue.Count == 0)
                    return;
                channel = _enrichmentQueue.Dequeue();
                _queuedEnrichments.Remove(channel.Id);
                _attemptedEnrichments.Add(channel.Id);
            }

            try
            {
                await _remoteGate.WaitAsync();
                try
                {
                    await MusicLibraryService.Current.AddOrRefreshChannelAsync(channel.SourceUrl);
                }
                finally
                {
                    _remoteGate.Release();
                }
                RequestRefresh();
            }
            catch
            {
                // Remote enrichment is best-effort and retried next app session.
            }
        }
    }

    private async Task RefreshFollowedChannelsAsync()
    {
        try
        {
            var followed = MusicLibraryService.Current.GetChannelSubscriptions();
            foreach (var channel in followed)
            {
                lock (_gate)
                {
                    if (_queuedEnrichments.Contains(channel.Id) || _attemptedEnrichments.Contains(channel.Id))
                        continue;
                }
                await _remoteGate.WaitAsync();
                try
                {
                    await MusicLibraryService.Current.RefreshChannelAsync(channel);
                }
                catch
                {
                    // Continue with the remaining followed channels.
                }
                finally
                {
                    _remoteGate.Release();
                }
                RequestRefresh();
            }
        }
        catch
        {
            // Startup refresh is optional and must remain invisible to the UI.
        }
    }
}
