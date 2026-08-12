using Resona.Models;
using Resona.Services;

namespace Resona.Tests;

public sealed class ImportQueueStateMachineTests
{
    [Theory]
    [InlineData(ImportQueueStatus.Queued, ImportQueueStatus.Downloading)]
    [InlineData(ImportQueueStatus.Downloading, ImportQueueStatus.ReadyForReview)]
    [InlineData(ImportQueueStatus.Downloading, ImportQueueStatus.Failed)]
    [InlineData(ImportQueueStatus.Analyzing, ImportQueueStatus.ReadyForReview)]
    [InlineData(ImportQueueStatus.Failed, ImportQueueStatus.Queued)]
    public void Allows_declared_transitions(ImportQueueStatus from, ImportQueueStatus to)
    {
        Assert.True(ImportQueueStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ImportQueueStatus.Queued, ImportQueueStatus.ReadyForReview)]
    [InlineData(ImportQueueStatus.Downloading, ImportQueueStatus.Queued)]
    [InlineData(ImportQueueStatus.ReadyForReview, ImportQueueStatus.Downloading)]
    [InlineData(ImportQueueStatus.Skipped, ImportQueueStatus.Queued)]
    public void Rejects_undeclared_transitions(ImportQueueStatus from, ImportQueueStatus to)
    {
        Assert.False(ImportQueueStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ImportQueueStatus.Queued)]
    [InlineData(ImportQueueStatus.Downloading)]
    [InlineData(ImportQueueStatus.Analyzing)]
    [InlineData(ImportQueueStatus.Failed)]
    public void Allows_progress_updates_without_phase_change(ImportQueueStatus status)
    {
        Assert.True(ImportQueueStateMachine.CanTransition(status, status));
    }
}
