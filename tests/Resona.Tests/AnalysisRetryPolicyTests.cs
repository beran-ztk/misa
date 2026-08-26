using Resona.Services;

namespace Resona.Tests;

public sealed class AnalysisRetryPolicyTests
{
    [Fact]
    public async Task Failed_connection_stops_after_three_attempts()
    {
        var attempts = 0;

        var connected = await AnalysisRetryPolicy.CheckConnectionAsync(
            _ => Task.FromResult(++attempts > 10));

        Assert.False(connected);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Successful_connection_stops_retrying_immediately()
    {
        var attempts = 0;

        var connected = await AnalysisRetryPolicy.CheckConnectionAsync(
            _ => Task.FromResult(++attempts == 2));

        Assert.True(connected);
        Assert.Equal(2, attempts);
    }
}
