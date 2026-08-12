using Resona.Services;

namespace Resona.Tests;

public sealed class CanonicalUrlOperationCoordinatorTests
{
    [Fact]
    public async Task Same_url_is_serialized()
    {
        var coordinator = new CanonicalUrlOperationCoordinator();
        using var first = await coordinator.AcquireAsync("https://youtu.be/same");
        var second = coordinator.AcquireAsync("https://youtu.be/same");

        await Task.Delay(30);
        Assert.False(second.IsCompleted);

        first.Dispose();
        using var secondLease = await second.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Different_urls_do_not_block_each_other()
    {
        var coordinator = new CanonicalUrlOperationCoordinator();
        using var first = await coordinator.AcquireAsync("https://youtu.be/one");
        using var second = await coordinator.AcquireAsync("https://youtu.be/two")
            .WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Cancelled_waiter_does_not_poison_the_url()
    {
        var coordinator = new CanonicalUrlOperationCoordinator();
        using var first = await coordinator.AcquireAsync("https://youtu.be/same");
        using var cancellation = new CancellationTokenSource();
        var cancelledWaiter = coordinator.AcquireAsync("https://youtu.be/same", cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWaiter);
        first.Dispose();

        using var next = await coordinator.AcquireAsync("https://youtu.be/same")
            .WaitAsync(TimeSpan.FromSeconds(1));
    }
}
