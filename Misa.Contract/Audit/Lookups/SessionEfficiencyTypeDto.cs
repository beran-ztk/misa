namespace Misa.Contract.Audit.Lookups;

public record SessionEfficiencyTypeDto(
    int Id,
    string Name,
    string? Synopsis,
    int SortOrder
);
