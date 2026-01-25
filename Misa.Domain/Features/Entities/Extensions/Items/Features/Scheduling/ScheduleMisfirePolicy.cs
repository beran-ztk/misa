namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Defines how the scheduler should handle missed execution triggers.
/// </summary>
public enum ScheduleMisfirePolicy
{
    Catchup,
    Skip,
    RunOnce
}
