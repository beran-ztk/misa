namespace Misa.Application.Items.Commands;

public record StopSessionCommand(
    Guid ItemId,
    int? EfficiencyId,
    int? ConcentrationId,
    string? Summary
);
