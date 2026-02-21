namespace Misa.Contract.Items.Components.Activity.Sessions;

public record StartSessionDto(
    Guid ItemId, 
    TimeSpan? PlannedDuration,
    string? Objective, 
    bool StopAutomatically, 
    string? AutoStopReason
);