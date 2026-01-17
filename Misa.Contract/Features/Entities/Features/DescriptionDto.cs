namespace Misa.Contract.Features.Entities.Features;

public record DescriptionDto(
    Guid Id,
    Guid EntityId,
    string Content,
    DateTimeOffset CreatedAtUtc
);
