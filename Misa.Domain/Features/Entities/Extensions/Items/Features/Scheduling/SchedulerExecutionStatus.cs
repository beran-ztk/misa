using System.Runtime.Serialization;

namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Represents the current state of a scheduled execution.
/// </summary>
public enum SchedulerExecutionStatus
{
    /// <summary>
    /// The execution is due but not yet claimed by a runner.
    /// </summary>
    Pending,
    
    /// <summary>
    /// A runner has claimed the execution but hasn't started yet.
    /// </summary>
    Claimed,
    
    /// <summary>
    /// The execution is currently running.
    /// </summary>
    Running,
    
    /// <summary>
    /// The execution completed successfully.
    /// </summary>
    Succeeded,
    
    /// <summary>
    /// The execution failed and may be retried.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The execution was deliberately skipped (e.g., misfire policy).
    /// </summary>
    Skipped
}