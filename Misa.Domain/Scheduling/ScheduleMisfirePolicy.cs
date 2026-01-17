using System.Runtime.Serialization;

namespace Misa.Domain.Scheduling;

/// <summary>
/// Defines how the scheduler should handle missed execution triggers.
/// </summary>
public enum ScheduleMisfirePolicy
{
    /// <summary>
    /// Skip the missed execution entirely.
    /// </summary>
    [EnumMember(Value = "skip")]
    Skip,
    
    /// <summary>
    /// Execute the missed trigger once immediately.
    /// </summary>
    [EnumMember(Value = "run_once")]
    RunOnce,
    
    /// <summary>
    /// Execute all missed triggers that should have occurred.
    /// </summary>
    [EnumMember(Value = "catchup")]
    Catchup
}
