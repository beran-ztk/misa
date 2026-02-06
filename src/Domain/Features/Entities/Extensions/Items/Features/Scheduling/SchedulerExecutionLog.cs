namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Represents a log entry for a scheduled execution.
/// </summary>
public sealed class SchedulerExecutionLog
{
    private SchedulerExecutionLog() { } // EF Core
    public SchedulerExecutionLog(
        Guid id,
        Guid schedulerId,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SchedulerId = schedulerId;
        ScheduledForUtc = scheduledForUtc;

        Status = SchedulerExecutionStatus.Pending;
        Attempts = 0;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SchedulerId { get; private set; }
    
    public DateTimeOffset ScheduledForUtc { get; private set; }
    
    public DateTimeOffset? ClaimedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }
    
    public SchedulerExecutionStatus Status { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }
    
    public DateTimeOffset CreatedAtUtc { get; private set; }
    
    public Scheduler Scheduler { get; private set; } = null!;

    public void Claim(DateTimeOffset now)
    {
        Status = SchedulerExecutionStatus.Claimed;
        ClaimedAtUtc = now;
    }
    public void Start(DateTimeOffset now)
    {
        Attempts += 1;
        
        Status = SchedulerExecutionStatus.Running;
        StartedAtUtc = now;
    }
    public void Succeeded(DateTimeOffset now)
    {
        Status = SchedulerExecutionStatus.Succeeded;
        FinishedAtUtc = now;
    }
    public void Fail(DateTimeOffset now)
    {
        Status = SchedulerExecutionStatus.Failed;
        FinishedAtUtc = now;
    }
}
