using Resona.Services;

namespace Resona.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void Instance_key_is_stable_per_application_and_identity()
    {
        var first = SingleInstanceCoordinator.BuildInstanceKey("Beran.Resona", "domain\\user");
        var second = SingleInstanceCoordinator.BuildInstanceKey("Beran.Resona", "domain\\user");
        var otherUser = SingleInstanceCoordinator.BuildInstanceKey("Beran.Resona", "domain\\other");

        Assert.Equal(first, second);
        Assert.NotEqual(first, otherUser);
        Assert.DoesNotContain('\\', first);
        Assert.DoesNotContain('/', first);
    }

    [Fact]
    public async Task Second_instance_notifies_primary_instance()
    {
        var applicationId = $"Resona.Tests.{Guid.NewGuid():N}";
        var identity = Guid.NewGuid().ToString("N");
        using var primary = SingleInstanceCoordinator.Start(applicationId, identity);
        using var secondary = SingleInstanceCoordinator.Start(applicationId, identity);
        using var activated = new ManualResetEventSlim();
        primary.SetActivationHandler(activated.Set);

        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);
        Assert.True(await secondary.NotifyPrimaryAsync());
        Assert.True(activated.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void Different_user_identity_gets_independent_instance()
    {
        var applicationId = $"Resona.Tests.{Guid.NewGuid():N}";
        using var first = SingleInstanceCoordinator.Start(applicationId, "user-one");
        using var second = SingleInstanceCoordinator.Start(applicationId, "user-two");

        Assert.True(first.IsPrimary);
        Assert.True(second.IsPrimary);
    }

    [Fact]
    public async Task Activation_is_remembered_until_window_handler_is_ready()
    {
        var applicationId = $"Resona.Tests.{Guid.NewGuid():N}";
        var identity = Guid.NewGuid().ToString("N");
        using var primary = SingleInstanceCoordinator.Start(applicationId, identity);
        using var secondary = SingleInstanceCoordinator.Start(applicationId, identity);
        using var activated = new ManualResetEventSlim();

        Assert.True(await secondary.NotifyPrimaryAsync());
        primary.SetActivationHandler(activated.Set);

        Assert.True(activated.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void Lock_is_available_after_primary_disposes()
    {
        var applicationId = $"Resona.Tests.{Guid.NewGuid():N}";
        var identity = Guid.NewGuid().ToString("N");
        using (var first = SingleInstanceCoordinator.Start(applicationId, identity))
            Assert.True(first.IsPrimary);

        using var next = SingleInstanceCoordinator.Start(applicationId, identity);
        Assert.True(next.IsPrimary);
    }
}
