namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

public record SessionEfficiencyTypeDto(
    int Id,
    string Name,
    string? Synopsis,
    int SortOrder
);
