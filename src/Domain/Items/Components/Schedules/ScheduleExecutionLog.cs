namespace Misa.Domain.Items.Components.Schedules;

/// <summary>
/// Represents a log entry for a scheduled execution.
/// </summary>
public sealed class ScheduleExecutionLog
{
    private ScheduleExecutionLog() { } // EF Core
    public ScheduleExecutionLog(
        Guid id,
        ItemId schedulerId,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SchedulerId = schedulerId;
        ScheduledForUtc = scheduledForUtc;

        Status = ScheduleExecutionStatus.Pending;
        Attempts = 0;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public ItemId SchedulerId { get; private set; }
    
    public DateTimeOffset ScheduledForUtc { get; private set; }
    
    public DateTimeOffset? ClaimedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }
    
    public ScheduleExecutionStatus Status { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }
    
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Claim(DateTimeOffset now)
    {
        Status = ScheduleExecutionStatus.Claimed;
        ClaimedAtUtc = now;
    }
    public void Start(DateTimeOffset now)
    {
        Attempts += 1;
        
        Status = ScheduleExecutionStatus.Running;
        StartedAtUtc = now;
    }
    public void Succeeded(DateTimeOffset now)
    {
        Status = ScheduleExecutionStatus.Succeeded;
        FinishedAtUtc = now;
    }
    public void Fail(DateTimeOffset now)
    {
        Status = ScheduleExecutionStatus.Failed;
        FinishedAtUtc = now;
    }
}
