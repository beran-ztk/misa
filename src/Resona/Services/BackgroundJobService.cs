using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resona.Services;

public enum BackgroundJobKind
{
    YouTubeDownload,
    YouTubeMetadata,
    YouTubePlaylist,
    YouTubeChannelRefresh
}

public enum BackgroundJobPriority
{
    UserInitiated = 0,
    Normal = 10,
    Background = 20
}

public enum BackgroundJobState
{
    Queued,
    Running,
    Completed,
    Failed,
    Canceled
}

public sealed record BackgroundJobOptions(
    BackgroundJobKind Kind,
    string Title,
    string Source,
    BackgroundJobPriority Priority = BackgroundJobPriority.Normal,
    int MaxAttempts = 1,
    TimeSpan? RetryDelay = null);

public sealed record BackgroundJobSnapshot(
    Guid Id,
    BackgroundJobKind Kind,
    string Title,
    string Source,
    BackgroundJobPriority Priority,
    BackgroundJobState State,
    string Detail,
    double? Progress,
    int Attempt,
    int MaxAttempts,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? Error);

public sealed record BackgroundJobServiceSnapshot(
    int MaximumConcurrency,
    bool BackgroundJobsPaused,
    IReadOnlyList<BackgroundJobSnapshot> Jobs);

public sealed class BackgroundJobContext
{
    private readonly Action<string, double?> _report;

    internal BackgroundJobContext(Action<string, double?> report) => _report = report;

    public void Report(string detail, double? progress = null) =>
        _report(detail, progress is null ? null : Math.Clamp(progress.Value, 0, 1));
}

/// <summary>
/// Central scheduler for remote YouTube work. Existing durable domain queues keep
/// ownership of their records; every yt-dlp execution passes through this one
/// scheduler and therefore shares its priority queue and global concurrency limit.
/// </summary>
public sealed class BackgroundJobService : IDisposable
{
    public const int DefaultMaximumConcurrency = 3;
    private const int MaximumRetainedFinishedJobs = 100;
    public static readonly BackgroundJobService Current = new(DefaultMaximumConcurrency);

    private readonly object _gate = new();
    private readonly List<WorkItem> _queue = [];
    private readonly Dictionary<Guid, WorkItem> _jobs = [];
    private readonly SemaphoreSlim _workAvailable = new(0);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task[] _workers;
    private long _nextSequence;
    private bool _backgroundJobsPaused;
    private bool _disposed;

    public BackgroundJobService(int maximumConcurrency)
    {
        if (maximumConcurrency is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrency));

