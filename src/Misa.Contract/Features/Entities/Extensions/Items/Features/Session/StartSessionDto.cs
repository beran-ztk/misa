namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public record StartSessionDto(
    Guid ItemId, 
    TimeSpan? PlannedDuration,
    string? Objective, 
    bool StopAutomatically, 
    string? AutoStopReason
);