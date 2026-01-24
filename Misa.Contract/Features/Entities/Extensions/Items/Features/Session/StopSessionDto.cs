namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public record StopSessionDto(
    Guid ItemId,
    EfficiencyContract Efficiency,
    ConcentrationContract Concentration,
    string? Summary
);
