using System.Runtime.Serialization;

namespace Misa.Domain.Scheduling;

/// <summary>
/// Represents the current state of a scheduled execution.
/// </summary>
public enum SchedulerExecutionStatus
{
    /// <summary>
    /// The execution is due but not yet claimed by a runner.
    /// </summary>
    [EnumMember(Value = "pending")]
    Pending,
    
    /// <summary>
    /// A runner has claimed the execution but hasn't started yet.
    /// </summary>
    [EnumMember(Value = "claimed")]
    Claimed,
    
    /// <summary>
    /// The execution is currently running.
    /// </summary>
    [EnumMember(Value = "running")]
    Running,
    
    /// <summary>
    /// The execution completed successfully.
    /// </summary>
    [EnumMember(Value = "succeeded")]
    Succeeded,
    
    /// <summary>
    /// The execution failed and may be retried.
    /// </summary>
    [EnumMember(Value = "failed")]
    Failed,
    
    /// <summary>
    /// The execution was deliberately skipped (e.g., misfire policy).
    /// </summary>
    [EnumMember(Value = "skipped")]
    Skipped
}