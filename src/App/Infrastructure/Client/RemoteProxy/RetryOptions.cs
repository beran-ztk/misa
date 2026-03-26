using System;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

public enum BackoffStrategy
{
    Constant,
    Linear,
    Exponential
}

public sealed class RetryOptions
{
    /// <summary>Send once, no retries.</summary>
    public static readonly RetryOptions None = new();

    /// <summary>
    /// Three attempts with 500 ms linear backoff — suitable for reads and idempotent operations.
    /// Delays before retries: 500 ms, then 1 000 ms.
    /// </summary>
    public static readonly RetryOptions Default = new()
    {
        MaxAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500)
    };

    /// <summary>Total number of send attempts (including the first). Must be ≥ 1.</summary>
    public int MaxAttempts { get; init; } = 1;

    /// <summary>Base delay between attempts. Exact meaning depends on <see cref="Backoff"/>.</summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>How the base delay is scaled as attempts increase. Default: Linear.</summary>
    public BackoffStrategy Backoff { get; init; } = BackoffStrategy.Linear;

    /// <summary>When true, adds ±10 % random jitter to reduce thundering-herd effects.</summary>
    public bool UseJitter { get; init; } = false;

    public bool HasRetry => MaxAttempts > 1;

    /// <summary>
    /// Computes the delay to wait before <paramref name="attempt"/> + 1 (i.e. after the given attempt number).
    /// </summary>
    public TimeSpan ComputeDelay(int attempt)
    {
        var baseMs = Delay.TotalMilliseconds;

        var computed = Backoff switch
        {
            BackoffStrategy.Constant    => baseMs,
            BackoffStrategy.Linear      => baseMs * attempt,
            BackoffStrategy.Exponential => baseMs * Math.Pow(2, attempt - 1),
            _                           => baseMs
        };

        if (UseJitter)
            computed *= 1.0 + (Random.Shared.NextDouble() * 0.2 - 0.1); // ±10 %

        return TimeSpan.FromMilliseconds(Math.Max(0, computed));
    }
}
