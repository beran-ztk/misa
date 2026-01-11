namespace Misa.Contract.Audit.Lookups;

public record SessionStateDto(
    int Id,
    string Name,
    string? Synopsis
);
