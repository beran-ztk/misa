namespace Misa.Domain.Items.Components.Schedules;

/// <summary>
/// Defines how the scheduler should handle missed execution triggers.
/// </summary>
public enum ScheduleMisfirePolicy
{
    Catchup,
    Skip,
    RunOnce
}
