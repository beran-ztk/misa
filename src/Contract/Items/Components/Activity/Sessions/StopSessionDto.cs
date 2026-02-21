namespace Misa.Contract.Items.Components.Activity.Sessions;

public record StopSessionDto(
    Guid ItemId,
    SessionEfficiencyDto SessionEfficiency,
    SessionConcentrationDto SessionConcentration,
    string? Summary
);
