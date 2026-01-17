namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;

public record StartSessionCommand(
    Guid ItemId, 
    TimeSpan? PlannedDuration,
    string? Objective, 
    bool StopAutomatically, 
    string? AutoStopReason
);