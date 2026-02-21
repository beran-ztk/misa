namespace Misa.Domain.Items.Components.Schedules;

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