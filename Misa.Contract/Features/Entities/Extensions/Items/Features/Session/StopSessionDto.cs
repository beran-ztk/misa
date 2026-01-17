namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public record StopSessionDto(
    Guid ItemId,
    int? EfficiencyId,
    int? ConcentrationId,
    string? Summary
);
