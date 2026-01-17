namespace Misa.Application.Items.Features.Sessions.Commands;

public record StopSessionCommand(
    Guid ItemId,
    int? EfficiencyId,
    int? ConcentrationId,
    string? Summary
);
