using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Resona.Services;

/// <summary>
/// Serializes operations that can create a local track for the same canonical URL.
/// This closes the check/download/insert race between manual imports and ChannelHub.
/// </summary>
public sealed class CanonicalUrlOperationCoordinator
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IDisposable> AcquireAsync(
        string canonicalUrl,
        CancellationToken cancellationToken = default)
    {
        Entry entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(canonicalUrl, out entry!))
                _entries[canonicalUrl] = entry = new Entry();
            entry.ReferenceCount++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            return new Lease(this, canonicalUrl, entry);
        }
        catch
        {
            ReleaseReference(canonicalUrl, entry, releaseSemaphore: false);
            throw;
        }
    }

    private void ReleaseReference(string key, Entry entry, bool releaseSemaphore)
    {
        if (releaseSemaphore)
            entry.Semaphore.Release();

        lock (_gate)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0
                && _entries.TryGetValue(key, out var current)
                && ReferenceEquals(current, entry))
            {
                _entries.Remove(key);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
    }

    private sealed class Lease(
        CanonicalUrlOperationCoordinator owner,
        string key,
        Entry entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.ReleaseReference(key, entry, releaseSemaphore: true);
        }
    }
}
