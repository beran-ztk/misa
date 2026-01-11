namespace Misa.Contract.Audit.Lookups;

public record SessionConcentrationTypeDto(
    int Id,
    string Name,
    string? Synopsis,
    int SortOrder
);
