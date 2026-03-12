using System;

namespace Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

public sealed class RetryOptions
{
    public static readonly RetryOptions None = new();

    public int MaxAttempts { get; init; } = 1;
    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(300);

    public bool Enabled => MaxAttempts > 1;
}