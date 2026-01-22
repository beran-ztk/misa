namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Defines how the scheduler should handle missed execution triggers.
/// </summary>
public enum ScheduleMisfirePolicy
{
    /// <summary>
    /// Execute all missed triggers that should have occurred.
    /// </summary>
    Catchup,
    
    /// <summary>
    /// Skip the missed execution entirely.
    /// </summary>
    Skip,
    
    /// <summary>
    /// Execute the missed trigger once immediately.
    /// </summary>
    RunOnce
}
