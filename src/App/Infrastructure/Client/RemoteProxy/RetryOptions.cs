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
    public static readonly RetryOptions None = new();

    public static readonly RetryOptions Default = new()
    {
        MaxAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500)
    };

    public int MaxAttempts { get; init; } = 1;
    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(300);
    public BackoffStrategy Backoff { get; init; } = BackoffStrategy.Linear;
    public bool UseJitter { get; init; } = false;
    public bool HasRetry => MaxAttempts > 1;

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
