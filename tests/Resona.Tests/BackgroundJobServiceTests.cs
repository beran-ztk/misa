using System.Collections.Concurrent;
using System.Linq;
using Resona.Services;

namespace Resona.Tests;

public sealed class BackgroundJobServiceTests
{
    [Fact]
    public async Task Never_runs_more_than_configured_global_limit()
    {
        using var service = new BackgroundJobService(3);
        var release = NewSignal();
        var allWorkersStarted = NewSignal();
        var running = 0;
        var maximumRunning = 0;

        var jobs = Enumerable.Range(0, 9)
            .Select(index => service.RunAsync(
                Options($"job-{index}"),
                async (_, cancellationToken) =>
                {
                    var current = Interlocked.Increment(ref running);
                    UpdateMaximum(ref maximumRunning, current);
                    if (current == 3)
                        allWorkersStarted.TrySetResult();
                    try
                    {
                        await release.Task.WaitAsync(cancellationToken);
                        return index;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref running);
                    }
                }))
            .ToArray();

        await allWorkersStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(3, Volatile.Read(ref maximumRunning));
        Assert.Equal(3, service.GetSnapshot().Jobs.Count(job => job.State == BackgroundJobState.Running));

        release.TrySetResult();
        await Task.WhenAll(jobs).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(3, Volatile.Read(ref maximumRunning));
    }

    [Fact]
    public async Task User_work_overtakes_queued_background_work()
    {
        using var service = new BackgroundJobService(1);
        var blockerStarted = NewSignal();
        var releaseBlocker = NewSignal();
        var order = new ConcurrentQueue<string>();

        var blocker = service.RunAsync(
            Options("blocker"),
            async (_, cancellationToken) =>
            {
                order.Enqueue("blocker");
                blockerStarted.TrySetResult();
                await releaseBlocker.Task.WaitAsync(cancellationToken);
                return true;
            });
        await blockerStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var background = service.RunAsync(
            Options("background", BackgroundJobPriority.Background),
            (_, _) =>
            {
                order.Enqueue("background");
                return Task.FromResult(true);
            });
        var user = service.RunAsync(
            Options("user", BackgroundJobPriority.UserInitiated),
            (_, _) =>
            {
                order.Enqueue("user");
                return Task.FromResult(true);
            });

        releaseBlocker.TrySetResult();
        await Task.WhenAll(blocker, background, user).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(["blocker", "user", "background"], order.ToArray());
    }

    [Fact]
    public async Task Pause_holds_only_background_work()
    {
        using var service = new BackgroundJobService(1);
        service.SetBackgroundJobsPaused(true);
        var backgroundStarted = NewSignal();

        var backgrounds = Enumerable.Range(0, 5)
            .Select(index => service.RunAsync(
                Options($"background-{index}", BackgroundJobPriority.Background),
                (_, _) =>
                {
                    backgroundStarted.TrySetResult();
                    return Task.FromResult(true);
                }))
            .ToArray();

        await Task.Delay(100);
        Assert.False(backgroundStarted.Task.IsCompleted);

        var user = service.RunAsync(
            Options("user", BackgroundJobPriority.UserInitiated),
            (_, _) => Task.FromResult(true));
        await user.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(backgroundStarted.Task.IsCompleted);

        service.SetBackgroundJobsPaused(false);
        await Task.WhenAll(backgrounds).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(backgroundStarted.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Running_job_can_be_canceled()
    {
        using var service = new BackgroundJobService(1);
        var started = NewSignal();
        var job = service.RunAsync(
            Options("cancel-me"),
            async (_, cancellationToken) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return true;
            });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var id = Assert.Single(service.GetSnapshot().Jobs).Id;

        Assert.True(service.Cancel(id));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await job);
        Assert.Equal(BackgroundJobState.Canceled, Assert.Single(service.GetSnapshot().Jobs).State);
    }

    [Fact]
    public async Task Retries_exception_and_records_successful_attempt()
    {
        using var service = new BackgroundJobService(1);
        var attempts = 0;
        var result = await service.RunAsync(
            Options("retry") with { MaxAttempts = 2, RetryDelay = TimeSpan.FromMilliseconds(1) },
            (_, _) =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                    throw new InvalidOperationException("temporary");
                return Task.FromResult(42);
            });

        var snapshot = Assert.Single(service.GetSnapshot().Jobs);
        Assert.Equal(42, result);
        Assert.Equal(2, attempts);
        Assert.Equal(2, snapshot.Attempt);
        Assert.Equal(BackgroundJobState.Completed, snapshot.State);
    }

    [Fact]
    public async Task Domain_failure_result_is_returned_and_job_is_marked_failed()
    {
        using var service = new BackgroundJobService(1);
        var result = await service.RunAsync(
            Options("domain-failure"),
            (_, _) => Task.FromResult((Success: false, Error: "unavailable")),
            value => value.Success ? null : value.Error);

        var snapshot = Assert.Single(service.GetSnapshot().Jobs);
        Assert.False(result.Success);
        Assert.Equal(BackgroundJobState.Failed, snapshot.State);
        Assert.Equal("unavailable", snapshot.Error);
    }

    private static BackgroundJobOptions Options(
        string title,
        BackgroundJobPriority priority = BackgroundJobPriority.Normal) =>
        new(BackgroundJobKind.YouTubeDownload, title, "test", priority);

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int target, int candidate)
    {
        var current = Volatile.Read(ref target);
        while (candidate > current)
        {
            var observed = Interlocked.CompareExchange(ref target, candidate, current);
            if (observed == current)
                return;
            current = observed;
        }
    }
}
