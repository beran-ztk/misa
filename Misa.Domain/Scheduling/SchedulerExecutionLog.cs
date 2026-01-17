namespace Misa.Domain.Scheduling;

/// <summary>
/// Represents a log entry for a scheduled execution.
/// </summary>
public sealed class SchedulerExecutionLog
{
    private SchedulerExecutionLog() { } // EF Core

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
}
