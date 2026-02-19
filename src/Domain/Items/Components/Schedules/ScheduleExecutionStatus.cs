namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Represents the current state of a scheduled execution.
/// </summary>
public enum ScheduleExecutionStatus
{
    Pending,
    Claimed,
    Running,
    Succeeded,
    Failed,
    Skipped
}