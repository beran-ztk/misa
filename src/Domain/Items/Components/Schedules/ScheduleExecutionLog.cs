namespace Misa.Domain.Items.Components.Schedules;

/// <summary>
/// Represents a log entry for a scheduled execution.
/// </summary>
public sealed class ScheduleExecutionLog
{
    private ScheduleExecutionLog() { } // EF Application
    public ScheduleExecutionLog(
        ItemId schedulerId,
        DateTimeOffset scheduledForUtc)
    {
        Id = Guid.NewGuid();
        SchedulerId = schedulerId;
        ScheduledForUtc = scheduledForUtc;

        Status = ScheduleExecutionStatus.Pending;
        Attempts = 0;
        CreatedAtUtc = DateTimeOffset.UtcNow;
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

    public void Claim()
    {
        Status = ScheduleExecutionStatus.Claimed;
        ClaimedAtUtc = DateTimeOffset.UtcNow;
    }
    public void Start()
    {
        Attempts += 1;

        Status = ScheduleExecutionStatus.Running;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }
    public void Succeeded()
    {
        Status = ScheduleExecutionStatus.Succeeded;
        FinishedAtUtc = DateTimeOffset.UtcNow;
    }
    public void Fail(string? error)
    {
        Status = ScheduleExecutionStatus.Failed;
        Error = error;
        FinishedAtUtc = DateTimeOffset.UtcNow;
    }
}