        MaximumConcurrency = maximumConcurrency;
        _workers = Enumerable.Range(0, maximumConcurrency)
            .Select(_ => Task.Run(ProcessQueueAsync))
            .ToArray();
    }

    public int MaximumConcurrency { get; }
    public event Action<BackgroundJobServiceSnapshot>? SnapshotChanged;

    public Task<T> RunAsync<T>(
        BackgroundJobOptions options,
        Func<BackgroundJobContext, CancellationToken, Task<T>> operation,
        Func<T, string?>? failureSelector = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(operation);
        if (string.IsNullOrWhiteSpace(options.Title))
            throw new ArgumentException("A job title is required.", nameof(options));
        if (options.MaxAttempts is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxAttempts must be between 1 and 10.");

        WorkItem item;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            item = new WorkItem(
                options,
                ++_nextSequence,
                cancellationToken,
                async (context, token) => await operation(context, token),
                failureSelector is null ? null : result => failureSelector((T)result!));
            _queue.Add(item);
            _jobs.Add(item.Id, item);
        }

        item.Cancellation.Token.Register(() => SignalWorkAvailable());
        SignalWorkAvailable();
        PublishSnapshot();
        return AwaitResult<T>(item.Completion.Task);
    }

    public BackgroundJobServiceSnapshot GetSnapshot()
    {
        lock (_gate)
            return BuildSnapshotLocked();
    }

    public void SetBackgroundJobsPaused(bool paused)
    {
        var changed = false;
        var queuedCount = 0;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_backgroundJobsPaused != paused)
            {
                _backgroundJobsPaused = paused;
                changed = true;
                if (!paused)
                    queuedCount = _queue.Count;
            }
        }

        if (!changed)
            return;
        if (!paused)
        {
            // Signals for paused jobs may already have been consumed by idle workers.
            // Re-signal every queued item so the queue is drained completely.
            for (var index = 0; index < queuedCount; index++)
                SignalWorkAvailable();
        }
        PublishSnapshot();
    }

    public bool Cancel(Guid jobId)
    {
        WorkItem? item;
        lock (_gate)
        {
            if (!_jobs.TryGetValue(jobId, out item)
                || item.Cancellation.IsCancellationRequested
                || item.State is BackgroundJobState.Completed
                    or BackgroundJobState.Failed
                    or BackgroundJobState.Canceled)
                return false;

            item.Detail = "Canceling…";
        }

        item.Cancellation.Cancel();
        SignalWorkAvailable();
        PublishSnapshot();
        return true;
    }

    public int ClearFinishedJobs()
    {
        List<WorkItem> finished;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            finished = _jobs.Values
                .Where(job => job.State is BackgroundJobState.Completed
                    or BackgroundJobState.Failed
                    or BackgroundJobState.Canceled)
                .ToList();
            foreach (var item in finished)
                _jobs.Remove(item.Id);
        }

        foreach (var item in finished)
            item.Dispose();
        if (finished.Count > 0)
            PublishSnapshot();
        return finished.Count;
    }

    private async Task ProcessQueueAsync()
    {
        var shutdownToken = _shutdown.Token;
        try
        {
            while (true)
            {
                await _workAvailable.WaitAsync(shutdownToken);
                WorkItem? item;
                lock (_gate)
                    item = TakeNextEligibleLocked();
                if (item is null)
                    continue;

                await ExecuteAsync(item, shutdownToken);
            }
        }
        catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
        {
        }
    }

    private WorkItem? TakeNextEligibleLocked()
    {
        WorkItem? selected = null;
        foreach (var candidate in _queue)
        {
            if (!candidate.Cancellation.IsCancellationRequested
                && _backgroundJobsPaused
                && candidate.Options.Priority == BackgroundJobPriority.Background)
                continue;

            if (selected is null
                || candidate.Options.Priority < selected.Options.Priority
                || (candidate.Options.Priority == selected.Options.Priority
                    && candidate.Sequence < selected.Sequence))
                selected = candidate;
        }

        if (selected is not null)
            _queue.Remove(selected);
        return selected;
    }

    private async Task ExecuteAsync(WorkItem item, CancellationToken shutdownToken)
    {
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            item.Cancellation.Token,
            shutdownToken);
        var cancellationToken = executionCancellation.Token;

        if (cancellationToken.IsCancellationRequested)
        {
            CompleteCanceled(item, cancellationToken);
            return;
        }

        for (var attempt = 1; attempt <= item.Options.MaxAttempts; attempt++)
        {
            Update(item, job =>
            {
                job.State = BackgroundJobState.Running;
                job.Attempt = attempt;
                job.StartedAtUtc ??= DateTime.UtcNow;
                job.Detail = attempt == 1 ? "Starting…" : $"Retrying · attempt {attempt}";
                job.Error = null;
            });

            try
            {
                var context = new BackgroundJobContext((detail, progress) =>
                    Update(item, job =>
                    {
                        job.Detail = detail;
                        job.Progress = progress;
                    }));
                var result = await item.Operation(context, cancellationToken);
                var error = item.FailureSelector?.Invoke(result);
                if (error is null)
                {
                    Update(item, job =>
                    {
                        job.State = BackgroundJobState.Completed;
                        job.Detail = "Completed";
                        job.Progress = 1;
                        job.FinishedAtUtc = DateTime.UtcNow;
                    });
                    item.Completion.TrySetResult(result);
                    PruneFinishedJobs();
                    return;
                }

                if (attempt < item.Options.MaxAttempts)
                {
                    await DelayBeforeRetry(item, error, attempt, cancellationToken);
                    continue;
                }

                Update(item, job =>
                {
                    job.State = BackgroundJobState.Failed;
                    job.Detail = "Failed";
                    job.Error = error;
                    job.FinishedAtUtc = DateTime.UtcNow;
                });
                // Domain callers still need the process result and its stderr.
                item.Completion.TrySetResult(result);
                PruneFinishedJobs();
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CompleteCanceled(item, cancellationToken);
                return;
            }
            catch (Exception exception)
            {
                if (attempt < item.Options.MaxAttempts)
                {
                    await DelayBeforeRetry(item, exception.Message, attempt, cancellationToken);
                    continue;
                }

                Update(item, job =>
                {
                    job.State = BackgroundJobState.Failed;
                    job.Detail = "Failed";
                    job.Error = exception.Message;
                    job.FinishedAtUtc = DateTime.UtcNow;
                });
                item.Completion.TrySetException(exception);
                PruneFinishedJobs();
                return;
            }
        }
    }

    private async Task DelayBeforeRetry(
        WorkItem item,
        string error,
        int attempt,
        CancellationToken cancellationToken)
    {
        var baseDelay = item.Options.RetryDelay ?? TimeSpan.FromSeconds(1);
        var delay = TimeSpan.FromMilliseconds(Math.Min(
            TimeSpan.FromSeconds(30).TotalMilliseconds,
            baseDelay.TotalMilliseconds * Math.Pow(2, Math.Max(0, attempt - 1))));
        Update(item, job =>
        {
            job.Detail = $"Retrying in {Math.Max(1, Math.Ceiling(delay.TotalSeconds)):0} sec";
            job.Error = error;
        });
        await Task.Delay(delay, cancellationToken);
    }

    private void CompleteCanceled(WorkItem item, CancellationToken cancellationToken)
    {
        Update(item, job =>
        {
            job.State = BackgroundJobState.Canceled;
            job.Detail = "Canceled";
            job.FinishedAtUtc = DateTime.UtcNow;
        });
        item.Completion.TrySetCanceled(cancellationToken);
        PruneFinishedJobs();
    }

    private void Update(WorkItem item, Action<WorkItem> update)
    {
        lock (_gate)
            update(item);
        PublishSnapshot();
    }

    private void PruneFinishedJobs()
    {
        lock (_gate)
        {
            var finished = _jobs.Values
                .Where(job => job.FinishedAtUtc is not null)
                .OrderByDescending(job => job.FinishedAtUtc)
                .Skip(MaximumRetainedFinishedJobs)
                .ToList();
            foreach (var item in finished)
            {
                _jobs.Remove(item.Id);
                item.Dispose();
            }
        }
        PublishSnapshot();
    }

    private BackgroundJobServiceSnapshot BuildSnapshotLocked() => new(
        MaximumConcurrency,
        _backgroundJobsPaused,
        _jobs.Values
            .OrderBy(job => job.State is BackgroundJobState.Running ? 0 : job.State == BackgroundJobState.Queued ? 1 : 2)
            .ThenBy(job => job.Options.Priority)
            .ThenByDescending(job => job.FinishedAtUtc)
            .ThenBy(job => job.Sequence)
            .Select(job => job.ToSnapshot())
            .ToList());

    private void PublishSnapshot()
    {
        BackgroundJobServiceSnapshot snapshot;
        Action<BackgroundJobServiceSnapshot>? handlers;
        lock (_gate)
        {
            snapshot = BuildSnapshotLocked();
            handlers = SnapshotChanged;
        }

        if (handlers is null)
            return;
        foreach (Action<BackgroundJobServiceSnapshot> handler in handlers.GetInvocationList())
        {
            try { handler(snapshot); }
            catch (Exception exception) { WorkflowLog.Error("jobs", "Snapshot observer failed.", exception); }
        }
    }

    private void SignalWorkAvailable()
    {
        try { _workAvailable.Release(); }
        catch (ObjectDisposedException) { }
        catch (SemaphoreFullException) { }
    }

    private static async Task<T> AwaitResult<T>(Task<object?> task) =>
        (T)(await task)!;

    public void Dispose()
    {
        List<WorkItem> jobs;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            jobs = _jobs.Values.ToList();
        }

        foreach (var job in jobs)
            job.Cancellation.Cancel();
        _shutdown.Cancel();
        try { Task.WaitAll(_workers, TimeSpan.FromSeconds(2)); }
        catch (AggregateException) { }
        lock (_gate)
        {
            foreach (var job in jobs.Where(job => !job.Completion.Task.IsCompleted))
            {
                job.State = BackgroundJobState.Canceled;
                job.Detail = "Canceled";
                job.FinishedAtUtc = DateTime.UtcNow;
                job.Completion.TrySetCanceled();
            }
        }
        foreach (var job in jobs)
            job.Dispose();
        _shutdown.Dispose();
        _workAvailable.Dispose();
    }

    private sealed class WorkItem : IDisposable
    {
        public WorkItem(
            BackgroundJobOptions options,
            long sequence,
            CancellationToken cancellationToken,
            Func<BackgroundJobContext, CancellationToken, Task<object?>> operation,
            Func<object?, string?>? failureSelector)
        {
            Options = options;
            Sequence = sequence;
            Operation = operation;
            FailureSelector = failureSelector;
            Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public Guid Id { get; } = Guid.NewGuid();
        public BackgroundJobOptions Options { get; }
        public long Sequence { get; }
        public Func<BackgroundJobContext, CancellationToken, Task<object?>> Operation { get; }
        public Func<object?, string?>? FailureSelector { get; }
        public CancellationTokenSource Cancellation { get; }
        public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public BackgroundJobState State { get; set; } = BackgroundJobState.Queued;
        public string Detail { get; set; } = "Waiting";
        public double? Progress { get; set; }
        public int Attempt { get; set; }
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? FinishedAtUtc { get; set; }
        public string? Error { get; set; }

        public BackgroundJobSnapshot ToSnapshot() => new(
            Id,
            Options.Kind,
            Options.Title,
            Options.Source,
            Options.Priority,
            State,
            Detail,
            Progress,
            Attempt,
            Options.MaxAttempts,
            CreatedAtUtc,
            StartedAtUtc,
            FinishedAtUtc,
            Error);

        public void Dispose() => Cancellation.Dispose();
    }
}
